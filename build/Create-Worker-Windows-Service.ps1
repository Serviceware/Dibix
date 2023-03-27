$ErrorActionPreference = 'Stop'

$serviceName       = 'Dibix Worker Host'
$runtimeIdentifier = 'win-x64'
$configuration     = 'Release'
$publishReadyToRun = 'True'
$publishSingleFile = 'True'
$sourcePath        = Resolve-Path (Join-Path $PSScriptRoot '../src/Dibix.Worker.Host')
$exePath           = Join-Path $sourcePath "bin/$configuration/net6.0/$runtimeIdentifier/publish/Dibix.Worker.Host.exe"

dotnet publish --configuration $configuration                `
               --runtime $runtimeIdentifier                  `
               --self-contained                              `
               --p:IgnoreProjectGuid=True                    `
               --p:PublishReadyToRun=$publishReadyToRun      `
               --p:PublishSingleFile=$publishSingleFile      `
               --p:IncludeNativeLibrariesForSelfExtract=True `
               --p:IncludeNativeLibrariesForSelfExtract=True `
               $sourcePath

Write-Output $exePath

<#

sc.exe delete $serviceName # Remove-Service was only introduced in PS 6
New-Service -Name $serviceName -BinaryPathName "$exePath --environment Development"

net start """$serviceName"""

#>