param
(
    [Parameter()]
    [ValidateNotNullOrEmpty()]
    [string]
    $ServiceName = 'Dibix Worker Host',

    [Parameter()]
    [ValidateNotNullOrEmpty()]
    [string]
    $ConnectionString,

    [Parameter()]
    [ValidateNotNullOrEmpty()]
    [string]
    $Extension,

    [Parameter()]
    [ValidateNotNullOrEmpty()]
    [string[]]
    $Workers,

    [Parameter()]
    [switch]$Development
)

$ErrorActionPreference = 'Stop'
. $PSScriptRoot\shared.ps1


$runtimeIdentifier = 'win-x64'
$configuration     = if ($Development) { 'Debug' } else { 'Release' }
$publishReadyToRun = 'True'
$publishSingleFile = 'True'
$sourcePath        = Resolve-Path (Join-Path $PSScriptRoot '../src/Dibix.Worker.Host')
$binaryFolder      = "bin/$configuration/net8.0/"

if (!$Development)
{
    $binaryFolder = "$binaryFolder$runtimeIdentifier/publish/"
}

$exePath = Join-Path $sourcePath "$($binaryFolder)Dibix.Worker.Host.exe"

function Set-ServiceEnvironmentVariables
{
    [string[]]$env = @(
        "Database:ConnectionString=$ConnectionString",
        "Hosting:Extension=$Extension"
    )
    for ($i = 0; $i -lt $Workers.Count; $i++) {
        $worker = $Workers[$i]
        $env += "Hosting:Workers:$i=$worker"
    }

    $key = "HKLM:\SYSTEM\CurrentControlSet\Services\$ServiceName"
    $name = 'Environment'
    New-ItemProperty $key -Name $name -Value $env -PropertyType MultiString -Force
}


$serviceExists = Get-Service $ServiceName -ErrorAction SilentlyContinue

if ($serviceExists)
{
    Exec "Stop-Service ""$ServiceName"""
}

if ($Development)
{
    Exec "dotnet build --configuration $configuration $sourcePath"
}
else
{
    Exec "dotnet publish --configuration $configuration
                         --runtime $runtimeIdentifier
                         --self-contained
                         --p:IgnoreProjectGuid=True
                         --p:PublishReadyToRun=$publishReadyToRun
                         --p:PublishSingleFile=$publishSingleFile
                         --p:IncludeNativeLibrariesForSelfExtract=True
                         --p:IncludeNativeLibrariesForSelfExtract=True
                         $sourcePath"
}

if ($ServiceExists)
{
    Exec "sc.exe delete ""$ServiceName""" # Remove-Service was only introduced in PS 6
}

Exec "New-Service -Name ""$ServiceName"" -BinaryPathName ""$exePath --environment Development"""
Write-Host 'Setting service environment variables' -ForegroundColor Cyan
Set-ServiceEnvironmentVariables
Exec "net start ""$ServiceName"""