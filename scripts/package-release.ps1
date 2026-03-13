param(
    [string]$Configuration = "Release",
    [string]$OutputRoot = ".\artifacts\release",
    [string]$Runtime = "win-x64"
)

$ErrorActionPreference = "Stop"

$apiOutput = Join-Path $OutputRoot "api"
$wpfOutput = Join-Path $OutputRoot "wpf"
$manifestPath = Join-Path $OutputRoot "release-manifest.json"

New-Item -ItemType Directory -Force -Path $OutputRoot | Out-Null

& .\scripts\publish-api.ps1 -Configuration $Configuration -Output $apiOutput
& .\scripts\publish-wpf.ps1 -Configuration $Configuration -Runtime $Runtime -Output $wpfOutput

$props = [xml](Get-Content ".\Directory.Build.props")
$version = $props.Project.PropertyGroup.Version
if ([string]::IsNullOrWhiteSpace($version)) {
    $version = "1.0.0"
}

$manifest = [ordered]@{
    createdAtUtc = [DateTime]::UtcNow.ToString("O")
    version = $version
    apiPath = (Resolve-Path $apiOutput).Path
    wpfPath = (Resolve-Path $wpfOutput).Path
    notes = "Copy the WPF publish output to each workstation and point appsettings.json to the shared API host."
}

$manifest | ConvertTo-Json -Depth 5 | Set-Content -Path $manifestPath -Encoding UTF8

Write-Host "Release package prepared in $OutputRoot"
Write-Host "Manifest written to $manifestPath"
