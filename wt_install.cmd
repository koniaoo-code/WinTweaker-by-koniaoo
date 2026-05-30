@echo off
REM Runs inside the IExpress self-extractor (cwd = temp extract dir).
REM Per-user install (no admin needed). %~dp0 = temp dir with WinTweaker.exe.
setlocal
set "DEST=%LocalAppData%\Programs\WinTweaker"
if not exist "%DEST%" mkdir "%DEST%" 2>nul
copy /y "%~dp0WinTweaker.exe" "%DEST%\WinTweaker.exe" >nul

REM Desktop + Start Menu shortcuts
powershell -NoProfile -ExecutionPolicy Bypass -Command "$w=New-Object -ComObject WScript.Shell; $t='%DEST%\WinTweaker.exe'; foreach($p in @([Environment]::GetFolderPath('Desktop'),[Environment]::GetFolderPath('Programs'))){ $s=$w.CreateShortcut($p+'\WinTweaker.lnk'); $s.TargetPath=$t; $s.IconLocation=$t; $s.WorkingDirectory='%DEST%'; $s.Save() }"

REM Register uninstall entry (per-user)
set "UNINS=%DEST%\uninstall.cmd"
> "%UNINS%" echo @echo off
>> "%UNINS%" echo reg delete "HKCU\Software\Microsoft\Windows\CurrentVersion\Uninstall\WinTweaker" /f ^>nul 2^>^&1
>> "%UNINS%" echo del "%%UserProfile%%\Desktop\WinTweaker.lnk" ^>nul 2^>^&1
>> "%UNINS%" echo del "%%AppData%%\Microsoft\Windows\Start Menu\Programs\WinTweaker.lnk" ^>nul 2^>^&1
>> "%UNINS%" echo rd /s /q "%DEST%"
reg add "HKCU\Software\Microsoft\Windows\CurrentVersion\Uninstall\WinTweaker" /v DisplayName /t REG_SZ /d "WinTweaker" /f >nul 2>&1
reg add "HKCU\Software\Microsoft\Windows\CurrentVersion\Uninstall\WinTweaker" /v DisplayIcon /t REG_SZ /d "%DEST%\WinTweaker.exe" /f >nul 2>&1
reg add "HKCU\Software\Microsoft\Windows\CurrentVersion\Uninstall\WinTweaker" /v Publisher /t REG_SZ /d "koniaoo" /f >nul 2>&1
reg add "HKCU\Software\Microsoft\Windows\CurrentVersion\Uninstall\WinTweaker" /v DisplayVersion /t REG_SZ /d "1.16" /f >nul 2>&1
reg add "HKCU\Software\Microsoft\Windows\CurrentVersion\Uninstall\WinTweaker" /v InstallLocation /t REG_SZ /d "%DEST%" /f >nul 2>&1
reg add "HKCU\Software\Microsoft\Windows\CurrentVersion\Uninstall\WinTweaker" /v UninstallString /t REG_SZ /d "cmd /c \"%UNINS%\"" /f >nul 2>&1
exit /b 0
