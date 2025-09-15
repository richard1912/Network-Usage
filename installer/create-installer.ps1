# Network Usage Monitor Installer Creation Script
# Creates Windows installer package with auto-start functionality

param(
    [string]$Configuration = "Release",
    [string]$OutputPath = "bin\installer",
    [switch]$SkipBuild = $false,
    [switch]$Verbose = $false
)

$ErrorActionPreference = "Stop"

Write-Host "Creating Network Usage Monitor Installer..." -ForegroundColor Green
Write-Host "Configuration: $Configuration" -ForegroundColor Yellow
Write-Host "Output Path: $OutputPath" -ForegroundColor Yellow

# Script paths
$ProjectRoot = Split-Path -Parent $PSScriptRoot
$InstallerDir = $PSScriptRoot
$BinPath = Join-Path $ProjectRoot "bin\$Configuration\net8.0-windows"
$SetupScript = Join-Path $InstallerDir "setup.iss"

try {
    # Step 1: Build the application if not skipping
    if (-not $SkipBuild) {
        Write-Host "`n1. Building application..." -ForegroundColor Cyan
        
        Set-Location $ProjectRoot
        
        # Clean previous builds
        if (Test-Path "bin") { Remove-Item "bin" -Recurse -Force }
        if (Test-Path "obj") { Remove-Item "obj" -Recurse -Force }
        
        # Restore and build
        Write-Host "Restoring packages..." -ForegroundColor Yellow
        dotnet restore NetworkUsage.sln --verbosity minimal
        
        Write-Host "Building application..." -ForegroundColor Yellow
        dotnet build NetworkUsage.sln --configuration $Configuration --verbosity minimal
        
        # Publish for deployment
        Write-Host "Publishing application..." -ForegroundColor Yellow
        dotnet publish NetworkUsage.csproj --configuration $Configuration --output $BinPath --verbosity minimal
        
        Write-Host "Build completed successfully!" -ForegroundColor Green
    }
    else {
        Write-Host "`n1. Skipping build (using existing binaries)" -ForegroundColor Yellow
    }

    # Step 2: Verify required files exist
    Write-Host "`n2. Verifying build output..." -ForegroundColor Cyan
    
    $RequiredFiles = @(
        "NetworkUsage.exe",
        "NetworkUsage.dll", 
        "appsettings.json"
    )
    
    foreach ($file in $RequiredFiles) {
        $filePath = Join-Path $BinPath $file
        if (-not (Test-Path $filePath)) {
            throw "Required file not found: $filePath"
        }
        Write-Host "✓ Found: $file" -ForegroundColor Green
    }

    # Step 3: Create application icon if needed
    Write-Host "`n3. Preparing application resources..." -ForegroundColor Cyan
    
    $IconPath = Join-Path $ProjectRoot "icon.ico"
    if (-not (Test-Path $IconPath)) {
        Write-Host "Creating default application icon..." -ForegroundColor Yellow
        
        # Skip icon creation for now - not required for basic installer
        # In production, a proper .ico file should be provided
        Write-Host "⚠ Icon file not found. Skipping icon creation." -ForegroundColor Yellow
        Write-Host "  For production builds, place a proper icon.ico file in the project root" -ForegroundColor Gray
    }

    # Step 4: Create installer directory structure
    Write-Host "`n4. Preparing installer directory..." -ForegroundColor Cyan
    
    $FullOutputPath = Join-Path $ProjectRoot $OutputPath
    if (-not (Test-Path $FullOutputPath)) {
        New-Item -Path $FullOutputPath -ItemType Directory -Force | Out-Null
    }
    
    Write-Host "✓ Installer output directory ready: $FullOutputPath" -ForegroundColor Green

    # Step 5: Generate batch installer (alternative to Inno Setup)
    Write-Host "`n5. Creating Windows installer..." -ForegroundColor Cyan
    
    $InstallerBatch = Join-Path $FullOutputPath "Install-NetworkUsage.bat"
    
    # Create the batch content with properly expanded variables
    $BatchContent = @"
@echo off
setlocal enabledelayedexpansion

echo Network Usage Monitor v1.0.0 Installer
echo ========================================
echo.

REM Check for .NET 8.0
echo Checking for .NET 8.0...
dotnet --version >nul 2>&1
if errorlevel 1 (
    echo ERROR: .NET 8.0 is required but not found.
    echo Please install .NET 8.0 from: https://dotnet.microsoft.com/download/dotnet/8.0
    pause
    exit /b 1
)
echo ✓ .NET 8.0 found

REM Create installation directory
set "INSTALL_DIR=%ProgramFiles%\NetworkUsage"
echo.
echo Installing to: !INSTALL_DIR!

if not exist "!INSTALL_DIR!" (
    mkdir "!INSTALL_DIR!" 2>nul
    if errorlevel 1 (
        echo ERROR: Failed to create installation directory.
        echo Please run as administrator or choose a different location.
        pause
        exit /b 1
    )
)

REM Copy application files
echo.
echo Copying application files...
xcopy /Y /Q "$($BinPath.Replace('\', '\\'))\*" "!INSTALL_DIR!\" >nul
if errorlevel 1 (
    echo ERROR: Failed to copy application files.
    pause
    exit /b 1
)
echo ✓ Application files copied

REM Create desktop shortcut
echo.
echo Creating desktop shortcut...
set "SHORTCUT=%USERPROFILE%\Desktop\Network Usage Monitor.lnk"
powershell -Command "New-Object -ComObject WScript.Shell | ForEach-Object { `$link = `$_.CreateShortcut('!SHORTCUT!'); `$link.TargetPath = '!INSTALL_DIR!\NetworkUsage.exe'; `$link.WorkingDirectory = '!INSTALL_DIR!'; `$link.Description = 'Network Usage Monitor'; `$link.Save() }"
echo ✓ Desktop shortcut created

REM Add to Windows startup (optional)
echo.
set /p "STARTUP=Add to Windows startup? (Y/N): "
if /i "!STARTUP!"=="Y" (
    reg add "HKCU\Software\Microsoft\Windows\CurrentVersion\Run" /v "NetworkUsage" /t REG_SZ /d "\"!INSTALL_DIR!\NetworkUsage.exe\" --minimized" /f >nul
    echo ✓ Added to Windows startup
)

REM Create uninstaller
echo.
echo Creating uninstaller...
set "UNINSTALLER=!INSTALL_DIR!\Uninstall.bat"
(
echo @echo off
echo echo Uninstalling Network Usage Monitor...
echo taskkill /f /im NetworkUsage.exe 2^>nul
echo reg delete "HKCU\Software\Microsoft\Windows\CurrentVersion\Run" /v "NetworkUsage" /f 2^>nul
echo rmdir /s /q "!INSTALL_DIR!" 2^>nul
echo del "%USERPROFILE%\Desktop\Network Usage Monitor.lnk" 2^>nul
echo echo Network Usage Monitor has been uninstalled.
echo pause
) > "!UNINSTALLER!"
echo ✓ Uninstaller created

echo.
echo ========================================
echo Installation completed successfully!
echo ========================================
echo.
echo Application installed to: !INSTALL_DIR!
echo Desktop shortcut created
echo.
set /p "LAUNCH=Launch Network Usage Monitor now? (Y/N): "
if /i "!LAUNCH!"=="Y" (
    start "" "!INSTALL_DIR!\NetworkUsage.exe" --minimized
    echo ✓ Application launched
)

echo.
echo Installation complete. Thank you for using Network Usage Monitor!
pause
"@
    
    Set-Content -Path $InstallerBatch -Value $BatchContent -Encoding UTF8
    Write-Host "✓ Batch installer created: $InstallerBatch" -ForegroundColor Green

    # Step 6: Create PowerShell installer for advanced features
    $PowerShellInstaller = Join-Path $FullOutputPath "Install-NetworkUsage.ps1"
    
    # Create PowerShell installer with properly expanded variables
    $PowerShellContent = @"
# Network Usage Monitor PowerShell Installer
# Provides enhanced installation with registry management and service registration

param(
    [string]`$InstallPath = "`$env:ProgramFiles\NetworkUsage",
    [switch]`$AddToStartup = `$true,
    [switch]`$CreateDesktopShortcut = `$true,
    [switch]`$Silent = `$false
)

`$ErrorActionPreference = "Stop"

function Write-Status {
    param([string]`$Message, [string]`$Color = "White")
    if (-not `$Silent) {
        Write-Host `$Message -ForegroundColor `$Color
    }
}

