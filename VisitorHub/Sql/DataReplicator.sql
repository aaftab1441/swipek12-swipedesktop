/*

	Documentation
		--Add new tables to Sync to #TablesToSync in code below when you need new tables synchronized.  
		--#TablesToSync has a setting for "Transactional" updates vs "Snapshot" updates
			--PersonDayAttend is an example of Transactional...it can be run very frequently.  
			--Students, Person, and "lookup" data is Snapshot
			--Transactional data uses the LastUpdated cols to determine what needs to be synced.  
		--we do NOT sync the "synced" cols since they do not exist on SwipeK12 database
	
	Known Issues
		--This does not handle DELETEs, but could if required.  ie, if a row exists on the swipestation but is not
		in SwipeK12 then it will remain on the local swipestation db.  

	Example calls:

		--standard call for the Snapshot tables (once per day)
		EXEC DataReplicator 
			@DataSource = 'www.swipek12.com'
		
		--same as previous, but explicitly declare the @SyncType
		EXEC DataReplicator 
			@DataSource = 'www.swipek12.com',
			@SyncType = 'Snapshot';

		--same as previous, but explicitly call for only a single @SchoolID
		EXEC DataReplicator 
			@DataSource = 'www.swipek12.com',
			@SchoolID = 12345;

		--on-going, frequently updated data calls
		EXEC DataReplicator 
			@DataSource = 'www.swipek12.com',
			@SyncType = 'Transactional';

		--something got out of sync...resync everything
		EXEC DataReplicator 
			@DataSource = 'www.swipek12.com',
			@SyncType = 'Resync';
		
		--this call doesn't connect, just prints out the commands that would be run.  
		EXEC DataReplicator 
			@DataSource = 'www.swipek12.com'
			,@DebugOnly = 1

	Logging:  
		SELECT * FROM DataReplicatorLog ORDER BY DataReplicatorLogID DESC
		TRUNCATE TABLE DataReplicatorLog


*/


IF EXISTS (select * from sys.objects where object_id = object_id('DataReplicator'))
	DROP PROCEDURE DataReplicator;
