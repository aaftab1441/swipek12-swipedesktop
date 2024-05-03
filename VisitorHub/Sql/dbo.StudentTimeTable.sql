CREATE TABLE [dbo].[StudentTimeTable]
(
	[Id] INT identity(1,1) NOT NULL PRIMARY KEY,
	[StudentId] int not null,
	[Day] varchar(10),
	[RoomName] varchar(20),
	[Period] varchar(15),
	[StartTime] datetime,
	[EndTime] datetime
	--CONSTRAINT [PK_StudentTimeTable] PRIMARY KEY CLUSTERED ([Id] ASC)
)
