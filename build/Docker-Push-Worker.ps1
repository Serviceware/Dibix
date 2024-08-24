$ErrorActionPreference = 'Stop'

$currentDirectory = $PSScriptRoot
$scriptPath       = Join-Path $currentDirectory 'Docker-Push.ps1'

& $scriptPath -AppName 'Dibix.Worker.Host'