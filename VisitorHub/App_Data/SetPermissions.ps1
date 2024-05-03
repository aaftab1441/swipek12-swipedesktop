$arch = [IntPtr]::Size
$PSScriptRoot = Split-Path -Parent -Path $MyInvocation.MyCommand.Definition
#$arch

$InheritanceFlag = [System.Security.AccessControl.InheritanceFlags]::None
$PropagationFlag = [System.Security.AccessControl.PropagationFlags]::None
$objType = [System.Security.AccessControl.AccessControlType]::Allow 

$PSScriptRoot

$installFolder = Split-Path -Path $PSScriptRoot -Parent

$logFolder = $installFolder + "\Logs"

$logFolder

$db = $PSScriptRoot + "\SwipeDesktop.mdf"
$db_log = $PSScriptRoot + "\SwipeDesktop_log.ldf"

#$db = "C:\Program Files\SwipeK12\SwipeDesktop\App_Data\SwipeDesktop.mdf"
#$db_log = "C:\Program Files\SwipeK12\SwipeDesktop\App_Data\SwipeDesktop_log.ldf"

#If($arch -eq 8)
#{
#    $db = "C:\Program Files (x86)\SwipeK12\SwipeDesktop\App_Data\SwipeDesktop.mdf"
#    $db_log = "C:\Program Files (x86)\SwipeK12\SwipeDesktop\App_Data\SwipeDesktop_log.ldf"
#}

$db
$db_log

$group = "BUILTIN\Users"
$user = [Environment]::UserDomainName + "\" + [Environment]::UserName

$group
$user

#$acl = Get-Acl $db
$acl = (Get-Item $db).GetAccessControl('Access')

#Current User
$permission = $user,"FullControl", $InheritanceFlag, $PropagationFlag, $objType
$accessRule = New-Object System.Security.AccessControl.FileSystemAccessRule $permission

$acl.SetAccessRule($accessRule)

#All Users Group
$gPermission = $group,"FullControl", $InheritanceFlag, $PropagationFlag, $objType
$gAccessRule = New-Object System.Security.AccessControl.FileSystemAccessRule $gPermission

#$acl.SetAccessRule($gAccessRule)
#(Get-Item $db).SetAccessControl($acl)

#log folder permissions
$acl = (Get-Item $logFolder).GetAccessControl('Access')
$acl.SetAccessRule($gAccessRule)
(Get-Item $logFolder).SetAccessControl($acl)
#Set-Acl $db $acl

#$acl = Get-Acl $db_log
$acl = (Get-Item $db_log).GetAccessControl('Access')

#Current User
$permission = $user,"FullControl", $InheritanceFlag, $PropagationFlag, $objType
$accessRule = New-Object System.Security.AccessControl.FileSystemAccessRule $permission

$acl.SetAccessRule($accessRule)

#All Users Group
$gPermission = $group,"FullControl", $InheritanceFlag, $PropagationFlag, $objType
$gAccessRule = New-Object System.Security.AccessControl.FileSystemAccessRule $gPermission

#$acl.SetAccessRule($gAccessRule)
#(Get-Item $db_log).SetAccessControl($acl)
#Set-Acl $db_log $acl

Import-Module SqlPs
Invoke-Sqlcmd -ServerInstance localhost\sqlexpress -InputFile "Install-Db.sql" 