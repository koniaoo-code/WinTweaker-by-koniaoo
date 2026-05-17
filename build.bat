@echo off
chcp 65001 >nul
title WinTweaker v1.1 - Build
color 0A
echo.
echo  =========================================
echo    WinTweaker v1.1 - build portable .exe
echo    by koniaoo
echo  =========================================
echo.
python --version >nul 2>&1
if errorlevel 1 ( echo [ERROR] Python not found & pause & exit /b )
echo [1/3] Installing dependencies...
pip install customtkinter pillow pyinstaller --quiet --upgrade
echo [2/3] Building .exe...
pyinstaller --onefile --windowed --name "WinTweaker" --uac-admin --icon="icon.ico" --add-data "icon.ico;." wintweaker.py
if errorlevel 1 ( pyinstaller --onefile --windowed --name "WinTweaker" --uac-admin wintweaker.py )
echo.
if exist "dist\WinTweaker.exe" (
    echo  DONE! dist\WinTweaker.exe
    explorer dist
) else ( echo [ERROR] Build failed )
pause
