param
(
    [Parameter(Mandatory)]
    [ValidateSet('Dibix.Http.Host', 'Dibix.Worker.Host')]
    [string]$AppName
)

$ErrorActionPreference = 'Stop'
. $PSScriptRoot\shared.ps1


$dockerTagName    = $AppName.ToLowerInvariant().Replace('.', '-')
$dockerRepository = 'tommylohsesw'
$version          = nbgv get-version --variable NuGetPackageVersion


Exec "docker push $dockerRepository/$($dockerTagName):latest"
Exec "docker push $dockerRepository/$($dockerTagName):$version"