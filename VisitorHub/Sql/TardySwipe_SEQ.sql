DROP Table  [dbo].[TardySwipe]
GO

CREATE SEQUENCE dbo.seq_TardySwipeId START WITH 1 INCREMENT BY 1
GO

CREATE TABLE [dbo].[TardySwipe](
	TardySwipeId int NOT NULL 
        DEFAULT (NEXT VALUE FOR dbo.seq_TardySwipeId)
        PRIMARY KEY CLUSTERED,
	[SchoolId] [int] NOT NULL,
	[StudentId] [int] NOT NULL,
	[SwipeTime] [datetime] NOT NULL,
	[Location] [varchar](50) NOT NULL,
	[Period] [varchar](10) NOT NULL,
	[AttendanceCode] [varchar](20) NULL,
	[Source] [varchar](50) NOT NULL,
	[LastUpdated] [datetime] NOT NULL
) ON [PRIMARY]

GO