GO
CREATE PROCEDURE DataReplicator (
	@DataSource varchar(2000)  --this is the SwipeK12 server
	,@DebugOnly bit = 0 
	,@SyncType varchar(200) = 'Snapshot'
	,@SchoolID int = NULL

) 
WITH ENCRYPTION
AS
BEGIN
	/*
		This procedure is installed in the local swipestation database and will connect to the remote SwipeK12 db.  
		It should be called asynchronously since it *might* run for some time.  

		We can make this far more dynamic to handle more tables with less effort in the future, but let's do this as a POC
		to ensure it actually works and meets your requirements.  
		
	*/
	SET NOCOUNT ON 
	SELECT @DebugOnly = COALESCE(@DebugOnly,0);
	SELECT @SyncType = COALESCE(@SyncType, 'Snapshot');

	EXEC DataReplicatorConnector @Action='CREATE', @DataSource = @DataSource;
	DECLARE @exec_str nvarchar(max), @TableName varchar(400), @error bit, @ColsToSkip varchar(4000);
	DECLARE @LogStartTime DATETIME, @ErrMsg nvarchar(4000);

	SELECT @error = 0 ;
	

	--get the list of tables to sync (this will make everything far more generic for future expansion)
	CREATE TABLE #TablesToSync (TableName varchar(200), SyncType varchar(200),ColsToSkip varchar(4000), SyncOrder int);
	--we skip FreeLunch col because that does not exist on the server
	INSERT INTO #TablesToSync VALUES 
		('School'			, 'Snapshot','CurrMonthDate',100),
		('Person'			, 'Snapshot',NULL, 200),
		('Students'			, 'Snapshot','FreeLunch,Grade',300),
		('AttendStatus'		, 'Snapshot','CUSTOMAttendStatus',400),
		('Episode'			, 'Snapshot',NULL,500),
		('Excuse'			, 'Snapshot',NULL,600),
		('FreeReducedLunch'	, 'Snapshot',NULL,700),
		('Outcome'			, 'Snapshot',NULL,800),
		('ReleaseReasons'	, 'Snapshot',NULL,900),
		--('TardySwipe'		, 'Snapshot',NULL,200),
		--('TardySwipe'		, 'Transactional',NULL,1200),
		('PersonDayAttend'	, 'Snapshot','CUSTOMPersonDayAttend',1100),
		--('PersonDayAttend'	, 'Transactional','CUSTOMPersonDayAttend',1100),
		('PersonOutcome'	, 'Snapshot','CUSTOMPersonOutcome',1200),
		('PersonEpisode'	, 'Snapshot','CUSTOMPersonEpisode',1300),
		('LunchSched'		, 'Snapshot','CUSTOMLunchSched', 1000),
		('Staff'			, 'Snapshot','CUSTOMStaff',1400),
		('StaffData'		, 'Snapshot',NULL,1400),
		('Inoutrooms'	, 'Snapshot',NULL,1500);
		--('StudentInOut'	, 'Snapshot',NULL,1500);
	
	--run each table in turn
	DECLARE curForEachTable CURSOR FOR
		SELECT TableName, ColsToSkip 
		FROM #TablesToSync
		--Resync is simply Transactional without the LastUpdated filter condition
		WHERE SyncType = CASE WHEN @SyncType = 'Resync' THEN 'Transactional' ELSE @SyncType END
		ORDER BY SyncOrder;				--resolves FK issues
	OPEN curForEachTable
	FETCH NEXT FROM curForEachTable INTO @TableName,@ColsToSkip;
	WHILE (@@fetch_status = 0)
	BEGIN
		
		EXEC dbo.ScriptGenerator
			@table_schema = 'dbo',
			@table_name = @TableName,
			@output = @exec_str OUTPUT,
			@ColsToSkip = @ColsToSkip,
			@SyncType = @SyncType,
			@SchoolID = @SchoolID;
		PRINT @exec_str;
		IF @DebugOnly = 0
		BEGIN
			BEGIN TRY
				IF COALESCE(@exec_str,'') = ''
				BEGIN
					SELECT @error = 1
					PRINT 'An error occurred while populating: ' + @TableName + ':'
					SELECT @ErrMsg = 'NULL @exec_str...dbo.ScriptGenerator failed somewhere. Probably because there is no key on the table.'
					PRINT @ErrMsg
					EXEC DataGeneratorLogger
						@LogStartTime = @LogStartTime,
						@SyncType = @SyncType,
						@Success = 0,
						@SyncTable = @TableName,
						@Command = @exec_str,
						@Results = @ErrMsg;
				END;
				ELSE
				BEGIN
					SELECT @LogStartTime = getdate();
					EXEC sys.sp_executesql @exec_str;
					PRINT 'Success: ' + @TableName;
					EXEC DataGeneratorLogger
						@LogStartTime = @LogStartTime,
						@SyncType = @SyncType,
						@Success = 1,
						@SyncTable = @TableName,
						@Command = @exec_str,
						@Results = NULL;
				END;
			END TRY
			BEGIN CATCH
				--mark that an error occurred and move along
				SELECT @error = 1
				PRINT 'An error occurred while populating: ' + @TableName + ':'
				PRINT ERROR_MESSAGE()
				SELECT @ErrMsg = ERROR_MESSAGE();
				PRINT 'Continuing to run DataReplicator for other tables, if possible'
				EXEC DataGeneratorLogger
					@LogStartTime = @LogStartTime,
					@SyncType = @SyncType,
					@Success = 0,
					@SyncTable = @TableName,
					@Command = @exec_str,
					@Results = @ErrMsg;

				--need to reset the identity stuff on the connection
				SELECT @exec_str = dbo.DataGeneratorIdentityStatement (@TableName,'OFF',0) 
				EXEC (@exec_str);
			END CATCH 
		END;

		FETCH NEXT FROM curForEachTable INTO @TableName,@ColsToSkip;
	END
	CLOSE curForEachTable;
	DEALLOCATE curForEachTable;
	
	EXEC DataReplicatorConnector @Action='DESTROY';

	IF @error = 1 
	BEGIN
		RAISERROR ('An error occurred during DataReplicator.  Search for ERROR in the PRINT statements above.',16,1);
		RETURN @error;
	END;

