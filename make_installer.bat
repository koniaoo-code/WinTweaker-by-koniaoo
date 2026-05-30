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
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true
if errorlevel 1 (
    echo  [X] App build FAILED. Copy the text above and send it to me.
    pause & exit /b 1
)
if not exist "%PUB%\WinTweaker.exe" (
    echo  [X] WinTweaker.exe not found after build.
    pause & exit /b 1
)

REM ---- 3) Prefer Inno Setup if already installed (nicer wizard) ----
set "ISCC="
call :find_iscc
if defined ISCC (
    echo  [i] Inno Setup found: !ISCC!
    echo  [*] Building installer with Inno Setup...
    "!ISCC!" "WinTweaker.iss" > "build_log.txt" 2>&1
    if not errorlevel 1 goto :ok
    echo  [!] Inno build failed - see build_log.txt. Falling back to IExpress...
)

REM ---- 4) Fallback: IExpress (built into Windows, always available) ----
call :build_iexpress
if exist "%HERE%\WinTweaker_Setup.exe" goto :ok

echo.
echo  [X] Installer was NOT created.
echo      A log was saved to build_log.txt next to this script - send it to me.
echo.
pause & exit /b 1

:ok
del "build_log.txt" >nul 2>&1
del "%HERE%\WinTweaker.sed" >nul 2>&1
del "%HERE%\WinTweaker.exe" >nul 2>&1
echo.
echo  ==========================================
echo    [OK] Done!  ->  WinTweaker_Setup.exe
echo  ==========================================
echo.
if exist "%HERE%\WinTweaker_Setup.exe" explorer /select,"%HERE%\WinTweaker_Setup.exe"
pause
exit /b 0

REM ============ subroutines ============

:build_iexpress
echo  [*] Building installer with IExpress...
where iexpress >nul 2>&1
if errorlevel 1 (
    echo  [X] iexpress.exe not found in this Windows.
    > "build_log.txt" echo iexpress.exe not found
    goto :eof
)
copy /y "%PUB%\WinTweaker.exe" "%HERE%\WinTweaker.exe" >nul
set "SED=%HERE%\WinTweaker.sed"
> "%SED%" echo [Version]
>> "%SED%" echo Class=IEXPRESS
>> "%SED%" echo SEDVersion=3
>> "%SED%" echo [Options]
>> "%SED%" echo PackagePurpose=InstallApp
>> "%SED%" echo ShowInstallProgramWindow=0
>> "%SED%" echo HideExtractAnimation=1
>> "%SED%" echo UseLongFileName=1
>> "%SED%" echo InsideCompressed=0
>> "%SED%" echo CAB_FixedSize=0
>> "%SED%" echo CAB_ResvCodeSigning=0
>> "%SED%" echo RebootMode=N
>> "%SED%" echo InstallPrompt=%%InstallPrompt%%
>> "%SED%" echo DisplayLicense=%%DisplayLicense%%
>> "%SED%" echo FinishMessage=%%FinishMessage%%
>> "%SED%" echo TargetName=%%TargetName%%
>> "%SED%" echo FriendlyName=%%FriendlyName%%
>> "%SED%" echo AppLaunched=%%AppLaunched%%
>> "%SED%" echo PostInstallCmd=%%PostInstallCmd%%
>> "%SED%" echo AdminQuitMode=%%AdminQuitMode%%
>> "%SED%" echo [Strings]
>> "%SED%" echo InstallPrompt=
>> "%SED%" echo DisplayLicense=
>> "%SED%" echo FinishMessage=WinTweaker installed. Shortcuts created on Desktop and Start Menu.
>> "%SED%" echo TargetName=%HERE%\WinTweaker_Setup.exe
>> "%SED%" echo FriendlyName=WinTweaker Setup
>> "%SED%" echo AppLaunched=cmd /c wt_install.cmd
>> "%SED%" echo PostInstallCmd=^<None^>
>> "%SED%" echo AdminQuitMode=NoBatch
>> "%SED%" echo FILE0="WinTweaker.exe"
>> "%SED%" echo FILE1="wt_install.cmd"
>> "%SED%" echo [SourceFiles]
>> "%SED%" echo SourceFiles0=%HERE%
>> "%SED%" echo [SourceFiles0]
>> "%SED%" echo %%FILE0%%=
>> "%SED%" echo %%FILE1%%=
iexpress /N /Q "%SED%" > "build_log.txt" 2>&1
goto :eof

:find_iscc
for /f "delims=" %%I in ('where iscc.exe 2^>nul') do if not defined ISCC set "ISCC=%%I"
if not defined ISCC if exist "%PF86%\Inno Setup 6\ISCC.exe" set "ISCC=%PF86%\Inno Setup 6\ISCC.exe"
if not defined ISCC if exist "%PF64%\Inno Setup 6\ISCC.exe" set "ISCC=%PF64%\Inno Setup 6\ISCC.exe"
if not defined ISCC if exist "%PF86%\Inno Setup 5\ISCC.exe" set "ISCC=%PF86%\Inno Setup 5\ISCC.exe"
if not defined ISCC if exist "%PF64%\Inno Setup 5\ISCC.exe" set "ISCC=%PF64%\Inno Setup 5\ISCC.exe"
goto :eof
