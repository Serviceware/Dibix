param
(
    [Parameter()]
    [ValidateSet('Dibix.Http.Host', 'Dibix.Worker.Host')]
    [string]$AppName,

    [Parameter()]
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Debug',

    [Parameter()]
    [string]$Arguments
)

$ErrorActionPreference = 'Stop'
. $PSScriptRoot\shared.ps1


$runtimeIdentifier = 'win-x64'
$publishReadyToRun = 'True'
$publishSingleFile = 'True'
$sourcePath        = Resolve-Path (Join-Path $PSScriptRoot "../src/$AppName")
$binaryFolder      = "bin/$Configuration/net8.0/"

if ($Configuration -eq 'Release')
{
    $binaryFolder = "$binaryFolder$runtimeIdentifier/publish/"
}

$exePath         = Join-Path $sourcePath "$($binaryFolder)$AppName.exe"
$environment     = if ($Configuration -eq 'Debug') { 'Development' } else { 'Production' }
$buildScriptPath = Join-Path $PSScriptRoot 'Build-Host.ps1'

& $buildScriptPath -AppName 'Dibix.Http.Host' -Configuration $Configuration

$commandArgs = ''
if ($Arguments)
{
    $commandArgs = " $Arguments"
}
Exec "Start-Process $exePath '--environment $environment$commandArgs'"