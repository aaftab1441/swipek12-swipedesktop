USE [SwipeDesktop]
GO

/****** Object:  Table [dbo].[StudentLunchTable]    Script Date: 2/18/2019 8:05:31 AM ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO


CREATE SEQUENCE dbo.[seq_AuditTableId] START WITH 1 INCREMENT BY 1
GO

CREATE TABLE [dbo].[AuditTable](
	[Id] [int] NOT NULL,
	[AuditTime] [datetime] NULL,
	[Type] [varchar](25) NULL,
	[Data] [varchar](2500) NULL
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO

ALTER TABLE [dbo].[AuditTable] ADD  DEFAULT (NEXT VALUE FOR [dbo].[seq_AuditTableId]) FOR [Id]
GO





