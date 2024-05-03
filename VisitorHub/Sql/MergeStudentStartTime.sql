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
CREATE PROCEDURE [dbo].[MergeStudentStartTimeSync]
	-- Add the parameters for the stored procedure here
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

    -- Insert statements for procedure here
	MERGE StudentStartTime AS T
	USING StudentStartTime_Sync AS S
	ON (T.studentid = S.studentid) 
	WHEN NOT MATCHED BY TARGET
		THEN INSERT(Id, SchoolId, StudentNumber, StudentId, StartTime) 
		VALUES(S.Id, S.SchoolId, S.StudentNumber, S.StudentId, S.StartTime)
	WHEN MATCHED 
		THEN UPDATE SET T.Id = S.Id
	OUTPUT $action, inserted.*, deleted.*;
END
GO