END;
GO

IF EXISTS (select * from sys.objects where object_id = object_id('DataReplicatorConnector'))
	DROP PROCEDURE DataReplicatorConnector;
GO
CREATE PROCEDURE DataReplicatorConnector (
	 @Action varchar(200) --CREATE/DESTROY
	,@DataSource varchar(2000) = NULL --swipeK12 server
) 
WITH ENCRYPTION
AS
BEGIN
	/*
		This procedure sets up and destroys the linked server back to the SwipeK12 home.  
		This should be deployed in the local swipestation
	*/

	DECLARE @LinkedServerName varchar(2000);
	SELECT @LinkedServerName = 'DataReplicatorConnector';

	IF @Action = 'DESTROY'
	BEGIN
		IF EXISTS (
			SELECT * FROM sys.servers 
			WHERE name = @LinkedServerName
		)
		BEGIN
			EXEC master..sp_droplinkedsrvlogin @rmtsrvname = @LinkedServerName, @locallogin = NULL;
			EXEC master..sp_dropserver @server = @LinkedServerName;
		END;

		RETURN 0;

	END;

	--this block is for CREATE
	--cleanup...just in case
	EXEC DataReplicatorConnector @Action = 'DESTROY';

	--build linked server
	EXEC sp_addlinkedserver 
		@server = @LinkedServerName,
		@srvproduct = N'DoesNotMatter',
		@provider = N'SQLNCLI', 
		@datasrc = @DataSource,
		@provstr = N'Encrypt=yes;TrustServerCertificate=yes;';
	EXEC sp_serveroption 
		@server = @LinkedServerName, 
		@optname = N'remote proc transaction promotion', 
		@optvalue = 'FALSE';  --When FALSE calling a remote stored procedure does NOT start a distributed transaction
	EXEC sp_serveroption 
		@server = @LinkedServerName, 
		@optname = N'rpc', 
		@optvalue = 'TRUE'; 
	EXEC sp_serveroption 
		@server = @LinkedServerName, 
		@optname = N'RPC OUT', 
		@optvalue = 'TRUE'; 
	EXEC sp_serveroption 
		@server = @LinkedServerName, 
		@optname=N'collation compatible', 
		@optvalue=N'false';
	EXEC sp_serveroption 
		@server = @LinkedServerName, 
		@optname=N'data access', 
		@optvalue=N'true';
	EXEC sp_serveroption 
		@server = @LinkedServerName, 
		@optname=N'connect timeout', 
		@optvalue=N'0';
	EXEC sp_serveroption 
		@server = @LinkedServerName, 
		@optname=N'collation name', 
		@optvalue= null;
	EXEC sp_serveroption 
		@server = @LinkedServerName, 
		@optname=N'query timeout', 
		@optvalue=N'0';
	EXEC sp_serveroption 
		@server = @LinkedServerName, 
		@optname=N'use remote collation', 
		@optvalue=N'true';
	EXEC master.dbo.sp_addlinkedsrvlogin
		@rmtsrvname = @LinkedServerName,
		@useself = 'false',
		@locallogin = NULL,
		@rmtuser = 'etl-jobs',
		@rmtpassword = 'ETL123$$$'; --'678#@4sw';	
END;
GO

/*
Tests:
	EXEC DataReplicatorConnector @Action='DESTROY';
	EXEC DataReplicatorConnector @Action='CREATE', @DataSource = '.\SQL2014';
	EXEC DataReplicatorConnector @Action='DESTROY';

*/

IF EXISTS (SELECT * FROM sysobjects WHERE id = object_id('DataGeneratorGetSchoolIDs'))
BEGIN
    DROP FUNCTION DataGeneratorGetSchoolIDs
