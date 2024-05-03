@Echo OFF
Echo Launch dir: "%~dp0"
Echo Current dir: "%CD%"
CD /d %~dp0

@powershell -NoProfile -ExecutionPolicy unrestricted -File SetPermissions.ps1
Pause

REM sqlcmd -S localhost\sqlexpress -i "Install-Db.sql"