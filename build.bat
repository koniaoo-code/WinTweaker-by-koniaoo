@echo off
setlocal
title WinTweaker - Build
cd /d "%~dp0"

echo.
echo  ==========================================
echo    WinTweaker  -  build portable .exe
echo  ==========================================
echo.

where dotnet >nul 2>&1
if errorlevel 1 (
    echo  [X] .NET SDK not found!
    echo.
    echo  Install .NET 8 SDK from:
    echo  https://dotnet.microsoft.com/download/dotnet/8.0
    echo  ^(you need the SDK, not just the Runtime^)
    echo.
    pause
    exit /b 1
)

echo  [i] dotnet version:
dotnet --version
echo.

if exist "bin" rmdir /s /q "bin"

echo  [*] Building...
echo.

dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true
if errorlevel 1 (
    echo.
    echo  [X] Build FAILED. Copy the text above and send it to me.
    echo.
    pause
    exit /b 1
)

REM Copy the finished exe right next to this build.bat
copy /y "bin\Release\net8.0-windows\win-x64\publish\WinTweaker.exe" "%~dp0WinTweaker.exe" >nul

echo.
echo  ==========================================
echo    [OK] Done!  ->  WinTweaker.exe
echo  ==========================================
echo.

if exist "%~dp0WinTweaker.exe" explorer /select,"%~dp0WinTweaker.exe"
pause
