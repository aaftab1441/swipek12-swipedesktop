use master
GO

DECLARE @dbname nvarchar(128)
SET @dbname = N'SwipeDesktop'

--IF EXISTS(select * from master.sys.databases where name='SwipeDesktop')
--ALTER DATABASE SwipeDesktop SET EMERGENCY;
--GO


IF EXISTS(select * from master.sys.databases where name='SwipeDesktop')
DROP DATABASE SwipeDesktop;
GO
--USE master
--IF (EXISTS (SELECT name FROM sys.databases WHERE ('[' + name + ']' = @dbname OR name = @dbname)))
--DROP DATABASE SwipeDesktop;
--exec sp_detach_db 'SwipeDesktop';

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
--exec sp_attach_single_file_db  'SwipeDesktop', @dbpath
GO

/*
use master
Go

CREATE LOGIN SwipeDesktopUser WITH PASSWORD = 'sW!peD3skt0p'
GO

EXEC sp_addsrvrolemember 'SwipeDesktopUser', 'sysadmin';
GO

Use SwipeDesktop;
GO

IF NOT EXISTS (SELECT * FROM sys.database_principals WHERE name = N'SwipeDesktopUser')
BEGIN
    CREATE USER [SwipeDesktopUser]FOR LOGIN [SwipeDesktopUser]
    EXEC sp_addrolemember N'db_owner', N'SwipeDesktopUser'
END;
GO

alter authorization on database::SwipeDesktop to [SwipeDesktopUser];
*/