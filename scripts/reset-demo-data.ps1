param(
    [string]$Project = ".\\src\\Task_Reminder.Api\\Task_Reminder.Api.csproj",
    [string]$StartupProject = ".\\src\\Task_Reminder.Api\\Task_Reminder.Api.csproj",
    [string]$ApiUrl = "https://localhost:7087/health",
    [int]$StartupTimeoutSeconds = 240,
    [int]$PollIntervalSeconds = 3
)

$ErrorActionPreference = "Stop"

$ef = Join-Path $env:USERPROFILE ".dotnet\\tools\\dotnet-ef.exe"
if (-not (Test-Path $ef)) {
    throw "dotnet-ef was not found at $ef"
}

$projectPath = (Resolve-Path $Project).Path
$startupProjectPath = (Resolve-Path $StartupProject).Path
$workingDirectory = Split-Path -Parent $startupProjectPath

& $ef database drop --force --project $projectPath --startup-project $startupProjectPath
& $ef database update --project $projectPath --startup-project $startupProjectPath

$api = Start-Process dotnet -ArgumentList "run --no-build --project `"$projectPath`"" -WorkingDirectory $workingDirectory -PassThru
try {
    $healthy = $false
    $attempts = [Math]::Max(1, [int][Math]::Ceiling($StartupTimeoutSeconds / [double][Math]::Max(1, $PollIntervalSeconds)))
    for ($attempt = 0; $attempt -lt $attempts; $attempt++) {
        Start-Sleep -Seconds $PollIntervalSeconds
        try {
            Invoke-RestMethod $ApiUrl -SkipCertificateCheck | Out-Null
            $healthy = $true
            break
        }
        catch {
        }
    }

    if (-not $healthy) {
        throw "API did not become healthy after reset within $StartupTimeoutSeconds seconds. Verify the database connection string and API startup logs."
    }
}
finally {
    Stop-Process -Id $api.Id -Force -ErrorAction SilentlyContinue
}

Write-Host "Demo database reset, migrated, and reseeded successfully."