function Test-DotNetVersion {
    try {
        `$version = & dotnet --version 2>`$null
        if (`$version -and `$version.StartsWith("8.")) {
            return `$true
        }
    }
    catch { }
    return `$false
}

function Install-NetworkUsage {
    Write-Status "Network Usage Monitor v1.0.0 Installer" "Green"
    Write-Status "=" * 45 "Green"
    
    # Check prerequisites
    Write-Status "`nChecking prerequisites..." "Cyan"
    
    if (-not (Test-DotNetVersion)) {
        Write-Status "ERROR: .NET 8.0 is required but not found." "Red"
        Write-Status "Please install from: https://dotnet.microsoft.com/download/dotnet/8.0" "Yellow"
        return `$false
    }
    Write-Status "✓ .NET 8.0 found" "Green"
    
    # Create installation directory
    Write-Status "`nCreating installation directory..." "Cyan"
    try {
        if (-not (Test-Path `$InstallPath)) {
            New-Item -Path `$InstallPath -ItemType Directory -Force | Out-Null
        }
        Write-Status "✓ Installation directory ready: `$InstallPath" "Green"
    }
    catch {
        Write-Status "ERROR: Failed to create installation directory: `$_" "Red"
        return `$false
    }
    
    # Copy application files
    Write-Status "`nCopying application files..." "Cyan"
    try {
        `$SourcePath = "$BinPath"
        Copy-Item -Path "`$SourcePath\*" -Destination `$InstallPath -Recurse -Force
        Write-Status "✓ Application files copied successfully" "Green"
    }
    catch {
        Write-Status "ERROR: Failed to copy files: `$_" "Red"
        return `$false
    }
    
    # Create desktop shortcut
    if (`$CreateDesktopShortcut) {
        Write-Status "`nCreating desktop shortcut..." "Cyan"
        try {
            `$WshShell = New-Object -ComObject WScript.Shell
            `$Shortcut = `$WshShell.CreateShortcut("`$env:USERPROFILE\Desktop\Network Usage Monitor.lnk")
            `$Shortcut.TargetPath = "`$InstallPath\NetworkUsage.exe"
            `$Shortcut.WorkingDirectory = `$InstallPath
            `$Shortcut.Description = "Network Usage Monitor - Real-time network traffic monitoring"
            `$Shortcut.Save()
            Write-Status "✓ Desktop shortcut created" "Green"
        }
        catch {
            Write-Status "Warning: Failed to create desktop shortcut: `$_" "Yellow"
        }
    }
    
    # Add to Windows startup
    if (`$AddToStartup) {
        Write-Status "`nAdding to Windows startup..." "Cyan"
        try {
            `$StartupPath = "HKCU:\Software\Microsoft\Windows\CurrentVersion\Run"
            Set-ItemProperty -Path `$StartupPath -Name "NetworkUsage" -Value "`"`$InstallPath\NetworkUsage.exe`" --minimized"
            Write-Status "✓ Added to Windows startup" "Green"
        }
        catch {
            Write-Status "Warning: Failed to add to startup: `$_" "Yellow"
        }
    }
    
    # Register application settings
    Write-Status "`nRegistering application..." "Cyan"
    try {
        `$AppRegPath = "HKCU:\Software\NetworkUsage"
        if (-not (Test-Path `$AppRegPath)) {
            New-Item -Path `$AppRegPath -Force | Out-Null
        }
        Set-ItemProperty -Path `$AppRegPath -Name "InstallPath" -Value `$InstallPath
        Set-ItemProperty -Path `$AppRegPath -Name "Version" -Value "1.0.0"
        Set-ItemProperty -Path `$AppRegPath -Name "InstallDate" -Value (Get-Date).ToString("yyyy-MM-dd HH:mm:ss")
        Write-Status "✓ Application registered" "Green"
    }
    catch {
        Write-Status "Warning: Failed to register application: `$_" "Yellow"
    }
    
    # Create uninstaller
    Write-Status "`nCreating uninstaller..." "Cyan"
    try {
        `$UninstallerPath = "`$InstallPath\Uninstall.ps1"
        `$UninstallerContent = @"
# Network Usage Monitor Uninstaller
param([switch]`$Silent = `$false)

`$ErrorActionPreference = "SilentlyContinue"

if (-not `$Silent) {
    Write-Host "Uninstalling Network Usage Monitor..." -ForegroundColor Yellow
}

# Stop application if running
Stop-Process -Name "NetworkUsage" -Force -ErrorAction SilentlyContinue

# Remove from startup
Remove-ItemProperty -Path "HKCU:\Software\Microsoft\Windows\CurrentVersion\Run" -Name "NetworkUsage" -ErrorAction SilentlyContinue

# Remove registry entries
Remove-Item -Path "HKCU:\Software\NetworkUsage" -Recurse -Force -ErrorAction SilentlyContinue

# Remove desktop shortcut
Remove-Item -Path "`$env:USERPROFILE\Desktop\Network Usage Monitor.lnk" -Force -ErrorAction SilentlyContinue

# Remove installation directory (self-delete)
Start-Process -FilePath "cmd" -ArgumentList "/c timeout /t 2 & rmdir /s /q `"`$InstallPath`"" -WindowStyle Hidden

if (-not `$Silent) {
    Write-Host "Network Usage Monitor has been uninstalled." -ForegroundColor Green
    Read-Host "Press Enter to close..."
}
"@
        Set-Content -Path `$UninstallerPath -Value `$UninstallerContent
        Write-Status "✓ Uninstaller created" "Green"
    }
    catch {
        Write-Status "Warning: Failed to create uninstaller: `$_" "Yellow"
    }
    
    return `$true
}

function Show-InstallationSummary {
    Write-Status "`n" + "=" * 50 "Green"
    Write-Status "Installation Summary" "Green"  
    Write-Status "=" * 50 "Green"
    Write-Status "Application: Network Usage Monitor v1.0.0"
    Write-Status "Location: `$InstallPath"
    Write-Status "Auto-start: `$(if (`$AddToStartup) { 'Enabled' } else { 'Disabled' })"
    Write-Status "Desktop shortcut: `$(if (`$CreateDesktopShortcut) { 'Created' } else { 'Skipped' })"
    Write-Status "`nInstaller files created in: `$OutputPath"
    Write-Status "`nTo distribute:"
    Write-Status "1. Share Install-NetworkUsage.ps1 for PowerShell installation"
    Write-Status "2. Share Install-NetworkUsage.bat for simple batch installation"
    Write-Status "3. Use Inno Setup with setup.iss for professional installer"
    Write-Status "=" * 50 "Green"
}

# Main execution
try {
    # Ensure we're in the right directory
    Set-Location $ProjectRoot
    
    # Run installation
    if (Install-NetworkUsage) {
        Write-Status "`n✓ Installer creation completed successfully!" "Green"
        
        # Test launch application
        if (-not `$Silent) {
            `$Launch = Read-Host "`nLaunch Network Usage Monitor now? (Y/N)"
            if (`$Launch -eq "Y" -or `$Launch -eq "y") {
                Start-Process -FilePath "`$InstallPath\NetworkUsage.exe" -ArgumentList "--minimized" -WindowStyle Hidden
                Write-Status "✓ Application launched in system tray" "Green"
            }
        }
        
        Show-InstallationSummary
    }
    else {
        Write-Status "✗ Installer creation failed" "Red"
        exit 1
    }
}
catch {
    Write-Status "FATAL ERROR: `$_" "Red"
    Write-Status "Stack trace: `$(`$_.ScriptStackTrace)" "Red"
    exit 1
}
finally {
    if (`$Verbose) {
        Write-Status "`nInstaller creation completed at `$(Get-Date)" "Gray"
    }
}
"@
    
    # Write the PowerShell installer to file
    Set-Content -Path $PowerShellInstaller -Value $PowerShellContent -Encoding UTF8
    Write-Host "✓ PowerShell installer created: $PowerShellInstaller" -ForegroundColor Green

    # Step 7: Copy application files to installer directory for distribution
    Write-Host "`n6. Preparing distribution package..." -ForegroundColor Cyan
    
    $DistributionPath = Join-Path $FullOutputPath "NetworkUsage"
    if (Test-Path $DistributionPath) {
        Remove-Item $DistributionPath -Recurse -Force
    }
    New-Item -Path $DistributionPath -ItemType Directory -Force | Out-Null
    
    # Copy application files
    Copy-Item -Path "$BinPath\*" -Destination $DistributionPath -Recurse -Force
    Write-Host "✓ Application files copied to distribution package" -ForegroundColor Green

    # Step 8: Show completion summary
    Write-Host "`n" -NoNewline
    Write-Host "=" * 60 -ForegroundColor Green
    Write-Host "Installer Creation Summary" -ForegroundColor Green
    Write-Host "=" * 60 -ForegroundColor Green
    Write-Host "✓ Application built and published" -ForegroundColor Green
    Write-Host "✓ Batch installer created: Install-NetworkUsage.bat" -ForegroundColor Green
    Write-Host "✓ PowerShell installer created: Install-NetworkUsage.ps1" -ForegroundColor Green
    Write-Host "✓ Distribution package prepared" -ForegroundColor Green
    Write-Host "`nOutput directory: $FullOutputPath" -ForegroundColor Yellow
    Write-Host "`nDistribution files:" -ForegroundColor White
    Write-Host "  - Install-NetworkUsage.bat (Simple batch installer)" -ForegroundColor Gray
    Write-Host "  - Install-NetworkUsage.ps1 (Advanced PowerShell installer)" -ForegroundColor Gray
    Write-Host "  - NetworkUsage\ (Application files for manual distribution)" -ForegroundColor Gray
    Write-Host "`nTo distribute your application:" -ForegroundColor White
    Write-Host "1. Share the installer files with end users" -ForegroundColor Gray
    Write-Host "2. Users can run either .bat or .ps1 installer" -ForegroundColor Gray  
    Write-Host "3. Or manually copy NetworkUsage folder to Program Files" -ForegroundColor Gray
    Write-Host "=" * 60 -ForegroundColor Green
    
    Write-Host "`n✓ Installer creation completed successfully!" -ForegroundColor Green
}
catch {
    Write-Host "`n✗ Error creating installer: $_" -ForegroundColor Red
    Write-Host "Stack trace: $($_.ScriptStackTrace)" -ForegroundColor Red
    exit 1
}
finally {
    # Return to original location
    Set-Location $ProjectRoot
    
    if ($Verbose) {
        Write-Host "`nScript completed at $(Get-Date)" -ForegroundColor Gray
    }
}
