param
(
    [Parameter()]
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Debug'
)

$ErrorActionPreference = 'Stop'

$currentDirectory = $PSScriptRoot
$scriptPath       = Join-Path $currentDirectory 'Start-Host.ps1'

& $scriptPath -AppName 'Dibix.Http.Host' -Configuration $Configuration -Arguments '--urls=https://+:7287;http://+:5128'