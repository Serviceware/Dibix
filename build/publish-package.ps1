param([string[]]$specificPackages, [switch]$clearNugetCache, [switch]$keepVersion)

$ErrorActionPreference = “Stop”

$packages = @(
    @{ 
		Name = "Dibix",
		IsPrivate = $false
	},
    @{ 
		Name = "Dibix.Dapper",
		IsPrivate = $false
	},
    @{ 
		Name = "Dibix.Sdk",
		IsPrivate = $true
	}
)

if ($specificPackages -ne $null)
{
    $packages = $specificPackages;
}

$scriptDirectory = Split-Path -Path $MyInvocation.MyCommand.Definition -Parent
$versionPropsPath = Join-Path $scriptDirectory "version.props"
$homeDirectory = & { If ($IsWindows -ne $False) { $env:USERPROFILE } Else { $env:HOME } }
$packagesCacheDirectory = Join-Path $homeDirectory ".nuget\packages"
$toolPackagesCacheDirectory = Join-Path $packagesCacheDirectory ".tools"
$sourceDirectory = Join-Path $scriptDirectory "..\src"

#region Functions
function Execute-Command($cmd)
{
    Invoke-Expression "& $cmd"

    if ($LASTEXITCODE -ne 0)
    {
        throw "Command '$cmd' returned exit code $LASTEXITCODE."
    }
}

function Get-NextVersion($versionDefinitionPath, $preventVersionIncrement)
{
    [xml]$versionProps = Get-Content -Path $versionDefinitionPath
    $versionPrefix = [string]$versionProps.Project.PropertyGroup.VersionPrefix
    $versionSuffix = [string]$versionProps.Project.PropertyGroup.VersionSuffix

    if (-Not $preventVersionIncrement)
    {
        $versionSuffixMatch = $versionSuffix -match '^([a-z]+)([\d]+)$';
        $preReleaseType = "preview"
        $preReleaseVersion = 1
        $preReleaseVersionLength = 3

        if ($versionSuffixMatch)
        {
            $preReleaseType = $matches[1] 
            $preReleaseVersion = [int]$matches[2] + 1
        }
        
        $versionSuffix = $preReleaseType + ([string]$preReleaseVersion).PadLeft($preReleaseVersionLength, '0')
    }

    @{
        VersionPrefix = $versionPrefix
        VersionSuffix = $versionSuffix
    }
}

function Update-Version($versionDefinitionPath, $versionPrefix, $versionSuffix)
{
    [xml]$versionProps = Get-Content -Path $versionDefinitionPath
    $versionProps.Project.PropertyGroup.VersionPrefix = $versionPrefix
    $versionProps.Project.PropertyGroup.VersionSuffix = $versionSuffix
    $versionProps.Save($versionDefinitionPath)
}
#endregion

# Get next version
$versionInfo = Get-NextVersion $versionPropsPath $keepVersion
$version = $versionInfo.VersionPrefix
if ($versionInfo.VersionSuffix)
{
    $version += "-$($versionInfo.VersionSuffix)";
}

# Update version
Update-Version $versionPropsPath $versionInfo.VersionPrefix $versionInfo.VersionSuffix

foreach ($package in $packages) 
{
    $packageSourceDirectory = Join-Path $sourceDirectory $package
    $packageCacheDirectory = Join-Path $packagesCacheDirectory $package
    $toolPackageCacheDirectory = Join-Path $toolPackagesCacheDirectory $package
    $packagePath = Join-Path $packageSourceDirectory "bin\Debug" | Join-Path -ChildPath "$package.$version.nupkg"

    # Apparently dotnet pack does not properly build the project, since for CLI tools, the .runtimeconfig.json is not created.
    Execute-Command 'dotnet build "$packageSourceDirectory"' "Build"

    # Create package
    Execute-Command 'dotnet pack "$packageSourceDirectory"' "CreatePackage"

    # Remove previously cached package
    if ($clearNugetCache) 
    {
        if (Test-Path $packageCacheDirectory) 
        {
            Remove-Item -Recurse -Force $packageCacheDirectory
        }
        if (Test-Path $toolPackageCacheDirectory) 
        {
            Remove-Item -Recurse -Force $toolPackageCacheDirectory
        }
    }

    # Publish to nuget
    Execute-Command 'dotnet nuget push --source "helpLine" --api-key VSTS "$packagePath"' "PublishPackage"
}