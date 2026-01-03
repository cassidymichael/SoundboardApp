# Soundboard Build Script
# Creates both portable and installer distributions

param(
    [switch]$Portable,
    [switch]$Installer,
    [switch]$All
)

$ErrorActionPreference = "Stop"

$ProjectDir = "$PSScriptRoot\SoundboardApp"

# Read version from csproj
$csproj = [xml](Get-Content "$ProjectDir\SoundboardApp.csproj")
$Version = $csproj.Project.PropertyGroup.Version
if (-not $Version) {
    throw "Version not found in csproj. Add <Version>x.y.z</Version> to PropertyGroup."
}
$PublishDir = "$PSScriptRoot\publish"
$DistDir = "$PSScriptRoot\dist"
$InstallerDir = "$PSScriptRoot\installer"

# Inno Setup compiler - adjust path if installed elsewhere
$ISCC = "C:\Program Files (x86)\Inno Setup 6\ISCC.exe"

function Write-Step($message) {
    Write-Host "`n=== $message ===" -ForegroundColor Cyan
}

function Publish-App {
    Write-Step "Publishing application"

    # Clean previous publish
    if (Test-Path $PublishDir) {
        # Check if Soundboard is running
        $proc = Get-Process -Name "Soundboard" -ErrorAction SilentlyContinue
        if ($proc) {
            Write-Host "Soundboard is running. Closing it..." -ForegroundColor Yellow
            $proc | Stop-Process -Force
            Start-Sleep -Milliseconds 500
        }

        try {
            Remove-Item $PublishDir -Recurse -Force -ErrorAction Stop
        }
        catch {
            Write-Host "Could not clean publish folder. Retrying..." -ForegroundColor Yellow
            Start-Sleep -Seconds 1
            Remove-Item $PublishDir -Recurse -Force
        }
    }

    # Publish self-contained single-file (version comes from csproj)
    dotnet publish $ProjectDir `
        -c Release `
        -r win-x64 `
        --self-contained true `
        -p:PublishSingleFile=true `
        -p:IncludeNativeLibrariesForSelfExtract=true `
        -p:DebugType=none `
        -o $PublishDir

    if ($LASTEXITCODE -ne 0) {
        throw "Publish failed"
    }

    Write-Host "Published to: $PublishDir" -ForegroundColor Green
}

function New-PortableZip {
    Write-Step "Creating portable zip"

    if (-not (Test-Path $DistDir)) {
        New-Item -ItemType Directory -Path $DistDir | Out-Null
    }

    $zipPath = "$DistDir\Soundboard-$Version-portable.zip"

    if (Test-Path $zipPath) {
        Remove-Item $zipPath -Force
    }

    # Use maximum compression
    Compress-Archive -Path "$PublishDir\Soundboard.exe" -DestinationPath $zipPath -CompressionLevel Optimal

    $zipSize = [math]::Round((Get-Item $zipPath).Length / 1MB, 1)
    Write-Host "Created: $zipPath ($zipSize MB)" -ForegroundColor Green
}

function New-Installer {
    Write-Step "Creating installer"

    if (-not (Test-Path $ISCC)) {
        throw "Inno Setup not found at: $ISCC`nDownload from: https://jrsoftware.org/isdl.php"
    }

    if (-not (Test-Path $DistDir)) {
        New-Item -ItemType Directory -Path $DistDir | Out-Null
    }

    # Update version in .iss file
    $issPath = "$InstallerDir\soundboard.iss"
    $issContent = Get-Content $issPath -Raw
    $issContent = $issContent -replace '#define MyAppVersion ".*"', "#define MyAppVersion `"$Version`""
    Set-Content $issPath $issContent

    # Run Inno Setup compiler
    & $ISCC $issPath

    if ($LASTEXITCODE -ne 0) {
        throw "Inno Setup compilation failed"
    }

    Write-Host "Installer created in: $DistDir" -ForegroundColor Green
}

# Main
Write-Host "Soundboard Build Script v$Version" -ForegroundColor Yellow

if (-not ($Portable -or $Installer -or $All)) {
    $All = $true  # Default to building all
}

# Always publish first
Publish-App

if ($Portable -or $All) {
    New-PortableZip
}

if ($Installer -or $All) {
    New-Installer
}

Write-Step "Build complete!"
Write-Host "Output files are in: $DistDir" -ForegroundColor Green
Get-ChildItem $DistDir | ForEach-Object { Write-Host "  - $($_.Name)" }
