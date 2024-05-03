USE [SwipeDesktop]
GO

/****** Object:  Table [dbo].[TardySwipe]    Script Date: 2/21/2018 5:34:15 PM ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[TardySwipeSync](
	[TardySwipeId] [int] NOT NULL,
	[SchoolId] [int] NOT NULL,
	[StudentId] [int] NOT NULL,
	[SwipeTime] [datetime] NOT NULL,
	[Location] [varchar](50) NOT NULL,
	[Period] [varchar](10) NOT NULL,
	[AttendanceCode] [varchar](20) NULL,
	[Source] [varchar](50) NOT NULL,
	[LastUpdated] [datetime] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[TardySwipeId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

GO

--ALTER TABLE [dbo].[TardySwipe] ADD  DEFAULT (NEXT VALUE FOR [dbo].[seq_TardySwipeId]) FOR [TardySwipeId]
--GO


