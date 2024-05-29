param
(
    [Parameter()]
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Debug',

    [Parameter()]
    [boolean]$SelfContained = $true
)

$ErrorActionPreference = 'Stop'

$currentDirectory = $PSScriptRoot
$scriptPath       = Join-Path $currentDirectory 'Build-Host.ps1'

& $scriptPath -AppName 'Dibix.Http.Host' -Configuration $Configuration -SelfContained $SelfContained