<#
.SYNOPSIS
    Packages the LeakDown mod into a ZIP for release.
.DESCRIPTION
    Reads the version from the .csproj, collects the build output DLL,
    README.txt, LICENSE, GPL3.0.txt, and info.json into a versioned ZIP.
.PARAMETER Configuration
    Build configuration (Debug or Release). Default: Release.
.PARAMETER Framework
    Target framework folder. Default: netstandard2.0.
#>
param(
    [string]$Configuration = 'Release',
    [string]$Framework     = 'netstandard2.0'
)

# Navigate to script directory (project root)
Push-Location -Path (Split-Path -Parent $MyInvocation.MyCommand.Path)

# Read version from csproj
[xml]$csproj        = Get-Content 'LeakDown.csproj'
$version           = $csproj.Project.PropertyGroup.InformationalVersion

# Prepare file list
$buildFolder      = Join-Path -Path 'bin' -ChildPath "$Configuration\$Framework"
$binaryPath       = Join-Path -Path $buildFolder -ChildPath 'LeakDown.dll'
$readme           = 'README.txt'
$license          = 'LICENSE'
$gpl              = 'GPL3.0.txt'
$info             = 'info.json'

$filesToPackage = @($binaryPath, $readme, $license, $gpl, $info)

# Create ZIP
$zipName = "LeakDown-$version.zip"
if (Test-Path $zipName) { Remove-Item $zipName -Force }
Compress-Archive -Path $filesToPackage -DestinationPath $zipName -Force
Write-Host "Created package: $zipName"

# Return to original location
Pop-Location
