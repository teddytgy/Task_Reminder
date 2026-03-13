param(
    [string]$Configuration = "Release",
    [string]$Output = ".\artifacts\publish\api",
    [switch]$SelfContained,
    [string]$Runtime = "win-x64"
)

$ErrorActionPreference = "Stop"

$publishArgs = @(
    "publish", ".\src\Task_Reminder.Api\Task_Reminder.Api.csproj",
    "-c", $Configuration,
    "-o", $Output
)

if ($SelfContained) {
    $publishArgs += @("-r", $Runtime, "--self-contained", "true")
}
else {
    $publishArgs += @("--self-contained", "false")
}

dotnet @publishArgs

Write-Host "API published to $Output"