END
GO
CREATE FUNCTION [dbo].[DataGeneratorGetSchoolIDs](@SchoolID INT = NULL)
RETURNS varchar(max)
AS
BEGIN
    declare @output varchar(max)
    select @output = COALESCE(@output + ',', '') + convert(varchar(200),SchoolID)
    from School
	WHERE SchoolID = @SchoolID OR @SchoolID IS NULL
    return @output
END;
GO


IF EXISTS (SELECT * FROM sysobjects WHERE type in ('P') and id = object_id('ScriptGenerator'))
     BEGIN
          DROP PROCEDURE ScriptGenerator
     END
GO

CREATE PROC ScriptGenerator
(
	@table_schema varchar(776),  	-- The table/view schema name
	@table_name varchar(776),  		-- The table/view for which the INSERT statements will be generated using the existing data
	@output nvarchar(max) OUTPUT,
	@ColsToSkip varchar(4000) = '',	--some tables on the server are missing cols that are on the swipestation db.  ie, Students.FreeLunch
	@SyncType varchar(200),			--determines whether LastUpdated is part of the WHERE clause
	@SchoolID int	= NULL			--when passed then only sync for that School, else all schools in dbo.School
	
)
AS
BEGIN


/*

Usage
------------
	This procedure can be called like this, at a minimum:

	DECLARE @Something nvarchar(max)
	EXEC dbo.ScriptGenerator
		@table_schema = 'dbo',
		@table_name = 'Students',
		@output = @Something OUTPUT;
	SELECT @Something
*/


SET NOCOUNT ON


--Variable declarations
DECLARE		
		@Column_ID int, 		
		@Column_List varchar(8000),
		@Update_Column_List varchar(max),
		@Column_Name varchar(128)
		,@RemoteServerString VARCHAR(255)
		,@IsKey bit;

SELECT @RemoteServerString = 'DataReplicatorConnector.SwipeK12.';
SELECT @ColsToSkip = COALESCE(@ColsToSkip,'');

--Variable Initialization
SET @Column_ID = 0
SET @Column_Name = ''
SET @Column_List = ''
SET @Update_Column_List = ''


--To get the first column's ID
SELECT	@Column_ID = MIN(ORDINAL_POSITION) 	
FROM	INFORMATION_SCHEMA.COLUMNS (NOLOCK) 
WHERE 	TABLE_NAME = @table_name 
AND TABLE_SCHEMA = @table_schema

--Loop through all the columns of the table, to get the column names and their data types
WHILE @Column_ID IS NOT NULL
	BEGIN
		SELECT 	@Column_Name = QUOTENAME(COLUMN_NAME)
		FROM 	INFORMATION_SCHEMA.COLUMNS (NOLOCK) 
		WHERE 	ORDINAL_POSITION = @Column_ID 
		AND TABLE_NAME = @table_name 
		AND TABLE_SCHEMA = @table_schema;
		

		--Generating the column list for the INSERT statement
		SET @Column_List = @Column_List +  @Column_Name + ','	
		
		--Generating the column list for the UPDATE statement
		--if the col is part of the key, do nothing
		SELECT @IsKey = dbo.DataGeneratorIsColumnPartOfKey (@table_name,@Column_Name)
		IF ( @IsKey = 0)
		BEGIN
			SET @Update_Column_List = @Update_Column_List + 'targ.' + @Column_Name + ' = src.' + @Column_Name + '
			,' 
		END;

		--we do not sync the "synced" cols.  Also, skip anything that does not exist on the server.  
		SELECT 	@Column_ID = MIN(ORDINAL_POSITION) 
		FROM 	INFORMATION_SCHEMA.COLUMNS (NOLOCK) 
		WHERE 	TABLE_NAME = @table_name 
		AND ORDINAL_POSITION > @Column_ID 
		AND TABLE_SCHEMA = @table_schema
		AND COLUMN_NAME <> 'synced'
		AND CHARINDEX(COLUMN_NAME,@ColsToSkip) = 0;
		--AND COLUMN_NAME NOT IN (@ColsToSkip);


	--Loop ends here!
	END

