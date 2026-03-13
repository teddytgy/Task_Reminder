param(
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64",
    [string]$Output = ".\artifacts\publish\wpf",
    [switch]$SelfContained
)

$ErrorActionPreference = "Stop"

dotnet publish .\src\Task_Reminder.Wpf\Task_Reminder.Wpf.csproj `
    -c $Configuration `
    -r $Runtime `
    --self-contained $($SelfContained.IsPresent.ToString().ToLowerInvariant()) `
    -o $Output

Write-Host "WPF client published to $Output"
