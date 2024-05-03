USE [SwipeDesktop]
GO

/****** Object:  Table [dbo].[StudentLunchTable]    Script Date: 2/18/2019 8:05:31 AM ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[StudentStartTime](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[SchoolId] [int] NOT NULL,
	[StudentNumber] [varchar](20) NULL,
	[StudentId] [int] NOT NULL,
	[StartTime] [varchar](15) NULL,
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO

ALTER TABLE [dbo].[StudentStartTime] ADD  DEFAULT (NEXT VALUE FOR [dbo].[StudentStartTime_seq]) FOR [Id]
GO