--To get rid of the extra characters that got concatenated during the last run through the loop
SET @Column_List = LEFT(@Column_List,len(@Column_List) - 1)
SET @Update_Column_List = LEFT(@Update_Column_List,len(@Update_Column_List) - 1)
--select @Column_List, @Update_Column_List

--begin emitting data
SELECT @output = '
---------------------------------------------------------------------------------
SET NOCOUNT ON

--build a base temp table
SELECT ' + @Column_List + '
INTO #' + @table_name + '
FROM ' + quotename(@table_schema) + '.' + quotename (@table_name) + '
WHERE 1 = 0;

--copy the data from SwipeK12 server
'
+ dbo.DataGeneratorIdentityStatement (@table_name,'ON',1) + '
INSERT INTO #' + @table_name + '(' + @Column_List + ')
' 
+ 
CASE @ColsToSkip 
	WHEN 'CUSTOMLunchSched' THEN '
SELECT Distinct
	cl.SchoolID, ssch.StudentID, cl.ClassName + '' Per: '' + per.PerName as LunchPeriod, 
	(ssch.active & 
	cl.active &
	css.active) AS Active, ssch.lastupdate AS LastUpdate
FROM ' + @RemoteServerString + 'dbo.classschedule css 
JOIN ' + @RemoteServerString + 'dbo.classes cl ON css.classid= cl.classid
JOIN ' + @RemoteServerString + 'dbo.studentschedule ssch ON ssch.scheduleid = css.scheduleid and ssch.active = 1
JOIN ' + @RemoteServerString + 'dbo.period per ON per.periodid = css.periodid
WHERE (cl.classname like ''%lunch%'' or per.pername like ''%lua%'') 
AND cl.SchoolID IN (' + dbo.DataGeneratorGetSchoolIDs(@SchoolID) + ') 
GROUP BY cl.schoolid , ssch.studentid, 
	(cl.classname + '' Per: '' + per.pername), 
	ssch.lastupdate,
	(ssch.active & 
	cl.active &
	css.active);delete FROM [dbo].[LunchSched] ' 
	WHEN 'CUSTOMPersonEpisode' THEN '

select pe.personepisodeid,pe.personid,episodetypeid,episodestarttime,episodenotes,personoutcomeid,outcomedays,pe.lastupdated FROM ' + @RemoteServerString + 'dbo.PersonEpisodeBySchool pe 
where pe.SchoolID IN (' + dbo.DataGeneratorGetSchoolIDs(@SchoolID) + ') ' 
	WHEN 'CUSTOMPersonDayAttend' THEN '

select pda.StudentAttendID,pda.EntryDate,pda.SchoolDayID,pda.StudentID,pda.AttStatusID,pda.ExcuseID,pda.ExcuseValid,0 as DayLateMin,pda.SchoolID,pda.PicturePath,pda.LastUpdated FROM ' + @RemoteServerString + 'dbo.ReplicatorPersonDayAttend pda 
where pda.SchoolID IN (' + dbo.DataGeneratorGetSchoolIDs(@SchoolID) + ') AND pda.StatusCd <> ''ABS'' ' 
	WHEN 'CUSTOMPersonOutcome' THEN '

select po.personoutcomeid,po.personid,outcometypeid,outcomedays,alerttext,startdate,enddate,po.active,location,po.lastupdated,0,FineAmt,PaidAmt FROM ' + @RemoteServerString + 'dbo.PersonOutcomeBySchool po
where po.SchoolID IN (' + dbo.DataGeneratorGetSchoolIDs(@SchoolID) + ') ' 
	WHEN 'CUSTOMAttendStatus' THEN '

	select AttendStatID,SchoolID,StatusName,StatusCd,OnTime,Present,ReqExcuse,LastUpdated FROM ' + @RemoteServerString + 'dbo.StationAttendStatus a
	where a.SchoolID IN (' + dbo.DataGeneratorGetSchoolIDs(@SchoolID) + ') ' 
	WHEN 'CUSTOMStaff' THEN '

	select [PersonID],[SchoolID],[DateHired],[Department],[EmployeeNumber],[Title],0 as PhotoPath,0 as HomeRoom,Department as OfficeLocation FROM ' + @RemoteServerString + 'dbo.StaffBySchool s
	where s.SchoolID IN (' + dbo.DataGeneratorGetSchoolIDs(@SchoolID) + ') ' 
	ELSE '
