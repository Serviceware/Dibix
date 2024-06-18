param
(
    [Parameter()]
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release',

    [Parameter()]
    [boolean]$SelfContained = $false
)

$ErrorActionPreference = 'Stop'

$currentDirectory = $PSScriptRoot
$scriptPath       = Join-Path $currentDirectory 'Build-Host.ps1'

& $scriptPath -AppName 'Dibix.Http.Host' -Configuration $Configuration -SelfContained $SelfContained