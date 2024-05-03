use master
go

DECLARE @dbname nvarchar(128)
SET @dbname = N'SwipeDesktop'

IF (EXISTS (SELECT name FROM master.dbo.sysdatabases WHERE ('[' + name + ']' = @dbname OR name = @dbname)))
exec sp_detach_db 'SwipeDesktop';
go

DECLARE @dbpath nvarchar(128)
DECLARE @logpath nvarchar(128)
set @dbpath = 'C:\Program Files\SwipeK12\SwipeDesktop\App_Data\SwipeDesktop.mdf'
set @logpath = 'C:\Program Files\SwipeK12\SwipeDesktop\App_Data\SwipeDesktop_log.ldf'

DECLARE @arch varchar(128)
SELECT @arch = Convert(varchar(128), SERVERPROPERTY('Edition'))

if(@arch like '%64-bit%')
begin
print @arch
print 'Installing'
set @dbpath = 'C:\Program Files (x86)\SwipeK12\SwipeDesktop\App_Data\SwipeDesktop.mdf'
set @logpath = 'C:\Program Files (x86)\SwipeK12\SwipeDesktop\App_Data\SwipeDesktop_log.ldf'
print @dbpath
print @logpath
end

exec sp_attach_db 'SwipeDesktop', @dbpath, @logpath
GO
CREATE LOGIN SwipeDesktopUser WITH PASSWORD = 'sW!peD3skt0p', CHECK_POLICY = OFF, CHECK_EXPIRATION = OFF;
GO

Use SwipeDesktop

GO
IF NOT EXISTS (SELECT * FROM sys.database_principals WHERE name = N'SwipeDesktopUser')
BEGIN

CREATE USER [SwipeDesktopUser] FOR LOGIN [SwipeDesktopUser]
EXEC sp_addrolemember 'db_datareader', N'SwipeDesktopUser'
EXEC sp_addrolemember 'db_datawriter', N'SwipeDesktopUser'

END
GO

use master
Go


/*
use SwipeDesktop
go

DECLARE @sys_usr char(30);
SET @sys_usr = SYSTEM_USER;
--print @sys_usr

EXEC sp_addrolemember N'db_datareader', @sys_usr

EXEC sp_addrolemember N'db_datawriter', @sys_usr
go*/