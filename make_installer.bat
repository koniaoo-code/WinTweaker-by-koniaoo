@echo off
setlocal enabledelayedexpansion
title WinTweaker - Make Installer
cd /d "%~dp0"

set "HERE=%~dp0"
if "%HERE:~-1%"=="\" set "HERE=%HERE:~0,-1%"
set "PUB=%HERE%\bin\Release\net8.0-windows\win-x64\publish"
set "PF86=%ProgramFiles(x86)%"
set "PF64=%ProgramFiles%"

echo.
echo  ==========================================
echo    WinTweaker  -  build installer (.exe)
echo  ==========================================
echo.

REM ---- 1) .NET 8 SDK ----
where dotnet >nul 2>&1
if errorlevel 1 (
    echo  [i] .NET SDK not found. Trying winget...
    winget install --id Microsoft.DotNet.SDK.8 -e --accept-source-agreements --accept-package-agreements
)
where dotnet >nul 2>&1
if errorlevel 1 (
    echo  [X] .NET SDK still missing. Install it:
    echo      https://dotnet.microsoft.com/download/dotnet/8.0
    pause & exit /b 1
)

REM ---- 2) Build the app ----
if exist "bin" rmdir /s /q "bin"
echo  [*] Building app...
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true
if errorlevel 1 (
    echo  [X] App build FAILED. Copy the text above and send it to me.
    pause & exit /b 1
)
if not exist "%PUB%\WinTweaker.exe" (
    echo  [X] WinTweaker.exe not found after build.
    pause & exit /b 1
)
for %%A in ("%PUB%\WinTweaker.exe") do set "EXESZ=%%~zA"
if %EXESZ% LSS 1000000 (
    echo  [X] Built exe is only %EXESZ% bytes - too small / corrupt.
    echo      Your ANTIVIRUS most likely blocked the build.
    echo      Add a Defender exclusion for this folder, then run again:
    echo      Windows Security ^> Virus ^& threat protection ^> Exclusions ^> Add folder.
    pause & exit /b 1
)

REM ---- 2b) Optional code signing (place sign.pfx next to this script) ----
call :sign "%PUB%\WinTweaker.exe"

REM ---- 3) Prefer Inno Setup if already installed (nicer wizard with folder choice) ----
set "ISCC="
call :find_iscc
if defined ISCC (
    echo  [i] Inno Setup found: !ISCC!
    echo  [*] Building installer with Inno Setup...
    "!ISCC!" "WinTweaker.iss" > "build_log.txt" 2>&1
    if not errorlevel 1 goto :ok
    echo  [!] Inno build failed - see build_log.txt. Using built-in installer instead...
)

REM ---- 4) No Inno: deliver the portable exe (install via the in-app button) ----
echo  [*] No Inno Setup found - delivering portable WinTweaker.exe...
del "%HERE%\WinTweaker.exe" >nul 2>&1
copy /y "%PUB%\WinTweaker.exe" "%HERE%\WinTweaker.exe" >nul
if not exist "%HERE%\WinTweaker.exe" goto :fail
for %%A in ("%HERE%\WinTweaker.exe") do set "SETSZ=%%~zA"
if %SETSZ% LSS 1000000 goto :truncated
call :sign "%HERE%\WinTweaker.exe"

del "build_log.txt" >nul 2>&1
echo.
echo  ==========================================
echo    [OK] Done!
echo  ==========================================
echo.
echo  WinTweaker.exe is now next to this script.
echo  Run it, open the "About" tab and click "Install"
echo  to create Desktop and Start Menu shortcuts.
echo.
if exist "%HERE%\WinTweaker.exe" explorer /select,"%HERE%\WinTweaker.exe"
pause
exit /b 0

:truncated
echo.
echo  [X] WinTweaker.exe got truncated to %SETSZ% bytes by Windows Defender / SmartScreen.
echo      Add this folder to Defender exclusions, then run again:
echo      Windows Security ^> Virus ^& threat protection ^> Exclusions ^> Add folder.
echo.
pause & exit /b 1

:fail
echo.
echo  [X] Installer was NOT created.
echo.
pause & exit /b 1

:ok
call :sign "%HERE%\WinTweaker_Setup.exe"
del "build_log.txt" >nul 2>&1
echo.
echo  ==========================================
echo    [OK] Done!  ->  WinTweaker_Setup.exe
echo  ==========================================
echo.
if exist "%HERE%\WinTweaker_Setup.exe" explorer /select,"%HERE%\WinTweaker_Setup.exe"
pause
exit /b 0

REM ============ subroutines ============

:find_iscc
for /f "delims=" %%I in ('where iscc.exe 2^>nul') do if not defined ISCC set "ISCC=%%I"
if not defined ISCC if exist "%PF86%\Inno Setup 6\ISCC.exe" set "ISCC=%PF86%\Inno Setup 6\ISCC.exe"
if not defined ISCC if exist "%PF64%\Inno Setup 6\ISCC.exe" set "ISCC=%PF64%\Inno Setup 6\ISCC.exe"
if not defined ISCC if exist "%PF86%\Inno Setup 5\ISCC.exe" set "ISCC=%PF86%\Inno Setup 5\ISCC.exe"
if not defined ISCC if exist "%PF64%\Inno Setup 5\ISCC.exe" set "ISCC=%PF64%\Inno Setup 5\ISCC.exe"
goto :eof

REM ---- Optional code signing: needs sign.pfx (and sign.pwd for password) next to this script ----
:sign
if not exist "%HERE%\sign.pfx" goto :eof
if not exist "%~1" goto :eof
if not defined SIGNTOOL call :find_signtool
if not defined SIGNTOOL (
    echo  [!] sign.pfx found but signtool.exe not found - skipping signing.
    goto :eof
)
set "PWD="
if exist "%HERE%\sign.pwd" set /p PWD=<"%HERE%\sign.pwd"
echo  [*] Signing %~nx1 ...
if defined PWD (
    "!SIGNTOOL!" sign /f "%HERE%\sign.pfx" /p "!PWD!" /fd SHA256 /tr http://timestamp.digicert.com /td SHA256 "%~1"
) else (
    "!SIGNTOOL!" sign /f "%HERE%\sign.pfx" /fd SHA256 /tr http://timestamp.digicert.com /td SHA256 "%~1"
)
goto :eof

:find_signtool
for /f "delims=" %%S in ('dir /b /s "%PF86%\Windows Kits\10\bin\*\x64\signtool.exe" 2^>nul') do set "SIGNTOOL=%%S"
goto :eof
