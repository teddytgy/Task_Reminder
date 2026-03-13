param(
    [string]$HostName = "localhost",
    [int]$Port = 5432,
    [string]$Database = "task_reminder",
    [string]$Username = "postgres",
    [Parameter(Mandatory = $true)]
    [string]$BackupFile,
    [string]$PgRestorePath = "pg_restore"
)

$ErrorActionPreference = "Stop"

if (-not (Test-Path $BackupFile)) {
    throw "Backup file not found: $BackupFile"
}

& $PgRestorePath `
    --host=$HostName `
    --port=$Port `
    --username=$Username `
    --clean `
    --if-exists `
    --no-owner `
    --dbname=$Database `
    $BackupFile

Write-Host "Database restore completed from $BackupFile"
