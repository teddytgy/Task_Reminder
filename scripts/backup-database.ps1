param(
    [string]$HostName = "localhost",
    [int]$Port = 5432,
    [string]$Database = "task_reminder",
    [string]$Username = "postgres",
    [string]$OutputFolder = ".\artifacts\backups",
    [string]$PgDumpPath = "pg_dump"
)

$ErrorActionPreference = "Stop"

New-Item -ItemType Directory -Force -Path $OutputFolder | Out-Null
$timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
$backupPath = Join-Path $OutputFolder "$Database-$timestamp.backup"

& $PgDumpPath `
    --host=$HostName `
    --port=$Port `
    --username=$Username `
    --format=custom `
    --file=$backupPath `
    $Database

Write-Host "Database backup created at $backupPath"
