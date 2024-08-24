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


Exec "docker tag $($dockerTagName):latest $dockerRepository/$($dockerTagName):latest"
Exec "docker push $dockerRepository/$($dockerTagName):latest"