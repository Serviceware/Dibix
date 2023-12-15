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

Write-Warning -WarningAction Inquire "Please ensure, that none of the projects are currently opened in Visual Studio, before running this script. Otherwise it will automatically restore these projects after clean using the wrong runtimes."

# The dotnet CLI picks up the global.json, which selects the SDK, only from the current working directory
# See: https://github.com/dotnet/sdk/issues/7465
Push-Location $rootPath

try
{
    Exec $cleanPath

    Exec "dotnet restore --runtime $runtimeIdentifier
                         --p:PublishReadyToRun=$publishReadyToRun
                         $rootPath"

    $projectsToBuild = @(
        'Dibix'
        'Dibix.Dapper'
        'Dibix.Sdk.Abstractions'
        'Dibix.Http.Client'
        'Dibix.Http.Server'
        'Dibix.Sdk.Sql'
        'Dibix.Sdk.CodeAnalysis'
        'Dibix.Sdk.Generators'
        'Dibix.Sdk.CodeGeneration'
        'Dibix.Sdk'
        'Dibix.Sdk.Cli'
        'Dibix.Testing'
        'Dibix.Testing.Generators'
        'Dibix.Worker.Abstractions'
    )
    
    foreach ($projectToBuild in $projectsToBuild)
    {
        $projectSourcePath = Join-Path $sourcePath $projectToBuild
        Exec "dotnet build $projectSourcePath
                           --configuration $Configuration
                           --no-restore
                           --no-dependencies
                           --bl:$LoggingDirectory/$Configuration/$projectToBuild.binlog
                           --p:PublishSingleFile=False
                           --no-self-contained"
    }
    
    $projectsToPublish = @(
        'Dibix.Http.Host'
        'Dibix.Worker.Host'
    )
    
    foreach ($projectToPublish in $projectsToPublish)
    {
        $projectSourcePath = Join-Path $sourcePath $projectToPublish
        
        # Build again for specific runtime
        Exec "dotnet build $projectSourcePath
                           --configuration $Configuration
                           --runtime $runtimeIdentifier
                           --no-restore
                           --no-dependencies
                           --bl:$LoggingDirectory/$Configuration/$projectToBuild.binlog
                           --p:PublishSingleFile=$publishSingleFile
                           --no-self-contained"
    
        Exec "dotnet publish $projectSourcePath
                             --configuration $Configuration
                             --runtime $runtimeIdentifier
                             --no-self-contained
                             --no-restore
                             --no-build
                             --p:IgnoreProjectGuid=True
                             --p:PublishReadyToRun=$publishReadyToRun
                             --p:PublishSingleFile=$publishSingleFile
                             --p:IncludeNativeLibrariesForSelfExtract=True"
    }
    
}
finally
{
    Pop-Location
}