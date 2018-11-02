$ErrorActionPreference = “Stop”
$userSecretsId = "dibix"

#region Functions
function Prompt-WithCurrentValue([string] $msg, [string] $currentValue)
{
    if ($silent) 
    {
        $currentValue
    }
    else
    {
        $prompt = Read-Host "$msg [$($currentValue)]"
        ($currentValue,$prompt)[[bool]$prompt]
    }
}

function Execute-Command($cmd)
{
    Invoke-Expression "& $cmd"

    if ($LASTEXITCODE -ne 0)
    {
        throw "Command '$cmd' returned exit code $LASTEXITCODE."
    }
}

function Get-Secret($key, $id)
{
    $secretsDirectory = & { 
        if ($IsWindows -ne $False) { Join-Path $env:APPDATA -ChildPath "Microsoft" | Join-Path -ChildPath "UserSecrets" } 
        else { Join-Path $env:HOME -ChildPath ".microsoft" | Join-Path -ChildPath "usersecrets" } 
    }
    $secretsPath = Join-Path $secretsDirectory -ChildPath "$id" | Join-Path -ChildPath "secrets.json"
    if (Test-Path $secretsPath)
    {
        $secrets = Get-Content $secretsPath | Out-String | ConvertFrom-Json
        $secrets.$key
    }
}

function Set-Secret($key, $value, $id)
{
    Execute-Command "dotnet user-secrets set $key $value --id $id"
}
#endregion

$connectionStringKey = "DefaultConnection:ConnectionString"
$providerNameKey = "DefaultConnection:ProviderName"

$connectionString = Get-Secret $connectionStringKey $userSecretsId
$connectionString = Prompt-WithCurrentValue "Enter connection string:" $connectionString
$providerName = "System.Data.SqlClient"

Set-Secret $connectionStringKey $connectionString $userSecretsId
Set-Secret $providerNameKey $providerName $userSecretsId