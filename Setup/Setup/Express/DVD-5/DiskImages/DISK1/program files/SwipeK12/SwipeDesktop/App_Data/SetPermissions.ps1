$arch = [IntPtr]::Size

#$arch

$InheritanceFlag = [System.Security.AccessControl.InheritanceFlags]::None
$PropagationFlag = [System.Security.AccessControl.PropagationFlags]::None
$objType = [System.Security.AccessControl.AccessControlType]::Allow 

#$PSScriptRoot

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

$user = [Environment]::UserDomainName + "\" + [Environment]::UserName

$acl = Get-Acl $db
$permission = $user,"FullControl", $InheritanceFlag, $PropagationFlag, $objType
$accessRule = New-Object System.Security.AccessControl.FileSystemAccessRule $permission

$acl.SetAccessRule($accessRule)
Set-Acl $db $acl

$acl = Get-Acl $db_log
$permission = $user,"FullControl", $InheritanceFlag, $PropagationFlag, $objType
$accessRule = New-Object System.Security.AccessControl.FileSystemAccessRule $permission

$acl.SetAccessRule($accessRule)
Set-Acl $db_log $acl

Invoke-Sqlcmd -InputFile "C:\MyFolder\TestSQLCmd.sql" 