SELECT ' + @Column_List + '
FROM ' + @RemoteServerString + quotename(@table_schema) + '.' + quotename (@table_name) + '
WHERE SchoolID IN (' + dbo.DataGeneratorGetSchoolIDs(@SchoolID) + ')' 
END 
+ 

--add the filter condition on LastUpdated for transactional tables
CASE @SyncType 
	WHEN 'Snapshot'			THEN ''		--do nothing
	WHEN 'Resync'			THEN ''		--do nothing, we'll get everything
	WHEN 'Transactional'	THEN ' AND LastUpdated >= ''' + convert (varchar(200),
					(
						--get the last successful run for the table, add 1 hour for safety, and basically sync everything if this is the first run
						SELECT DATEADD(HOUR,-1,COALESCE(MAX(SyncDateTime),'1/1/1980')) 
						FROM DataReplicatorLog 
						WHERE SyncTable = @table_name
						AND Success = 1
					)) + ''''
	
END
+
';
'
+ dbo.DataGeneratorIdentityStatement (@table_name,'OFF',1) + '

' +
+ dbo.DataGeneratorIdentityStatement (@table_name,'ON',0) + '

SET NOCOUNT OFF
;MERGE INTO ' + quotename(@table_schema) + '.' + quotename (@table_name) + ' AS targ
USING (
	SELECT ' + @Column_List + '
	FROM #' + @table_name + '
) AS src 
' +
dbo.DataGeneratorGetKeystring (@Table_Name) + '
WHEN NOT MATCHED BY TARGET THEN
	INSERT ( 
	' + @Column_List + '
	) 
	VALUES (
	' + @Column_List + '
	)
WHEN MATCHED THEN 
	UPDATE SET
		' + @Update_Column_List + '
;' 
+ dbo.DataGeneratorIdentityStatement (@table_name,'OFF',0) + '


---------------------------------------------------------------------------------
'

SET NOCOUNT OFF
RETURN 0 
END
GO


IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'DataGeneratorGetKeystring'))
  DROP FUNCTION DataGeneratorGetKeystring
GO
CREATE FUNCTION DataGeneratorGetKeystring
(
  @TblName     SYSNAME
)
RETURNS VARCHAR(MAX)
AS
BEGIN
  DECLARE
      @ColList VARCHAR(MAX),
      @FullName SYSNAME

  SELECT @FullName = 'dbo.' + @TblName

  SELECT  @ColList =
    (SELECT CASE
              WHEN sic.key_ordinal > 1 THEN ' AND targ.'
              --ELSE ''
			  WHEN sic.key_ordinal = 1 THEN 'ON targ.'
            END 
			+
            sc.name 
			+
			' = src.' 
			+
			sc.name 
       FROM sys.tables st 
       JOIN sys.indexes si
         ON st.object_id = si.object_id
       JOIN sys.index_columns sic
         ON st.object_id = sic.object_id
        AND si.index_id = sic.index_id 
       JOIN sys.columns sc 
         ON st.object_id = sc.object_id
        AND sic.column_id = sc.column_id 
      WHERE st.object_id = OBJECT_ID(@FullName)
        AND si.is_primary_key = 1
   ORDER BY sic.key_ordinal  
     FOR XML PATH(''))

  IF @ColList IS NULL AND @TblName = 'LunchSched'
  BEGIN
	SELECT @ColList = 'ON targ.StudentID = src.StudentID AND targ.SchoolID = src.SchoolID AND targ.Active = src.Active AND targ.LunchPeriod = src.LunchPeriod'
  END;

  RETURN @ColList
END
GO

IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'DataGeneratorIsColumnPartOfKey'))
  DROP FUNCTION DataGeneratorIsColumnPartOfKey
GO
CREATE FUNCTION DataGeneratorIsColumnPartOfKey
(
  @TblName     SYSNAME,
  @ColumnName  varchar(200)
)
RETURNS BIT
AS
BEGIN
  DECLARE
      @ColList VARCHAR(MAX),
      @FullName SYSNAME,
	  @output bit 

	  SELECT @output = 0;
	  SELECT @ColumnName = REPLACE(REPLACE(@ColumnName,'[',''),']','');

  SELECT @FullName = 'dbo.' + @TblName;

  IF EXISTS (
    SELECT 1
       FROM sys.tables st 
       JOIN sys.indexes si
         ON st.object_id = si.object_id
       JOIN sys.index_columns sic
         ON st.object_id = sic.object_id
        AND si.index_id = sic.index_id 
       JOIN sys.columns sc 
         ON st.object_id = sc.object_id
        AND sic.column_id = sc.column_id 
      WHERE st.object_id = OBJECT_ID(@FullName)
        AND si.is_primary_key = 1
		AND sc.name = @ColumnName
	)
	BEGIN 
		SELECT @output = 1;
	END
	ELSE IF (@TblName = 'LunchSched' AND @ColumnName IN ('StudentID','SchoolID','Active','LunchPeriod'))
	BEGIN
		SELECT @output = 1
	END;

  RETURN @output
END
GO
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'DataGeneratorIdentityStatement'))
  DROP FUNCTION DataGeneratorIdentityStatement
GO
CREATE FUNCTION DataGeneratorIdentityStatement
(
  @TblName     SYSNAME,
  @Position		varchar(3),
  @ForTempTable bit
)
RETURNS varchar(500)
AS
BEGIN
  DECLARE
      @ColList VARCHAR(MAX),
      @FullName SYSNAME,
	  @output varchar(500); 

	  SELECT @output = '';


BEGIN
  IF EXISTS (
    SELECT 1
       FROM sys.tables st 
       JOIN sys.columns sc 
         ON st.object_id = sc.object_id
      WHERE st.object_id = OBJECT_ID(@TblName)
        AND sc.is_identity = 1
	)
	BEGIN 
		IF @Position = 'OFF'
			SELECT @output = 'SET IDENTITY_INSERT ' + CASE @ForTempTable WHEN 0 THEN '' ELSE '#' END + @TblName + ' OFF;'

		IF @Position = 'ON'
			SELECT @output = 'SET IDENTITY_INSERT ' + CASE @ForTempTable WHEN 0 THEN '' ELSE '#' END + @TblName + ' ON;'
			 
	END;
END

  RETURN @output
END
GO

IF NOT EXISTS (select * from sys.objects where object_id = object_id('dbo.DataReplicatorLog'))
BEGIN
	CREATE TABLE DataReplicatorLog (
		DataReplicatorLogID bigint NOT NULL PRIMARY KEY IDENTITY (1,1),
		SyncDateTime DATETIME NOT NULL,
		ElapsedMS int NULL, 
		SyncType varchar(200) NOT NULL,
		Success bit NOT NULL,
		SyncTable varchar(200) NOT NULL,
		Command varchar(4000) NULL, 
		Results varchar(4000) NULL
	);

END;

GO

IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'DataGeneratorLogger'))
  DROP PROCEDURE DataGeneratorLogger
GO
CREATE PROCEDURE DataGeneratorLogger
(
	@LogStartTime DATETIME,
	@SyncType varchar(200),
	@Success bit,
	@SyncTable varchar(200),
	@Command varchar(4000),
	@Results varchar(4000)
)
AS
BEGIN
	SET NOCOUNT ON

	--Duration calculation
	DECLARE @Duration INT;

	SELECT @Duration = datediff(MILLISECOND,@LogStartTime,getdate())

	INSERT INTO DataReplicatorLog (SyncDateTime,ElapsedMS,SyncType,Success,SyncTable,Command,Results)
	VALUES (getdate(),@Duration,@SyncType,@Success,@SyncTable,@Command,@Results);

	SET NOCOUNT OFF
END
GO