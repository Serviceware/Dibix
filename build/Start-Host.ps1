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


$runtimeIdentifier = 'win-x64'
$repositoryRoot    = Resolve-Path (Join-Path $PSScriptRoot '..')
$sourcePath        = Join-Path $repositoryRoot "src/$AppName"
$binaryFolder      = Join-Path $sourcePath "bin/$Configuration/net8.0"
$environment       = if ($Configuration -eq 'Debug') { 'Development' } else { 'Production' }

if ($Configuration -eq 'Release')
{
    $binaryFolder = Join-Path $binaryFolder "$runtimeIdentifier/publish"
}

$exePath = Join-Path $binaryFolder "$AppName.exe"
& $exePath --environment $environment --contentRoot $binaryFolder $Arguments