param
(
    [Parameter(Mandatory)]
    [ValidateSet('Dibix.Http.Host', 'Dibix.Worker.Host')]
    [string]$AppName,

    [Parameter()]
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release',

    [Parameter()]
    [boolean]$SelfContained = $false
)

$ErrorActionPreference = 'Stop'
. $PSScriptRoot\shared.ps1


$runtimeIdentifier = 'win-x64'
$publishReadyToRun = 'True'
$publishSingleFile = 'True'
$sourcePath        = Resolve-Path (Join-Path $PSScriptRoot "../src/$AppName")


if ($Configuration -eq 'Debug')
{
    Exec "dotnet build --configuration $Configuration $sourcePath"
}
else
{
    Exec "dotnet publish --configuration $Configuration
                         --runtime $runtimeIdentifier
                         $(if ($SelfContained) { "--self-contained" } else { "--no-self-contained" })
                         --p:IgnoreProjectGuid=True
                         --p:PublishReadyToRun=$publishReadyToRun
                         --p:PublishSingleFile=$publishSingleFile
                         --p:IncludeNativeLibrariesForSelfExtract=True
                         $sourcePath"
}