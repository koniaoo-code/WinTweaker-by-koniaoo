@echo off
chcp 65001 >nul 2>&1
title WinTweaker Builder by koniaoo

echo.
echo  === WinTweaker by koniaoo ===
echo.

echo [1/4] Checking Python...
python --version >nul 2>&1
if errorlevel 1 (
    echo [ERROR] Python not found! Install Python 3.10+ from python.org
    pause
    exit /b 1
)
echo [OK] Python found

echo.
echo [2/4] Installing dependencies...
pip install customtkinter pillow pyinstaller --quiet --upgrade
if errorlevel 1 (
    echo [ERROR] Failed to install dependencies
    pause
    exit /b 1
)
echo [OK] Done

echo.
echo [3/4] Generating icon...
python generate_icon.py
echo [OK] Icon ready

echo.
echo [4/4] Building WinTweaker.exe ...
echo Please wait 1-3 minutes...

if exist "wintweaker.ico" (
    pyinstaller --onefile --windowed --noconsole --name WinTweaker --icon wintweaker.ico wintweaker.py
) else (
    pyinstaller --onefile --windowed --noconsole --name WinTweaker wintweaker.py
)

if errorlevel 1 (
    echo.
    echo [ERROR] Build failed. Check output above.
    pause
    exit /b 1
)

echo.
echo ================================
echo  SUCCESS! dist\WinTweaker.exe
echo ================================
echo.
echo Run WinTweaker.exe as Administrator!
echo.
pause
