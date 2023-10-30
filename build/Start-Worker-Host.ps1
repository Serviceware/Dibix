param
(
    [Parameter()]
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Debug'
)

$ErrorActionPreference = 'Stop'

$currentDirectory = $PSScriptRoot
$scriptPath       = Join-Path $currentDirectory 'Start-Host.ps1'

& $scriptPath -AppName 'Dibix.Worker.Host' -Configuration $Configuration