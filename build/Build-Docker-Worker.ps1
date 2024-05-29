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
$scriptPath       = Join-Path $currentDirectory 'Build-Docker.ps1'

& $scriptPath -AppName 'Dibix.Worker.Host' -Configuration $Configuration -SelfContained $SelfContained