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
    # Ensure that the project is restored from scratch, as previous VS restores may have been done with different settings, i.E. PublishReadyToRun
    # This causes issues in the single file compilation, and causes a crash of the application during start
    Exec "dotnet restore --force $sourcePath
                         --runtime $runtimeIdentifier
                         --p:PublishReadyToRun=$publishReadyToRun"

    Exec "dotnet publish --configuration $Configuration
                         --runtime $runtimeIdentifier
                         --no-restore
                         $(if ($SelfContained) { "--self-contained" } else { "--no-self-contained" })
                         --p:IgnoreProjectGuid=True
                         --p:PublishReadyToRun=$publishReadyToRun
                         --p:PublishSingleFile=$publishSingleFile
                         --p:IncludeNativeLibrariesForSelfExtract=True
                         $sourcePath"
}