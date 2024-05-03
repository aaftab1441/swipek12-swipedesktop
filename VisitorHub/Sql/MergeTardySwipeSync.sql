-- ================================================
-- Template generated from Template Explorer using:
-- Create Procedure (New Menu).SQL
--
-- Use the Specify Values for Template Parameters 
-- command (Ctrl-Shift-M) to fill in the parameter 
-- values below.
--
-- This block of comments will not be included in
-- the definition of the procedure.
-- ================================================
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- =============================================
-- Author:		<Author,,Name>
-- Create date: <Create Date,,>
-- Description:	<Description,,>
-- =============================================
CREATE PROCEDURE MergeTardySwipeSync
	-- Add the parameters for the stored procedure here
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

    -- Insert statements for procedure here
	MERGE TardySwipe AS T
	USING TardySwipeSync AS S
	ON (T.studentid = S.studentid AND T.Location = S.Location AND dbo.shortdate(T.swipetime) = dbo.shortdate(S.swipetime)) 
	WHEN NOT MATCHED BY TARGET --AND S.EmployeeName LIKE 'S%' 
		THEN INSERT(TardySwipeId, SchoolId, StudentId, SwipeTime, Location, Period, AttendanceCode, Source, LastUpdated) 
		VALUES(S.TardySwipeId, S.SchoolId, S.StudentId, S.SwipeTime, S.Location, S.Period, S.AttendanceCode, S.Source, S.LastUpdated)
	WHEN MATCHED 
		THEN UPDATE SET T.TardySwipeId = S.TardySwipeId
	--WHEN NOT MATCHED BY SOURCE AND T.EmployeeName LIKE 'S%'
	--   THEN DELETE 
	OUTPUT $action, inserted.*, deleted.*;
END
GO
