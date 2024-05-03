@powershell -NoProfile -ExecutionPolicy unrestricted -File SetPermissions.ps1

sqlcmd -S localhost -i "Install-Db.sql"