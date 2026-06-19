@echo off
setlocal
title WinTweaker - Run (dev)
cd /d "%~dp0"

where dotnet >nul 2>&1
if errorlevel 1 (
    echo  [X] .NET SDK not found. Install .NET 8 SDK from:
    echo  https://dotnet.microsoft.com/download/dotnet/8.0
    pause
    exit /b 1
)

echo  [*] Running WinTweaker from source...
echo      For system tweaks, build the .exe and run it as Administrator.
echo.
dotnet run -c Release
if errorlevel 1 pause
