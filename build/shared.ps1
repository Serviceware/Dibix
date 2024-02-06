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