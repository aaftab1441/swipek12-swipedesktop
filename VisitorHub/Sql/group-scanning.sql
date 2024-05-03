USE [SwipeDesktop]
GO

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

drop table StudentGroups
go

drop table Groups

go

CREATE TABLE [dbo].[Groups](
	[GroupId] [int] NOT NULL,
	[SchoolId] [int] NOT NULL,
	[GroupCode] [varchar](50) NULL,
	[GroupName] [varchar](255) NOT NULL,
	[GroupType] [varchar](25) NULL,
	[IsPrivate] [varchar](1) NULL,
	[LastUpdated] [datetime] NULL,
 CONSTRAINT [PK_Groups] PRIMARY KEY CLUSTERED 
(
	[GroupId] ASC,
	[SchoolId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, FILLFACTOR = 70) ON [PRIMARY]
) ON [PRIMARY]

GO

CREATE TABLE [dbo].[PersonGroups](
	[Id] [int] NOT NULL,
	[GroupId] [int] NOT NULL,
	[PersonId] [int] NOT NULL,
	[LastUpdated] [datetime] NOT NULL,
 CONSTRAINT [PK_PersonGroups] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, FILLFACTOR = 70) ON [PRIMARY]
) ON [PRIMARY]

GO