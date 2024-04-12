param
(
    [Parameter(Mandatory)]
    [ValidateSet('Dibix.Http.Host', 'Dibix.Worker.Host')]
    [string]$AppName
)

$ErrorActionPreference = 'Stop'
. $PSScriptRoot\shared.ps1


$runtimeIdentifier = 'linux-musl-x64'
$configuration     = 'Release'
$publishReadyToRun = 'True'
$publishSingleFile = 'True'
$sourcePath        = Resolve-Path (Join-Path $PSScriptRoot "../src/$AppName")


Exec "dotnet publish --configuration $Configuration
                     --runtime $runtimeIdentifier
                     --self-contained
                     --p:IgnoreProjectGuid=True
                     --p:PublishReadyToRun=$publishReadyToRun
                     --p:PublishSingleFile=$publishSingleFile
                     --p:IncludeNativeLibrariesForSelfExtract=True
                     $sourcePath"

$binaryFolder       = Resolve-Path (Join-Path $sourcePath "bin/$configuration/net8.0/$runtimeIdentifier/publish/")
$dockerBuildContext = $binaryFolder
$dockerFilePath     = Join-Path $sourcePath 'Dockerfile'
$dockerTagName      = $AppName.ToLowerInvariant().Replace('.', '-')

Exec "docker build --tag $($dockerTagName):latest --file ""$dockerFilePath"" ""$dockerBuildContext"""