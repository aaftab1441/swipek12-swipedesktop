USE [SwipeDesktop]
GO

/****** Object:  StoredProcedure [dbo].[MergeStudentLunchTableSync]    Script Date: 2/18/2019 8:19:03 AM ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

-- =============================================
-- Author:		<Author,,Name>
-- Create date: <Create Date,,>
-- Description:	<Description,,>
-- =============================================
CREATE PROCEDURE [dbo].[MergeStudentLunchTableSync]
	-- Add the parameters for the stored procedure here
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

    -- Insert statements for procedure here
	MERGE StudentLunchTable AS T
	USING StudentLunchTable_Sync AS S
	ON (T.studentid = S.studentid AND T.LunchKey = S.LunchKey /*AND dbo.shortdate(T.swipetime) = dbo.shortdate(S.swipetime)*/) 
	WHEN NOT MATCHED BY TARGET
		THEN INSERT(Id, StudentId, [Day], RoomName, Period, StartTime, EndTime, LunchKey) 
		VALUES(S.Id, S.StudentId, S.Day, S.RoomName, S.Period, S.StartTime, S.EndTime, S.LunchKey)
	WHEN MATCHED 
		THEN UPDATE SET T.Id = S.Id
	--WHEN NOT MATCHED BY SOURCE AND T.EmployeeName LIKE 'S%'
	--   THEN DELETE 
	OUTPUT $action, inserted.*, deleted.*;
END
GO


