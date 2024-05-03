USE [SwipeDesktop]
GO

/****** Object:  Table [dbo].[SchoolStartTimes]    Script Date: 2/18/2019 10:16:17 AM ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[SchoolStartTimes](
	[SchoolID] [int] NOT NULL,
	[Grade] [varchar](10) NOT NULL,
	[StartTime] [datetime] NOT NULL,
 CONSTRAINT [PK_SchoolStartTimes] PRIMARY KEY CLUSTERED 
(
	[Grade] ASC,
	[SchoolID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, FILLFACTOR = 70) ON [PRIMARY]
) ON [PRIMARY]
GO


CREATE TABLE [dbo].[SchoolStartTimes_Sync](
	[SchoolID] [int] NOT NULL,
	[Grade] [varchar](10) NOT NULL,
	[StartTime] [datetime] NOT NULL,
 CONSTRAINT [PK_SchoolStartTimes_Sync] PRIMARY KEY CLUSTERED 
(
	[Grade] ASC,
	[SchoolID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, FILLFACTOR = 70) ON [PRIMARY]
) ON [PRIMARY]
GO

