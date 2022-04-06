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

Get-ChildItem $PSScriptRoot/../src/*.csproj -Recurse | Foreach-Object {
    $cmd = "dotnet build $($_.FullName) --configuration $Configuration /bl:$LoggingDirectory/$($_.BaseName).binlog"
    Write-Host $cmd
    Invoke-Expression "& $cmd"
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
}