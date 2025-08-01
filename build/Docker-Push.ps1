param
(
    [Parameter(Mandatory)]
    [ValidateSet('Dibix.Http.Host', 'Dibix.Worker.Host')]
    [string]$AppName
)

$ErrorActionPreference = 'Stop'
. $PSScriptRoot\shared.ps1


$repositoryRoot   = Resolve-Path (Join-Path $PSScriptRoot '..')
$dockerRepository = $AppName.ToLowerInvariant().Replace('.', '-')
$dockerRegistry   = 'servicewareit'
$version          = nbgv get-version --variable NuGetPackageVersion --project $repositoryRoot


Exec "docker push $dockerRegistry/$($dockerRepository):latest"
Exec "docker push $dockerRegistry/$($dockerRepository):$version"