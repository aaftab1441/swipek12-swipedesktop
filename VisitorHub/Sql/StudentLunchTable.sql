USE [SwipeDesktop]
GO

/****** Object:  Table [dbo].[StudentLunchTable]    Script Date: 2/18/2019 8:05:31 AM ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[StudentLunchTable](
	[Id] [int] NOT NULL,
	[StudentId] [int] NOT NULL,
	[Day] [varchar](10) NULL,
	[RoomName] [varchar](20) NULL,
	[Period] [varchar](15) NULL,
	[StartTime] [datetime] NULL,
	[EndTime] [datetime] NULL,
	[LunchKey] [varchar](25) NULL,
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO

ALTER TABLE [dbo].[StudentLunchTable] ADD  DEFAULT (NEXT VALUE FOR [dbo].[seq_LunchTimeTableId]) FOR [Id]
GO

