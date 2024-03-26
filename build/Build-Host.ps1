param
(
    [Parameter()]
    [ValidateSet('Dibix.Http.Host', 'Dibix.Worker.Host')]
    [string]$AppName,

    [Parameter()]
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Debug'
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
                         $sourcePath"
}