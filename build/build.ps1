[CmdletBinding()]
param (
    [Parameter()]
    [ValidateSet('Debug', 'Release')]
    [string]
    $Configuration = 'Release',

    [Parameter()]
    [string]
    $LoggingDirectory = "$PSScriptRoot/../bin/$Configuration/logs"
)

$ErrorActionPreference = 'Stop'

$runtimeIdentifier = 'win-x64'
$Configuration = 'Release'
$publishReadyToRun = 'True'
$publishSingleFile = 'True'
$publishReadyToRun = 'True'
$rootPath = Resolve-Path (Join-Path $PSScriptRoot '..')
$sourcePath = Join-Path $rootPath 'src'
$cleanPath = Join-Path $PSScriptRoot 'clean.bat'

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

Exec $cleanPath

Exec "dotnet restore --runtime $runtimeIdentifier
                     --p:PublishReadyToRun=$publishReadyToRun
                     $rootPath"

$projects = @(
    'Dibix'
    'Dibix.Dapper'
    'Dibix.Sdk.Abstractions'
    'Dibix.Generators'
    'Dibix.Http.Client'
    'Dibix.Http.Server'
    'Dibix.Sdk.Sql'
    'Dibix.Sdk.CodeAnalysis'
    'Dibix.Sdk.CodeGeneration'
    'Dibix.Sdk'
    'Dibix.Sdk.Cli'
    'Dibix.Testing'
    'Dibix.Worker.Abstractions'
)

foreach ($project in $projects)
{
    $projectSourcePath = Join-Path $sourcePath $project
    Exec "dotnet build $projectSourcePath
                       --configuration $Configuration
                       --no-restore
                       --no-dependencies
                       --bl:$LoggingDirectory/$Configuration/$project.binlog
                       --p:PublishSingleFile=False
                       --no-self-contained"
}

$hostSourcePath = Join-Path $sourcePath 'Dibix.Worker.Host'
Exec "dotnet build $hostSourcePath
                   --configuration $Configuration
                   --runtime $runtimeIdentifier
                   --no-restore
                   --no-dependencies
                   --bl:$LoggingDirectory/$Configuration/$project.binlog
                   --p:PublishSingleFile=$publishSingleFile
                   --no-self-contained"

Exec "dotnet publish $hostSourcePath
                     --configuration $Configuration
                     --runtime $runtimeIdentifier
                     --no-self-contained
                     --no-restore
                     --no-build
                     --p:IgnoreProjectGuid=True
                     --p:PublishReadyToRun=$publishReadyToRun
                     --p:PublishSingleFile=$publishSingleFile
                     --p:IncludeNativeLibrariesForSelfExtract=True
                     --p:IncludeNativeLibrariesForSelfExtract=True"