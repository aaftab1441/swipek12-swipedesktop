CREATE TABLE [dbo].[TardySwipe] (
    [TardySwipeId]   INT          IDENTITY (1, 1) NOT NULL,
    [SchoolId]       INT          NOT NULL,
    [StudentId]      INT          NOT NULL,
    [SwipeTime]      DATETIME     NOT NULL,
    [Location]       VARCHAR (50) NOT NULL,
    [Period]         VARCHAR (10) NOT NULL,
    [AttendanceCode] VARCHAR (5)  NOT NULL,
    [Source]         VARCHAR (50) NOT NULL,
    [LastUpdated]    DATETIME     NOT NULL,
    CONSTRAINT [PK_TardySwipe] PRIMARY KEY CLUSTERED ([TardySwipeId] ASC)
);