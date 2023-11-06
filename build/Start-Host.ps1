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
$publishReadyToRun = 'True'
$publishSingleFile = 'True'
$sourcePath        = Resolve-Path (Join-Path $PSScriptRoot "../src/$AppName")
$binaryFolder      = "bin/$Configuration/net6.0/"

if ($Configuration -eq 'Release')
{
    $binaryFolder = "$binaryFolder$runtimeIdentifier/publish/"
}

$exePath = Join-Path $sourcePath "$($binaryFolder)$AppName.exe"
$environment = if ($Configuration -eq 'Debug') { 'Development' } else { 'Production' }


function Exec
{
    param (
        [Parameter(Mandatory)]
        [ValidateNotNullOrEmpty()]
        [string]$Cmd
    )

    $normalizedCmd = $Cmd.Replace("`r`n", '') -replace '\s+', ' '
    Write-Host $normalizedCmd -ForegroundColor Cyan
    Invoke-Expression "& $normalizedCmd"
    if ($LASTEXITCODE -ne 0)
    {
        exit $LASTEXITCODE
    }
}


if ($Configuration -eq 'Debug')
{
    Exec "dotnet build --configuration $Configuration $sourcePath"
}
else
{
    Exec "dotnet publish --configuration $Configuration
                         --runtime $runtimeIdentifier
                         --self-contained
                         --p:IgnoreProjectGuid=True
                         --p:PublishReadyToRun=$publishReadyToRun
                         --p:PublishSingleFile=$publishSingleFile
                         --p:IncludeNativeLibrariesForSelfExtract=True
                         --p:IncludeNativeLibrariesForSelfExtract=True
                         $sourcePath"
}

$commandArgs = ''
if ($Arguments)
{
    $commandArgs = " $Arguments"
}
Exec "Start-Process $exePath '--environment $environment$commandArgs'"