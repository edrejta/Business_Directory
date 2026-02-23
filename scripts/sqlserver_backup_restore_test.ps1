param(
  [string]$ContainerName = "business-directory-sqlserver",
  [string]$DatabaseName = "BusinessDirectory",
  [string]$SaPassword = "Your_strong_password123!"
)

$ErrorActionPreference = "Stop"

$backupDir = "/var/opt/mssql/backup"
$backupFile = "$backupDir/${DatabaseName}.bak"

function SqlCmd([string]$query) {
  docker exec $ContainerName /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P $SaPassword -d $DatabaseName -C -Q $query -W -h -1
}

function SqlCmdMaster([string]$query) {
  docker exec $ContainerName /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P $SaPassword -d master -C -Q $query -W -h -1
}

$beforeRaw = SqlCmd "SET NOCOUNT ON; SELECT COUNT(*) FROM Users;"
$before = 0
[int]::TryParse(($beforeRaw | Select-Object -Last 1).Trim(), [ref]$before) | Out-Null

docker exec $ContainerName /bin/bash -c "mkdir -p $backupDir" | Out-Null
SqlCmd "BACKUP DATABASE [$DatabaseName] TO DISK = N'$backupFile' WITH INIT, FORMAT;" | Out-Null

SqlCmdMaster @"
ALTER DATABASE [$DatabaseName] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
DROP DATABASE [$DatabaseName];
"@ | Out-Null

$restoreList = docker exec $ContainerName /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P $SaPassword -d master -C -Q "RESTORE FILELISTONLY FROM DISK = N'$backupFile';" -W -h -1
$dataLogical = ($restoreList | Select-Object -First 1).Split()[0]
$logLogical = ($restoreList | Select-Object -Skip 1 -First 1).Split()[0]

SqlCmdMaster "RESTORE DATABASE [$DatabaseName] FROM DISK = N'$backupFile' WITH MOVE N'$dataLogical' TO N'/var/opt/mssql/data/$DatabaseName.mdf', MOVE N'$logLogical' TO N'/var/opt/mssql/data/${DatabaseName}_log.ldf', REPLACE;" | Out-Null

$afterRaw = SqlCmd "SET NOCOUNT ON; SELECT COUNT(*) FROM Users;"
$after = 0
[int]::TryParse(($afterRaw | Select-Object -Last 1).Trim(), [ref]$after) | Out-Null

$ok = $before -eq $after -and $after -gt 0
[pscustomobject]@{
  beforeUsers = $before
  afterUsers = $after
  pass = $ok
} | ConvertTo-Json
