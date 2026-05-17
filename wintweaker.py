"""
WinTweaker v2.1 — by koniaoo
Fixes: font clarity, log size, Defender, PC Info encoding+speed, UWP real scan
"""
import customtkinter as ctk
import subprocess, threading, json, os, sys, ctypes, webbrowser, tempfile
from datetime import datetime
from pathlib import Path

# ── DPI + font clarity ───────────────────────────────────
try:    ctypes.windll.shcore.SetProcessDpiAwareness(2)
except:
    try: ctypes.windll.user32.SetProcessDPIAware()
    except: pass

def _fix_fonts():
    """Enable ClearType + smooth font rendering via registry."""
    try:
        import winreg
        k = winreg.OpenKey(winreg.HKEY_CURRENT_USER,
                           r"Control Panel\Desktop", 0, winreg.KEY_SET_VALUE)
        winreg.SetValueEx(k, "FontSmoothing",            0, winreg.REG_SZ,    "2")
        winreg.SetValueEx(k, "FontSmoothingType",        0, winreg.REG_DWORD, 2)
        winreg.SetValueEx(k, "FontSmoothingGamma",       0, winreg.REG_DWORD, 1400)
        winreg.SetValueEx(k, "FontSmoothingOrientation", 0, winreg.REG_DWORD, 1)
        winreg.CloseKey(k)
    except: pass
_fix_fonts()

ctk.set_appearance_mode("dark")
ctk.set_default_color_theme("dark-blue")

APP_VERSION = "1.1.0"
GITHUB_URL  = "https://github.com/koniaoo-code/WinTweaker-by-koniaoo"
GITHUB_PROF = "https://github.com/koniaoo-code"
WEBSITE_URL = "https://koniaoo-code.netlify.app"
DISCORD_TAG = "kon1xx_04470"

ORANGE       = "#FF6B00"
ORANGE_HOVER = "#FF8C00"
BG_DARK      = "#0F0F0F"
BG_CARD      = "#1A1A1A"
BG_SIDEBAR   = "#141414"
BG_PANEL     = "#161616"
TEXT_PRIMARY = "#FFFFFF"
TEXT_SEC     = "#888888"
TEXT_MUTED   = "#555555"
GREEN        = "#00C853"
RED          = "#FF3D00"
RED_BG       = "#1A0800"
RED_BORDER   = "#5A1500"

# ── Settings ─────────────────────────────────────────────
def _cfg():
    base = Path(sys.executable).parent if getattr(sys,"frozen",False) else Path(__file__).parent
    return base / "wintweaker_settings.json"

def load_cfg() -> dict:
    try:
        if _cfg().exists():
            with open(_cfg(),"r",encoding="utf-8") as f: return json.load(f)
    except: pass
    return {}

def save_cfg(d: dict):
    try:
        with open(_cfg(),"w",encoding="utf-8") as f: json.dump(d,f,ensure_ascii=False,indent=2)
    except: pass

def is_admin():
    try:    return bool(ctypes.windll.shell32.IsUserAnAdmin())
    except: return False

# ── Run helpers ───────────────────────────────────────────
def run_cmd(cmd: str) -> tuple:
    """Run cmd with UTF-8 forced — no кракозябры."""
    try:
        wrapped = f'chcp 65001 >nul 2>&1 & {cmd}'
        r = subprocess.run(
            wrapped, shell=True, capture_output=True,
            creationflags=subprocess.CREATE_NO_WINDOW, timeout=30
        )
        raw = r.stdout + r.stderr
        out = ""
        for enc in ("utf-8-sig","utf-8","cp1251","cp866","latin-1"):
            try: out = raw.decode(enc).strip(); break
            except: pass
        # Remove chcp output line
        lines = [l for l in out.splitlines()
                 if not l.strip().startswith("Active code page")]
        return r.returncode == 0, "\n".join(lines).strip() or "OK"
    except subprocess.TimeoutExpired: return False, "Timeout"
    except Exception as e: return False, str(e)

def run_ps_utf8(cmd: str, timeout=20) -> str:
    """Run PowerShell with forced UTF-8 — no кракозябры."""
    full = (
        "$OutputEncoding=[System.Text.Encoding]::UTF8;"
        "[Console]::OutputEncoding=[System.Text.Encoding]::UTF8;" + cmd
    )
    try:
        r = subprocess.run(
            ["powershell","-NoProfile","-NonInteractive",
             "-ExecutionPolicy","Bypass","-Command", full],
            capture_output=True,
            creationflags=subprocess.CREATE_NO_WINDOW, timeout=timeout
        )
        raw = r.stdout
        for enc in ("utf-8-sig","utf-8","cp1251","cp866","latin-1"):
            try: return raw.decode(enc).strip()
            except: pass
        return raw.decode("utf-8","replace").strip()
    except: return ""

# ══════════════════════════════════════════════════════════
#  LOCALIZATION
# ══════════════════════════════════════════════════════════
STR = {
"ru": dict(
    by="by koniaoo", gh_link="GitHub профиль ↗",
    performance="Производительность", telemetry="Телеметрия",
    ads="Реклама", cleanup="Очистка", network="Сеть",
    privacy="Приватность", uwp="Удалить UWP приложения",
    pcinfo="Инфо о ПК", about="О программе",
    apply_sec="  ⚡  Применить все твики раздела",
    apply_all="  ⚡  Применить всё",
    admin_ok="● Администратор", admin_no="● Нет прав администратора",
    admin_req="Требуются права\nадминистратора",
    log_lbl="Лог", log_clear="Очистить",
    loading="Загрузка...", refresh="🔄  Обновить",
    enable="Вкл", disable="Выкл",
    no_admin="⚠ Запустите от имени администратора!",
    extreme_title="⚠️  ЭКСТРЕМАЛЬНАЯ ОПТИМИЗАЦИЯ СИСТЕМЫ",
    extreme_warn="МОЖЕТ НАВРЕДИТЬ ВАШЕЙ СИСТЕМЕ — НА СВОЙ СТРАХ И РИСК",
    extreme_apply="⚠️  ПРИМЕНИТЬ (НА СВОЙ СТРАХ И РИСК)",
    extreme_items=[
        "Максимальная схема питания",
        "Отключить все службы телеметрии",
        "Отключить Windows Defender Real-Time",
        "Отключить автообновление Windows",
        "Отключить ненужные службы (SysMain, WSearch)",
        "Очистить все временные файлы и кэши",
        "Отключить Meltdown/Spectre патч (+10% CPU)",
        "Оптимизировать параметры реестра",
    ],
    uwp_scan="🔍  Сканировать установленные UWP приложения",
    uwp_scanning="Сканирование...",
    uwp_remove="Удалить", uwp_removed="Удалено ✓", uwp_err="Ошибка",
    uwp_none="Нет приложений для удаления",
    uwp_warn="Удаление необратимо. Можно восстановить через Microsoft Store.",
    uwp_found="Найдено UWP приложений: ",
    about_dev="Разработчик", about_ver="Версия",
    about_gh="GitHub проект", about_dc="Discord", about_site="Сайт",
    open_gh="Открыть GitHub ↗", open_site="Открыть сайт ↗",
    copy_dc="Скопировать Discord", copied="Скопировано!",
),
"en": dict(
    by="by koniaoo", gh_link="GitHub profile ↗",
    performance="Performance", telemetry="Telemetry",
    ads="Ads & Bloat", cleanup="Cleanup", network="Network",
    privacy="Privacy", uwp="Remove UWP Apps",
    pcinfo="PC Info", about="About",
    apply_sec="  ⚡  Apply all tweaks in section",
    apply_all="  ⚡  Apply All",
    admin_ok="● Administrator", admin_no="● No admin rights",
    admin_req="Administrator\nrights required",
    log_lbl="Log", log_clear="Clear",
    loading="Loading...", refresh="🔄  Refresh",
    enable="On", disable="Off",
    no_admin="⚠ Run as Administrator!",
    extreme_title="⚠️  EXTREME SYSTEM OPTIMIZATION",
    extreme_warn="MAY HARM YOUR SYSTEM — USE AT YOUR OWN RISK",
    extreme_apply="⚠️  APPLY (AT YOUR OWN RISK)",
    extreme_items=[
        "Maximum performance power plan",
        "Disable all telemetry services",
        "Disable Windows Defender Real-Time",
        "Disable Windows Auto-Update",
        "Disable SysMain, WSearch services",
        "Clear all temp files and caches",
        "Disable Meltdown/Spectre patch (+10% CPU)",
        "Optimize registry parameters",
    ],
    uwp_scan="🔍  Scan installed UWP apps",
    uwp_scanning="Scanning...",
    uwp_remove="Remove", uwp_removed="Removed ✓", uwp_err="Error",
    uwp_none="No removable apps found",
    uwp_warn="Removal is permanent. Restore via Microsoft Store.",
    uwp_found="Found UWP apps: ",
    about_dev="Developer", about_ver="Version",
    about_gh="GitHub project", about_dc="Discord", about_site="Website",
    open_gh="Open GitHub ↗", open_site="Open Website ↗",
    copy_dc="Copy Discord tag", copied="Copied!",
),
}

# ── UWP bloatware patterns (safe to remove) ──────────────
UWP_BLOAT_PATTERNS = [
    "Microsoft.XboxApp","Microsoft.XboxGamingOverlay","Microsoft.Xbox.TCUI",
    "Microsoft.XboxIdentityProvider","Microsoft.XboxSpeechToTextOverlay",
    "MicrosoftTeams","Microsoft.Teams",
    "Microsoft.549981C3F5F10",          # Cortana
    "Microsoft.OneDriveSync","Microsoft.SkyDrive",
    "microsoft.windowscommunicationsapps",  # Mail & Calendar
    "Microsoft.WindowsMaps",
    "Microsoft.BingNews","Microsoft.MicrosoftNews",
    "Microsoft.BingWeather",
    "Microsoft.BingFinance","Microsoft.BingSports","Microsoft.BingTravel",
    "Microsoft.ZuneVideo","Microsoft.ZuneMusic",
    "Microsoft.MixedReality.Portal",
    "Microsoft.Microsoft3DViewer",
    "Microsoft.WindowsFeedbackHub",
    "Microsoft.GetHelp",
    "Microsoft.Getstarted",             # Tips
    "Microsoft.MicrosoftStickyNotes",
    "Microsoft.MSPaint",                # Paint 3D
    "Microsoft.MicrosoftSolitaireCollection",
    "Microsoft.YourPhone","Microsoft.Link2Windows",
    "Microsoft.MicrosoftOfficeHub",
    "Microsoft.People",
    "Microsoft.SkypeApp",
    "Microsoft.OneConnect",
    "Microsoft.Wallet",
    "Microsoft.WindowsSoundRecorder",   # Voice Recorder
    "Microsoft.MicrosoftJournal",
    "Microsoft.PowerAutomateDesktop",
    "Microsoft.Todos",
    "Microsoft.Windows.NarratorQuickStart",
    "Microsoft.WindowsFeedback",
    "Windows.Client.WebExperience",     # Copilot / Widgets
    "Clipchamp.Clipchamp",
    "Microsoft.GamingApp",
    "Microsoft.OutlookForWindows",
    "MicrosoftCorporationII.MicrosoftFamily",
    "Microsoft.549981C3F5F10",          # Cortana standalone
    "Microsoft.Office.OneNote",
]

# ══════════════════════════════════════════════════════════
#  TWEAKS DATA
# ══════════════════════════════════════════════════════════
TWEAKS = {
"ru":{
"performance":[
  {"id":"max_power",    "name":"Максимальная производительность питания",        "desc":"Переключает схему питания на «Максимальная производительность»",     "enable":'powercfg /setactive 8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c',"disable":'powercfg /setactive 381b4222-f694-41f0-9685-ff5bb260df2e'},
  {"id":"superfetch",   "name":"Отключить SysMain (Superfetch)",                "desc":"Освобождает ОЗУ — полезно если меньше 8 ГБ",                         "enable":'sc stop SysMain & sc config SysMain start=disabled',"disable":'sc config SysMain start=auto & sc start SysMain'},
  {"id":"search_idx",   "name":"Отключить индексирование поиска",               "desc":"Снижает нагрузку на диск",                                           "enable":'sc stop WSearch & sc config WSearch start=disabled',"disable":'sc config WSearch start=delayed-auto & sc start WSearch'},
  {"id":"visual_fx",    "name":"Отключить визуальные эффекты",                  "desc":"Убирает анимации и тени — интерфейс отзывчивее",                     "enable":r'reg add "HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\VisualEffects" /v VisualFXSetting /t REG_DWORD /d 2 /f',"disable":r'reg add "HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\VisualEffects" /v VisualFXSetting /t REG_DWORD /d 0 /f'},
  {"id":"game_mode",    "name":"Включить Game Mode",                            "desc":"Оптимизирует ресурсы CPU/GPU под игры",                              "enable":r'reg add "HKCU\Software\Microsoft\GameBar" /v AllowAutoGameMode /t REG_DWORD /d 1 /f',"disable":r'reg add "HKCU\Software\Microsoft\GameBar" /v AutoGameModeEnabled /t REG_DWORD /d 0 /f'},
  {"id":"fast_startup", "name":"Быстрый запуск Windows",                        "desc":"Ускоряет загрузку системы через гибернацию ядра",                    "enable":r'reg add "HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Power" /v HiberbootEnabled /t REG_DWORD /d 1 /f',"disable":r'reg add "HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Power" /v HiberbootEnabled /t REG_DWORD /d 0 /f'},
  {"id":"timer_res",    "name":"Высокое разрешение таймера",                    "desc":"Уменьшает задержки — полезно для игр и аудио",                       "enable":r'reg add "HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile" /v SystemResponsiveness /t REG_DWORD /d 0 /f',"disable":r'reg add "HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile" /v SystemResponsiveness /t REG_DWORD /d 20 /f'},
  {"id":"hags",         "name":"Аппаратное ускорение GPU (HAGS)",               "desc":"Снижает задержку GPU в DirectX 12 (Win10 2004+)",                    "enable":r'reg add "HKLM\SYSTEM\CurrentControlSet\Control\GraphicsDrivers" /v HwSchMode /t REG_DWORD /d 2 /f',"disable":r'reg add "HKLM\SYSTEM\CurrentControlSet\Control\GraphicsDrivers" /v HwSchMode /t REG_DWORD /d 1 /f'},
  {"id":"cpu_prio",     "name":"Приоритет CPU для активных программ",           "desc":"ОС отдаёт больше ресурсов активному окну",                           "enable":r'reg add "HKLM\SYSTEM\CurrentControlSet\Control\PriorityControl" /v Win32PrioritySeparation /t REG_DWORD /d 38 /f',"disable":r'reg add "HKLM\SYSTEM\CurrentControlSet\Control\PriorityControl" /v Win32PrioritySeparation /t REG_DWORD /d 2 /f'},
  {"id":"meltdown",     "name":"Отключить Meltdown/Spectre патч ⚠",             "desc":"Снижает безопасность, но даёт +10% CPU — только домашние ПК",        "enable":r'reg add "HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management" /v FeatureSettingsOverride /t REG_DWORD /d 3 /f & reg add "HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management" /v FeatureSettingsOverrideMask /t REG_DWORD /d 3 /f',"disable":r'reg delete "HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management" /v FeatureSettingsOverride /f'},
  {"id":"hibernate",    "name":"Отключить гибернацию",                          "desc":"Удаляет hiberfil.sys — освобождает место",                           "enable":'powercfg /hibernate off',"disable":'powercfg /hibernate on'},
],
"telemetry":[
  {"id":"tel_main",   "name":"Отключить телеметрию Windows",           "desc":"Блокирует отправку данных в Microsoft",                             "enable":r'reg add "HKLM\SOFTWARE\Policies\Microsoft\Windows\DataCollection" /v AllowTelemetry /t REG_DWORD /d 0 /f & sc stop DiagTrack & sc config DiagTrack start=disabled',"disable":r'reg add "HKLM\SOFTWARE\Policies\Microsoft\Windows\DataCollection" /v AllowTelemetry /t REG_DWORD /d 1 /f & sc config DiagTrack start=auto'},
  {"id":"activity",   "name":"Отключить историю активности",           "desc":"Microsoft не хранит историю ваших действий",                       "enable":r'reg add "HKLM\SOFTWARE\Policies\Microsoft\Windows\System" /v EnableActivityFeed /t REG_DWORD /d 0 /f',"disable":r'reg add "HKLM\SOFTWARE\Policies\Microsoft\Windows\System" /v EnableActivityFeed /t REG_DWORD /d 1 /f'},
  {"id":"diagtrack",  "name":"Отключить DiagTrack (Connected UE)",     "desc":"Останавливает главную службу слежки",                              "enable":'sc stop DiagTrack & sc config DiagTrack start=disabled',"disable":'sc config DiagTrack start=auto & sc start DiagTrack'},
  {"id":"compat_tel", "name":"Отключить CompatTelRunner",              "desc":"Останавливает задачи сбора данных совместимости",                   "enable":'schtasks /Change /TN "Microsoft\\Windows\\Application Experience\\Microsoft Compatibility Appraiser" /Disable',"disable":'schtasks /Change /TN "Microsoft\\Windows\\Application Experience\\Microsoft Compatibility Appraiser" /Enable'},
  {"id":"location",   "name":"Отключить геолокацию",                   "desc":"Запрещает приложениям доступ к местоположению",                    "enable":r'reg add "HKLM\SOFTWARE\Policies\Microsoft\Windows\LocationAndSensors" /v DisableLocation /t REG_DWORD /d 1 /f',"disable":r'reg add "HKLM\SOFTWARE\Policies\Microsoft\Windows\LocationAndSensors" /v DisableLocation /t REG_DWORD /d 0 /f'},
  {"id":"feedback",   "name":"Отключить запросы обратной связи",       "desc":"Убирает опросы Microsoft",                                         "enable":r'reg add "HKCU\Software\Microsoft\Siuf\Rules" /v NumberOfSIUFInPeriod /t REG_DWORD /d 0 /f',"disable":r'reg delete "HKCU\Software\Microsoft\Siuf\Rules" /v NumberOfSIUFInPeriod /f'},
  {"id":"adv_id",     "name":"Отключить рекламный ID",                 "desc":"Запрещает использовать рекламный идентификатор",                   "enable":r'reg add "HKCU\Software\Microsoft\Windows\CurrentVersion\AdvertisingInfo" /v Enabled /t REG_DWORD /d 0 /f',"disable":r'reg add "HKCU\Software\Microsoft\Windows\CurrentVersion\AdvertisingInfo" /v Enabled /t REG_DWORD /d 1 /f'},
  {"id":"cortana",    "name":"Отключить Cortana",                      "desc":"Деактивирует голосового ассистента",                               "enable":r'reg add "HKLM\SOFTWARE\Policies\Microsoft\Windows\Windows Search" /v AllowCortana /t REG_DWORD /d 0 /f',"disable":r'reg add "HKLM\SOFTWARE\Policies\Microsoft\Windows\Windows Search" /v AllowCortana /t REG_DWORD /d 1 /f'},
  {"id":"wer",        "name":"Отключить отчёты об ошибках",            "desc":"Прекращает отправку crash-отчётов в Microsoft",                    "enable":r'sc stop WerSvc & sc config WerSvc start=disabled & reg add "HKLM\SOFTWARE\Microsoft\Windows\Windows Error Reporting" /v Disabled /t REG_DWORD /d 1 /f',"disable":'sc config WerSvc start=demand'},
],
"ads":[
  {"id":"start_ads",   "name":"Убрать рекламу в меню Пуск",            "desc":"Отключает рекламные плитки и предложения",                         "enable":r'reg add "HKCU\Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager" /v SystemPaneSuggestionsEnabled /t REG_DWORD /d 0 /f',"disable":r'reg add "HKCU\Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager" /v SystemPaneSuggestionsEnabled /t REG_DWORD /d 1 /f'},
  {"id":"lock_ads",    "name":"Убрать рекламу на экране блокировки",   "desc":"Отключает Windows Spotlight и советы",                             "enable":r'reg add "HKCU\Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager" /v RotatingLockScreenEnabled /t REG_DWORD /d 0 /f',"disable":r'reg add "HKCU\Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager" /v RotatingLockScreenEnabled /t REG_DWORD /d 1 /f'},
  {"id":"tips",        "name":"Отключить советы Windows",              "desc":"Убирает всплывающие подсказки",                                    "enable":r'reg add "HKCU\Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager" /v SoftLandingEnabled /t REG_DWORD /d 0 /f',"disable":r'reg add "HKCU\Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager" /v SoftLandingEnabled /t REG_DWORD /d 1 /f'},
  {"id":"auto_inst",   "name":"Отключить автоустановку приложений",    "desc":"Запрещает тихую установку bloatware",                              "enable":r'reg add "HKCU\Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager" /v SilentInstalledAppsEnabled /t REG_DWORD /d 0 /f',"disable":r'reg add "HKCU\Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager" /v SilentInstalledAppsEnabled /t REG_DWORD /d 1 /f'},
  {"id":"bing",        "name":"Убрать Bing из поиска",                 "desc":"Поиск в Пуске не лезет в интернет",                                "enable":r'reg add "HKCU\Software\Microsoft\Windows\CurrentVersion\Search" /v BingSearchEnabled /t REG_DWORD /d 0 /f',"disable":r'reg add "HKCU\Software\Microsoft\Windows\CurrentVersion\Search" /v BingSearchEnabled /t REG_DWORD /d 1 /f'},
  {"id":"edge_ads",    "name":"Отключить рекламу в Edge",              "desc":"Убирает рекламные плитки на новой вкладке Edge",                   "enable":r'reg add "HKLM\SOFTWARE\Policies\Microsoft\Edge" /v NewTabPageContentEnabled /t REG_DWORD /d 0 /f',"disable":r'reg delete "HKLM\SOFTWARE\Policies\Microsoft\Edge" /v NewTabPageContentEnabled /f'},
],
"cleanup":[
  {"id":"temp",       "name":"Очистить временные файлы",               "desc":"Удаляет файлы из %TEMP% и Windows\\Temp",                          "enable":'cmd /c "del /q /f /s %TEMP%\\* 2>nul & del /q /f /s C:\\Windows\\Temp\\* 2>nul"',"disable":'echo N/A'},
  {"id":"prefetch",   "name":"Очистить Prefetch",                      "desc":"Удаляет кэш предзагрузки",                                         "enable":'del /q /f /s C:\\Windows\\Prefetch\\* 2>nul',"disable":'echo N/A'},
  {"id":"dns_flush",  "name":"Сбросить DNS-кэш",                       "desc":"Очищает кэш DNS",                                                  "enable":'ipconfig /flushdns',"disable":'echo N/A'},
  {"id":"recycle",    "name":"Очистить корзину",                       "desc":"Удаляет все файлы из корзины",                                     "enable":'PowerShell -Command "Clear-RecycleBin -Force -EA SilentlyContinue"',"disable":'echo N/A'},
  {"id":"event_logs", "name":"Очистить журналы событий",               "desc":"Стирает все системные логи",                                       "enable":'PowerShell -Command "wevtutil el | ForEach-Object { wevtutil cl $_ }"',"disable":'echo N/A'},
  {"id":"thumb",      "name":"Очистить кэш миниатюр",                  "desc":"Удаляет кэш превью Проводника",                                    "enable":r'del /f /s /q %LocalAppData%\Microsoft\Windows\Explorer\thumbcache_*.db 2>nul',"disable":'echo N/A'},
  {"id":"upd_cache",  "name":"Очистить кэш обновлений",                "desc":"Освобождает место от загруженных обновлений",                      "enable":'net stop wuauserv 2>nul & rd /s /q C:\\Windows\\SoftwareDistribution\\Download 2>nul & net start wuauserv 2>nul',"disable":'echo N/A'},
],
"network":[
  {"id":"tcp_auto",   "name":"TCP автотюнинг",                         "desc":"Авто-оптимизация TCP буфера — быстрее скачивание",                  "enable":'netsh int tcp set global autotuninglevel=normal',"disable":'netsh int tcp set global autotuninglevel=disabled'},
  {"id":"tcp_fast",   "name":"TCP Fast Open",                          "desc":"Ускоряет установку соединений",                                    "enable":'netsh int tcp set global fastopen=enabled',"disable":'netsh int tcp set global fastopen=disabled'},
  {"id":"dns_goog",   "name":"DNS Google (8.8.8.8)",                   "desc":"Google DNS для всех адаптеров — быстрее резолв",                   "enable":'PowerShell -Command "Get-NetAdapter | Where Status -eq Up | Set-DnsClientServerAddress -ServerAddresses (\'8.8.8.8\',\'8.8.4.4\')"',"disable":'PowerShell -Command "Get-NetAdapter | Where Status -eq Up | Set-DnsClientServerAddress -ResetServerAddresses"'},
  {"id":"ipv6_off",   "name":"Отключить IPv6",                         "desc":"Упрощает стек сети",                                               "enable":r'reg add "HKLM\SYSTEM\CurrentControlSet\Services\Tcpip6\Parameters" /v DisabledComponents /t REG_DWORD /d 255 /f',"disable":r'reg add "HKLM\SYSTEM\CurrentControlSet\Services\Tcpip6\Parameters" /v DisabledComponents /t REG_DWORD /d 0 /f'},
  {"id":"bw_limit",   "name":"Снять лимит полосы (обновления)",        "desc":"Убирает резервирование 20% канала под обновления",                 "enable":r'reg add "HKLM\SOFTWARE\Policies\Microsoft\Windows\Psched" /v NonBestEffortLimit /t REG_DWORD /d 0 /f',"disable":r'reg delete "HKLM\SOFTWARE\Policies\Microsoft\Windows\Psched" /v NonBestEffortLimit /f'},
  {"id":"nagle",      "name":"Отключить алгоритм Нагла",               "desc":"Снижает latency в онлайн-играх",                                   "enable":r'reg add "HKLM\SYSTEM\CurrentControlSet\Services\Tcpip\Parameters" /v TCPNoDelay /t REG_DWORD /d 1 /f',"disable":r'reg delete "HKLM\SYSTEM\CurrentControlSet\Services\Tcpip\Parameters" /v TCPNoDelay /f'},
],
"privacy":[
  {"id":"priv_mic",   "name":"Отключить доступ к микрофону",           "desc":"Запрет для UWP-приложений",                                        "enable":r'reg add "HKCU\Software\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\microphone" /v Value /t REG_SZ /d Deny /f',"disable":r'reg add "HKCU\Software\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\microphone" /v Value /t REG_SZ /d Allow /f'},
  {"id":"priv_cam",   "name":"Отключить доступ к камере",              "desc":"Запрет для UWP-приложений",                                        "enable":r'reg add "HKCU\Software\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\webcam" /v Value /t REG_SZ /d Deny /f',"disable":r'reg add "HKCU\Software\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\webcam" /v Value /t REG_SZ /d Allow /f'},
  {"id":"priv_clip",  "name":"Отключить синхронизацию буфера обмена",  "desc":"Содержимое не уходит в облако Microsoft",                          "enable":r'reg add "HKCU\Software\Microsoft\Clipboard" /v EnableClipboardHistory /t REG_DWORD /d 0 /f',"disable":r'reg add "HKCU\Software\Microsoft\Clipboard" /v EnableClipboardHistory /t REG_DWORD /d 1 /f'},
  {"id":"priv_apps",  "name":"Отключить отслеживание запуска",         "desc":"Windows не ведёт статистику запускаемых приложений",               "enable":r'reg add "HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced" /v Start_TrackProgs /t REG_DWORD /d 0 /f',"disable":r'reg add "HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced" /v Start_TrackProgs /t REG_DWORD /d 1 /f'},
  {"id":"show_ext",   "name":"Показывать расширения файлов",           "desc":"Файлы покажут .exe .py .txt",                                      "enable":r'reg add "HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced" /v HideFileExt /t REG_DWORD /d 0 /f',"disable":r'reg add "HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced" /v HideFileExt /t REG_DWORD /d 1 /f'},
  {"id":"show_hide",  "name":"Показывать скрытые файлы",               "desc":"Скрытые и системные файлы видны в Проводнике",                     "enable":r'reg add "HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced" /v Hidden /t REG_DWORD /d 1 /f',"disable":r'reg add "HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced" /v Hidden /t REG_DWORD /d 2 /f'},
  {"id":"dark_mode",  "name":"Тёмная тема системы",                    "desc":"Тёмный режим для всех элементов и приложений",                     "enable":r'reg add "HKCU\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize" /v AppsUseLightTheme /t REG_DWORD /d 0 /f & reg add "HKCU\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize" /v SystemUsesLightTheme /t REG_DWORD /d 0 /f',"disable":r'reg add "HKCU\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize" /v AppsUseLightTheme /t REG_DWORD /d 1 /f'},
],
},
}

# Build EN from RU (same commands)
def _en_section(ru_list, names, descs):
    return [{**t,"name":names[i] if i<len(names) else t["name"],"desc":descs[i] if i<len(descs) else t["desc"]} for i,t in enumerate(ru_list)]

TWEAKS["en"] = {
"performance":_en_section(TWEAKS["ru"]["performance"],
    ["Max Performance Power Plan","Disable SysMain","Disable Search Indexing","Disable Visual Effects","Enable Game Mode","Fast Startup","High Timer Resolution","GPU Hardware Scheduling","CPU Priority for Active Apps","Disable Meltdown/Spectre Patch ⚠","Disable Hibernation"],
    ["Switches to Maximum Performance power plan","Frees RAM — useful with 8GB or less","Reduces disk load","Removes animations and shadows","Prioritizes CPU/GPU for games","Speeds up boot via kernel hibernation","Reduces latency for gaming/audio","Reduces GPU latency in DX12 (Win10 2004+)","OS gives more CPU to foreground window","Reduces security but gains ~10% CPU perf","Removes hiberfil.sys — frees disk space"]),
"telemetry":_en_section(TWEAKS["ru"]["telemetry"],
    ["Disable Windows Telemetry","Disable Activity History","Disable DiagTrack","Disable CompatTelRunner","Disable Location","Disable Feedback Requests","Disable Advertising ID","Disable Cortana","Disable Error Reporting"],
    ["Blocks usage data from Microsoft","Won't store activity history","Stops main tracking service","Stops compat data collection","No app location access","Removes Microsoft survey popups","No advertising identifier","Disables Cortana voice assistant","Stops crash reports to Microsoft"]),
"ads":_en_section(TWEAKS["ru"]["ads"],
    ["Remove Start Menu Ads","Remove Lock Screen Ads","Disable Tips","Disable Silent App Install","Remove Bing from Search","Disable Edge Ads"],
    ["Disables ad tiles","Disables Windows Spotlight","Removes popups","Prevents silent bloatware","Start search won't use internet","Removes ad tiles in Edge"]),
"cleanup":_en_section(TWEAKS["ru"]["cleanup"],
    ["Clear Temp Files","Clear Prefetch","Flush DNS Cache","Empty Recycle Bin","Clear Event Logs","Clear Thumbnail Cache","Clear Update Cache"],
    ["Deletes from %TEMP% and Windows Temp","Deletes app preload cache","Clears DNS cache","Deletes all recycle bin files","Wipes all event logs","Deletes Explorer preview cache","Frees space from update packages"]),
"network":_en_section(TWEAKS["ru"]["network"],
    ["TCP Autotuning","TCP Fast Open","Google DNS","Disable IPv6","Remove BW Limit","Disable Nagle Algorithm"],
    ["Auto TCP buffer optimization","Faster connection setup","Google DNS for all adapters","Simplifies network stack","Remove 20% bandwidth reservation","Less latency in online games"]),
"privacy":_en_section(TWEAKS["ru"]["privacy"],
    ["Disable Microphone Access","Disable Camera Access","Disable Clipboard Sync","Disable App Launch Tracking","Show File Extensions","Show Hidden Files","Enable Dark Mode"],
    ["Deny mic for UWP apps","Deny webcam for UWP apps","Clipboard won't sync to cloud","Won't track app launches","Show .exe .py .txt etc","Show hidden/system files","Dark mode for all elements"]),
}

SECTION_ICONS = {"performance":"⚡","telemetry":"🔒","ads":"🚫","cleanup":"🧹",
                 "network":"🌐","privacy":"🔏","uwp":"🗑","pcinfo":"💻","about":"ℹ️"}

# ── EXTREME cmd (fixed Defender) ─────────────────────────
EXTREME_CMD = (
    'powercfg /setactive 8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c & '
    'sc stop DiagTrack & sc config DiagTrack start=disabled & '
    'sc stop SysMain & sc config SysMain start=disabled & '
    'sc stop WSearch & sc config WSearch start=disabled & '
    'net stop wuauserv & sc config wuauserv start=disabled & '
    r'reg add "HKLM\SOFTWARE\Policies\Microsoft\Windows\DataCollection" /v AllowTelemetry /t REG_DWORD /d 0 /f & '
    r'reg add "HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile" /v SystemResponsiveness /t REG_DWORD /d 0 /f & '
    r'reg add "HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\VisualEffects" /v VisualFXSetting /t REG_DWORD /d 2 /f & '
    r'reg add "HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management" /v FeatureSettingsOverride /t REG_DWORD /d 3 /f & '
    r'reg add "HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management" /v FeatureSettingsOverrideMask /t REG_DWORD /d 3 /f & '
    r'reg add "HKLM\SOFTWARE\Policies\Microsoft\Windows Defender\Real-Time Protection" /v DisableRealtimeMonitoring /t REG_DWORD /d 1 /f & '
    r'reg add "HKLM\SOFTWARE\Policies\Microsoft\Windows Defender" /v DisableAntiSpyware /t REG_DWORD /d 1 /f & '
    'PowerShell -Command "Set-MpPreference -DisableRealtimeMonitoring $true -ErrorAction SilentlyContinue" & '
    'cmd /c "del /q /f /s %TEMP%\\* 2>nul" & '
    'PowerShell -Command "wevtutil el | ForEach-Object { wevtutil cl $_ }"'
)


# ══════════════════════════════════════════════════════════
#  LANGUAGE DIALOG
# ══════════════════════════════════════════════════════════
class LanguageDialog(ctk.CTkToplevel):
    def __init__(self):
        super().__init__()
        self.result = "ru"
        self.title("WinTweaker"); self.geometry("400x240")
        self.resizable(False,False); self.configure(fg_color=BG_DARK)
        self.grab_set()
        sw,sh = self.winfo_screenwidth(),self.winfo_screenheight()
        self.geometry(f"400x240+{(sw-400)//2}+{(sh-240)//2}")

        lf = ctk.CTkFrame(self,fg_color="transparent"); lf.pack(pady=(24,6))
        ctk.CTkLabel(lf,text="Win",font=("Segoe UI Black",26),text_color=TEXT_PRIMARY).pack(side="left")
        ctk.CTkLabel(lf,text="Tweaker",font=("Segoe UI Black",26),text_color=ORANGE).pack(side="left")
        ctk.CTkLabel(self,text="Выберите язык / Select language",font=("Segoe UI",12),text_color=TEXT_SEC).pack(pady=(0,18))

        bf = ctk.CTkFrame(self,fg_color="transparent"); bf.pack()
        ctk.CTkButton(bf,text="🇷🇺  Русский",width=152,height=42,
                      font=("Segoe UI Bold",13),fg_color=ORANGE,hover_color=ORANGE_HOVER,
                      text_color="#FFF",corner_radius=10,command=lambda:self._pick("ru")).pack(side="left",padx=8)
        ctk.CTkButton(bf,text="🇬🇧  English",width=152,height=42,
                      font=("Segoe UI Bold",13),fg_color="#2A2A2A",hover_color="#3A3A3A",
                      text_color="#FFF",corner_radius=10,command=lambda:self._pick("en")).pack(side="left",padx=8)
        self.protocol("WM_DELETE_WINDOW",lambda:self._pick("ru"))
        self.wait_window()

    def _pick(self,lang):
        self.result=lang; self.destroy()


# ══════════════════════════════════════════════════════════
#  WIDGETS
# ══════════════════════════════════════════════════════════
class AnimatedToggle(ctk.CTkSwitch):
    def __init__(self,master,**kw):
        super().__init__(master,progress_color=ORANGE,button_color="#CCCCCC",
                         button_hover_color="#FFFFFF",fg_color="#333333",
                         width=52,height=26,**kw)

class TweakCard(ctk.CTkFrame):
    def __init__(self,master,tweak,settings,on_toggle,**kw):
        super().__init__(master,fg_color=BG_CARD,corner_radius=10,
                         border_width=1,border_color="#2A2A2A",**kw)
        self.tweak=tweak; self.on_toggle=on_toggle
        self.columnconfigure(0,weight=1)
        ctk.CTkLabel(self,text=tweak["name"],font=("Segoe UI Bold",13),
                     text_color=TEXT_PRIMARY,anchor="w"
                     ).grid(row=0,column=0,sticky="w",padx=16,pady=(12,2))
        ctk.CTkLabel(self,text=tweak["desc"],font=("Segoe UI",12),
                     text_color=TEXT_SEC,anchor="w",wraplength=520
                     ).grid(row=1,column=0,sticky="w",padx=16,pady=(0,12))
        self.var = ctk.BooleanVar(value=bool(settings.get(tweak["id"],False)))
        AnimatedToggle(self,variable=self.var,command=self._toggled,text=""
                       ).grid(row=0,column=1,rowspan=2,padx=16,pady=8)

    def _toggled(self): self.on_toggle(self.tweak,self.var.get())

class ExtremeCard(ctk.CTkFrame):
    def __init__(self,master,S,on_apply,**kw):
        super().__init__(master,fg_color=RED_BG,corner_radius=10,
                         border_width=1,border_color=RED_BORDER,**kw)
        self.columnconfigure(0,weight=1)
        ctk.CTkLabel(self,text=S["extreme_title"],font=("Segoe UI Black",13),
                     text_color=RED,anchor="w").grid(row=0,column=0,sticky="w",padx=16,pady=(14,2))
        ctk.CTkLabel(self,text=S["extreme_warn"],font=("Segoe UI Bold",10),
                     text_color="#FF6644",anchor="w",wraplength=520
                     ).grid(row=1,column=0,sticky="w",padx=16,pady=(0,6))
        itf=ctk.CTkFrame(self,fg_color="transparent")
        itf.grid(row=2,column=0,sticky="w",padx=16,pady=(0,6))
        for it in S["extreme_items"]:
            ctk.CTkLabel(itf,text=f"  ▸ {it}",font=("Segoe UI",11),
                         text_color="#CC4422",anchor="w").pack(anchor="w")
        ctk.CTkButton(self,text=S["extreme_apply"],font=("Segoe UI Bold",12),height=36,
                      fg_color=RED,hover_color="#CC2200",text_color="#FFF",corner_radius=8,
                      command=on_apply).grid(row=3,column=0,padx=16,pady=(4,14),sticky="w")


class LogPanel(ctk.CTkFrame):
    """Compact log panel — height fixed small."""
    def __init__(self,master,**kw):
        super().__init__(master,fg_color="#0D0D0D",corner_radius=0,**kw)
        self.columnconfigure(0,weight=1)
        self.rowconfigure(1,weight=1)
        hdr=ctk.CTkFrame(self,fg_color="transparent")
        hdr.grid(row=0,column=0,sticky="ew",padx=12,pady=(5,0))
        self._lbl=ctk.CTkLabel(hdr,text="Log",font=("Segoe UI Semibold",11),text_color=TEXT_SEC)
        self._lbl.pack(side="left")
        self._clr=ctk.CTkButton(hdr,text="Clear",width=66,height=20,
                                 fg_color="#2A2A2A",hover_color="#333",text_color=TEXT_SEC,
                                 font=("Segoe UI",10),command=self.clear,corner_radius=5)
        self._clr.pack(side="right")
        self.box=ctk.CTkTextbox(self,fg_color="transparent",text_color="#AAAAAA",
                                 font=("Consolas",11),activate_scrollbars=True,border_width=0)
        self.box.grid(row=1,column=0,sticky="nsew",padx=12,pady=(2,5))
        self.box.configure(state="disabled")

    def set_labels(self,lbl,clr):
        self._lbl.configure(text=lbl); self._clr.configure(text=clr)

    def log(self,msg,success=None):
        ts=datetime.now().strftime("%H:%M:%S")
        icon="✓" if success is True else ("✗" if success is False else "►")
        self.box.configure(state="normal")
        self.box.insert("end",f"[{ts}] {icon} {msg}\n")
        self.box.see("end"); self.box.configure(state="disabled")

    def clear(self):
        self.box.configure(state="normal"); self.box.delete("1.0","end")
        self.box.configure(state="disabled")


# ── PC Info Panel ─────────────────────────────────────────
class PCInfoPanel(ctk.CTkFrame):
    def __init__(self,master,S,**kw):
        super().__init__(master,fg_color="transparent",**kw)
        self.S=S; self.columnconfigure(0,weight=1)
        self._show_loading()
        threading.Thread(target=self._load,daemon=True).start()

    def _show_loading(self):
        for w in self.winfo_children(): w.destroy()
        ctk.CTkLabel(self,text=self.S["loading"],font=("Segoe UI",13),
                     text_color=TEXT_SEC).grid(row=0,column=0,pady=40)

    def _load(self):
        # ONE PS call for everything — much faster + UTF-8 forced
        script = r"""
[Console]::OutputEncoding=[System.Text.Encoding]::UTF8
$sep="|||"
try{$os=(Get-WmiObject Win32_OperatingSystem);echo ($os.Caption+' '+$os.Version)}catch{echo "—"}
echo $sep
echo $env:COMPUTERNAME
echo $sep
try{$c=(Get-WmiObject Win32_Processor);echo $c.Name}catch{echo "—"}
echo $sep
try{$c=(Get-WmiObject Win32_Processor);echo $c.NumberOfCores}catch{echo "—"}
echo $sep
try{$c=(Get-WmiObject Win32_Processor);echo $c.NumberOfLogicalProcessors}catch{echo "—"}
echo $sep
try{$c=(Get-WmiObject Win32_Processor);echo $c.MaxClockSpeed}catch{echo "—"}
echo $sep
try{echo ([math]::Round((Get-WmiObject Win32_ComputerSystem).TotalPhysicalMemory/1GB,1))}catch{echo "—"}
echo $sep
try{echo ([math]::Round((Get-WmiObject Win32_OperatingSystem).FreePhysicalMemory/1MB,1))}catch{echo "—"}
echo $sep
try{$g=(Get-WmiObject Win32_VideoController|Select -First 1);echo $g.Name}catch{echo "—"}
echo $sep
try{$g=(Get-WmiObject Win32_VideoController|Select -First 1);echo ([math]::Round($g.AdapterRAM/1GB,1))}catch{echo "—"}
echo $sep
try{Get-WmiObject Win32_LogicalDisk|Where DeviceID -eq 'C:'|%{echo ([math]::Round($_.Size/1GB,0).ToString()+' GB total, '+[math]::Round($_.FreeSpace/1GB,1).ToString()+' GB free')}}catch{echo "—"}
echo $sep
try{echo (Get-WmiObject Win32_BaseBoard).Product}catch{echo "—"}
echo $sep
try{echo (Get-WmiObject Win32_BIOS).SMBIOSBIOSVersion}catch{echo "—"}
echo $sep
try{$b=(gcim Win32_OperatingSystem).LastBootUpTime;$u=(Get-Date)-$b;echo ('{0}d {1}h {2}m' -f $u.Days,$u.Hours,$u.Minutes)}catch{echo "—"}
echo $sep
try{echo (Get-NetIPAddress -AddressFamily IPv4|Where InterfaceAlias -notlike '*Loopback*'|Select -First 1).IPAddress}catch{echo "—"}
"""
        raw = run_ps_utf8(script, timeout=25)
        # Split by ||| separator — each block is one value
        parts = [p.strip() for p in raw.split("|||")]
        # Remove empty first/last, filter internal newlines from each value
        values = []
        for p in parts:
            lines = [l.strip() for l in p.splitlines() if l.strip()]
            values.append(lines[0] if lines else "—")
        keys = ["os","host","cpu","cores","threads","mhz","ram","ram_free",
                "gpu","vram","disk","mobo","bios","uptime","ip"]
        info = {}
        for i,k in enumerate(keys):
            val = values[i] if i < len(values) and values[i] else "—"
            # Fix units
            if k in ("ram","vram") and val != "—":
                val = val + " GB" if not val.endswith("GB") else val
            if k == "mhz" and val != "—":
                val = val + " МГц" if val.isdigit() else val
            info[k] = val
        self.after(0, lambda: self._render(info))

    def _render(self, info: dict):
        for w in self.winfo_children(): w.destroy()

        ctk.CTkButton(self,text=self.S["refresh"],height=28,width=120,
                      font=("Segoe UI",11),fg_color="#2A2A2A",hover_color=ORANGE,
                      text_color=TEXT_SEC,corner_radius=7,command=self._refresh
                      ).grid(row=0,column=0,sticky="e",padx=4,pady=(4,2))

        groups = [
            ("🖥  Система",   [("ОС",info["os"]),("Имя ПК",info["host"]),("Аптайм",info["uptime"])]),
            ("⚡  Процессор", [("CPU",info["cpu"]),("Ядра",info["cores"]),("Потоки",info["threads"]),("МГц (макс)",info["mhz"])]),
            ("🧠  Память",    [("ОЗУ всего",info["ram"]),("ОЗУ своб.",info["ram_free"])]),
            ("🎮  Графика",   [("GPU",info["gpu"]),("VRAM",info["vram"])]),
            ("💾  Диск C:",   [("Диск",info["disk"])]),
            ("🌐  Сеть",      [("IP",info["ip"])]),
            ("🔩  Материнка", [("Плата",info["mobo"]),("BIOS",info["bios"])]),
        ]
        row=1
        for title,items in groups:
            ctk.CTkLabel(self,text=title,font=("Segoe UI Black",12),text_color=ORANGE,anchor="w"
                         ).grid(row=row,column=0,sticky="w",padx=4,pady=(10,2)); row+=1
            card=ctk.CTkFrame(self,fg_color=BG_CARD,corner_radius=10,
                               border_width=1,border_color="#2A2A2A")
            card.grid(row=row,column=0,sticky="ew"); card.columnconfigure(1,weight=1); row+=1
            for i,(k,v) in enumerate(items):
                ctk.CTkLabel(card,text=k,font=("Segoe UI",11),text_color=TEXT_MUTED,
                             anchor="w",width=120).grid(row=i,column=0,sticky="w",padx=(14,6),pady=5)
                ctk.CTkLabel(card,text=str(v),font=("Segoe UI Bold",12),
                             text_color=TEXT_PRIMARY,anchor="w",wraplength=480
                             ).grid(row=i,column=1,sticky="w",padx=6,pady=5)

    def _refresh(self):
        self._show_loading()
        threading.Thread(target=self._load,daemon=True).start()


# ── UWP Panel — real scan ─────────────────────────────────
class UWPPanel(ctk.CTkFrame):
    def __init__(self,master,S,log_fn,**kw):
        super().__init__(master,fg_color="transparent",**kw)
        self.S=S; self.log_fn=log_fn
        self.columnconfigure(0,weight=1)
        self._build()

    def _build(self):
        for w in self.winfo_children(): w.destroy()

        ctk.CTkLabel(self,text=f"⚠  {self.S['uwp_warn']}",
                     font=("Segoe UI",11),text_color="#CC4422",anchor="w",wraplength=620
                     ).grid(row=0,column=0,sticky="w",padx=4,pady=(6,8))

        self._scan_btn = ctk.CTkButton(
            self,text=self.S["uwp_scan"],height=40,
            font=("Segoe UI Bold",13),fg_color=ORANGE,hover_color=ORANGE_HOVER,
            text_color="#FFF",corner_radius=10,command=self._start_scan)
        self._scan_btn.grid(row=1,column=0,sticky="ew",padx=0,pady=(0,10))

        self._list_frame = ctk.CTkFrame(self,fg_color="transparent")
        self._list_frame.grid(row=2,column=0,sticky="ew")
        self._list_frame.columnconfigure(0,weight=1)

    def _start_scan(self):
        self._scan_btn.configure(text=self.S["uwp_scanning"],state="disabled")
        for w in self._list_frame.winfo_children(): w.destroy()
        threading.Thread(target=self._scan,daemon=True).start()

    def _scan(self):
        script = r"""
[Console]::OutputEncoding=[System.Text.Encoding]::UTF8
Get-AppxPackage | Where-Object {
    $_.NonRemovable -ne $true -and
    $_.PackageUserInformation -ne $null -and
    ($_.Publisher -like '*Microsoft*' -or $_.SignatureKind -eq 'Store')
} | Sort-Object Name | ForEach-Object {
    $_.Name + '|' + $_.PackageFullName + '|' + $_.Publisher
}
"""
        raw = run_ps_utf8(script, timeout=20)
        apps = []
        for line in raw.splitlines():
            line = line.strip()
            if not line or "|" not in line: continue
            parts = line.split("|",2)
            if len(parts) < 2: continue
            name, pkg = parts[0].strip(), parts[1].strip()
            pub = parts[2].strip() if len(parts)>2 else ""
            if not name or not pkg: continue
            # Filter: only if matches bloat pattern OR is Microsoft
            is_bloat = any(p.lower() in name.lower() for p in [
                "xbox","teams","cortana","onedrive","maps","news","weather",
                "zune","mixedreality","3dviewer","feedback","gethelp",
                "getstarted","stickynotes","mspaint","solitaire","yourphone",
                "officehub","people","skype","wallet","soundrecorder",
                "journal","powerautomate","todos","clipchamp","copilot",
                "webexperience","onenote","bingfinance","bingsports","family",
                "outlook","gamingapp","549981","narrator"
            ])
            is_ms = "microsoft" in pub.lower() or "microsoft" in name.lower()
            if is_bloat or is_ms:
                apps.append({"name":name,"pkg":pkg,"pub":pub})

        self.after(0, lambda: self._show_apps(apps))

    def _show_apps(self, apps):
        self._scan_btn.configure(text=self.S["uwp_scan"],state="normal")
        for w in self._list_frame.winfo_children(): w.destroy()

        if not apps:
            ctk.CTkLabel(self._list_frame,text=self.S["uwp_none"],
                         font=("Segoe UI",13),text_color=TEXT_SEC
                         ).grid(row=0,column=0,pady=20); return

        ctk.CTkLabel(self._list_frame,
                     text=self.S["uwp_found"] + str(len(apps)),
                     font=("Segoe UI",11),text_color=TEXT_MUTED,anchor="w"
                     ).grid(row=0,column=0,sticky="w",pady=(0,6))

        for i,app in enumerate(apps):
            card=ctk.CTkFrame(self._list_frame,fg_color=BG_CARD,corner_radius=10,
                               border_width=1,border_color="#2A2A2A")
            card.grid(row=i+1,column=0,sticky="ew",pady=3)
            card.columnconfigure(0,weight=1)

            # Friendly display name (strip Microsoft. prefix)
            disp=app["name"].replace("Microsoft.","").replace("MicrosoftCorporationII.","")

            ctk.CTkLabel(card,text=disp,font=("Segoe UI Semibold",12),
                         text_color=TEXT_PRIMARY,anchor="w"
                         ).grid(row=0,column=0,sticky="w",padx=14,pady=(8,2))
            ctk.CTkLabel(card,text=app["pkg"][:60]+"…" if len(app["pkg"])>60 else app["pkg"],
                         font=("Segoe UI",10),text_color=TEXT_MUTED,anchor="w"
                         ).grid(row=1,column=0,sticky="w",padx=14,pady=(0,8))

            btn=ctk.CTkButton(card,text=self.S["uwp_remove"],width=90,height=28,
                               font=("Segoe UI Bold",11),fg_color=RED,
                               hover_color="#CC2200",text_color="#FFF",corner_radius=7)
            btn.configure(command=lambda a=app,b=btn:threading.Thread(
                target=self._remove,args=(a,b),daemon=True).start())
            btn.grid(row=0,column=1,rowspan=2,padx=14,pady=8)

    def _remove(self,app,btn):
        self.after(0,lambda:btn.configure(text="...",state="disabled"))
        name = app["name"].replace("'","")
        ps_cmd = (
            "Get-AppxPackage -Name '*" + name + "*' | Remove-AppxPackage -EA SilentlyContinue;"
            "Get-AppxPackage -AllUsers -Name '*" + name + "*' | Remove-AppxPackage -AllUsers -EA SilentlyContinue"
        )
        try:
            run_ps_utf8(ps_cmd, timeout=15)
            ok = True
        except Exception:
            ok = False
        txt = self.S["uwp_removed"] if ok else self.S["uwp_err"]
        col = GREEN if ok else RED
        self.after(0,lambda:btn.configure(text=txt,fg_color=col,state="disabled"))
        aname = app["name"]
        self.log_fn(f"{aname}: OK" if ok else f"{aname}: error", ok)


# ── About Panel ───────────────────────────────────────────
class AboutPanel(ctk.CTkFrame):
    def __init__(self,master,S,**kw):
        super().__init__(master,fg_color="transparent",**kw)
        self.S=S; self._copy_btn=None
        self.columnconfigure(0,weight=1); self._build()

    def _build(self):
        S = self.S
        self.columnconfigure(0, weight=1)

        # ── Icon (bigger, nicer) ──
        try:
            from PIL import Image, ImageDraw, ImageFilter
            from customtkinter import CTkImage
            sz = 120
            img = Image.new("RGBA", (sz, sz), (0,0,0,0))
            d = ImageDraw.Draw(img)
            # Glow background
            d.ellipse([4,4,sz-4,sz-4], fill=(40,18,0,255))
            d.ellipse([4,4,sz-4,sz-4], outline=(255,107,0,255), width=4)
            # Lightning bold
            pts = [(60,12),(38,62),(55,62),(42,108),(84,52),(63,52),(76,12)]
            d.polygon(pts, fill=(255,107,0,255))
            # Inner highlight
            d.polygon(pts, outline=(255,200,80,160), width=1)
            ph = CTkImage(img, size=(sz, sz))
            lb = ctk.CTkLabel(self, image=ph, text="")
            lb.image = ph
            lb.grid(row=0, column=0, pady=(32,10))
        except:
            ctk.CTkLabel(self, text="⚡", font=("Segoe UI",56)
                         ).grid(row=0, column=0, pady=(32,10))

        # ── Title ──
        nf = ctk.CTkFrame(self, fg_color="transparent")
        nf.grid(row=1, column=0, pady=(0,2))
        ctk.CTkLabel(nf, text="Win",     font=("Segoe UI Black",32),
                     text_color=TEXT_PRIMARY).pack(side="left")
        ctk.CTkLabel(nf, text="Tweaker", font=("Segoe UI Black",32),
                     text_color=ORANGE).pack(side="left")

        ctk.CTkLabel(self, text=f"v{APP_VERSION}  —  Windows Optimizer",
                     font=("Segoe UI",12), text_color=TEXT_MUTED
                     ).grid(row=2, column=0, pady=(2,20))

        # ── Info cards (2 columns grid) ──
        info_outer = ctk.CTkFrame(self, fg_color="transparent")
        info_outer.grid(row=3, column=0, sticky="ew", padx=60, pady=(0,4))
        info_outer.columnconfigure((0,1), weight=1)

        tiles = [
            ("👤", S["about_dev"],  "koniaoo"),
            ("📦", S["about_ver"],  f"v{APP_VERSION}"),
            ("💬", S["about_dc"],   DISCORD_TAG),
            ("🔗", S["about_gh"],   "WinTweaker-by-koniaoo"),
            ("🌐", S["about_site"], "koniaoo-code.netlify.app"),
        ]
        for i, (icon, label, val) in enumerate(tiles):
            col = i % 2
            row_i = i // 2
            tile = ctk.CTkFrame(info_outer, fg_color=BG_CARD,
                                corner_radius=12, border_width=1,
                                border_color="#2A2A2A")
            tile.grid(row=row_i, column=col, padx=5, pady=5, sticky="ew")
            tile.columnconfigure(1, weight=1)

            ctk.CTkLabel(tile, text=icon, font=("Segoe UI",20), width=36,
                         anchor="center"
                         ).grid(row=0, column=0, rowspan=2, padx=(12,6), pady=14)
            ctk.CTkLabel(tile, text=label, font=("Segoe UI",10),
                         text_color=TEXT_MUTED, anchor="w"
                         ).grid(row=0, column=1, sticky="w", padx=(0,10), pady=(10,0))
            ctk.CTkLabel(tile, text=val, font=("Segoe UI Bold",12),
                         text_color=TEXT_PRIMARY, anchor="w", wraplength=220
                         ).grid(row=1, column=1, sticky="w", padx=(0,10), pady=(0,10))

        # ── Buttons ──
        bf = ctk.CTkFrame(self, fg_color="transparent")
        bf.grid(row=4, column=0, pady=22)

        ctk.CTkButton(bf, text=S["open_gh"], width=170, height=42,
                      font=("Segoe UI Bold",13), fg_color=ORANGE,
                      hover_color=ORANGE_HOVER, text_color="#FFF",
                      corner_radius=10,
                      command=lambda: webbrowser.open(GITHUB_URL)
                      ).pack(side="left", padx=8)

        ctk.CTkButton(bf, text=S["open_site"], width=160, height=42,
                      font=("Segoe UI Bold",13), fg_color="#2A2A2A",
                      hover_color="#383838", text_color="#FFF",
                      corner_radius=10,
                      command=lambda: webbrowser.open(WEBSITE_URL)
                      ).pack(side="left", padx=8)

        self._copy_btn = ctk.CTkButton(
            bf, text=S["copy_dc"], width=170, height=42,
            font=("Segoe UI Bold",13), fg_color="#5865F2",
            hover_color="#4752C4", text_color="#FFF",
            corner_radius=10, command=self._copy_dc)
        self._copy_btn.pack(side="left", padx=8)

    def _copy_dc(self):
        try:
            self.clipboard_clear(); self.clipboard_append(DISCORD_TAG)
            self._copy_btn.configure(text=self.S["copied"])
            self.after(2000,lambda:self._copy_btn.configure(text=self.S["copy_dc"]))
        except: pass


# ══════════════════════════════════════════════════════════
#  MAIN APP
# ══════════════════════════════════════════════════════════
class WinTweakerApp(ctk.CTk):
    TWEAK_SECS   = ["performance","telemetry","ads","cleanup","network","privacy"]
    SPECIAL_SECS = ["uwp","pcinfo","about"]

    def __init__(self,lang="ru"):
        super().__init__()
        self.lang=lang; self.S=STR[lang]; self.TD=TWEAKS[lang]
        self.title("WinTweaker — by koniaoo")
        self.geometry("1100x700"); self.minsize(920,580)
        self.configure(fg_color=BG_DARK)
        self._set_icon()
        self.settings=load_cfg(); self.current_sec="performance"
        self.admin_ok=is_admin()
        self._build_ui()
        self._apply_font_fix()
        self._show_section("performance")
        self._update_admin()

    def _apply_font_fix(self):
        """Apply tk-level font smoothing and scaling."""
        try:
            import tkinter.font as tkfont
            tkfont.nametofont("TkDefaultFont").configure(
                family="Segoe UI", size=11, weight="normal")
            tkfont.nametofont("TkTextFont").configure(
                family="Segoe UI", size=11, weight="normal")
            tkfont.nametofont("TkFixedFont").configure(
                family="Consolas", size=11)
            # Scale UI for sharp rendering
            try:
                import tkinter as _tk
                dpi = self.winfo_fpixels("1i")
                scale = round(dpi / 72, 1)
                self.tk.call("tk", "scaling", scale)
            except: pass
        except: pass

    def _set_icon(self):
        try:
            from PIL import Image,ImageDraw
            img=Image.new("RGBA",(64,64),(0,0,0,0)); d=ImageDraw.Draw(img)
            d.ellipse([2,2,62,62],fill=(20,20,20,255))
            d.polygon([(32,6),(20,34),(30,34),(22,58),(46,28),(34,28),(42,6)],fill=(255,107,0,255))
            tmp=tempfile.NamedTemporaryFile(suffix=".ico",delete=False)
            img.save(tmp.name,format="ICO",sizes=[(64,64)]); tmp.close()
            self.iconbitmap(tmp.name)
        except: pass

    def _build_ui(self):
        self.grid_columnconfigure(1,weight=1); self.grid_rowconfigure(0,weight=1)
        S=self.S

        # SIDEBAR
        sb=ctk.CTkFrame(self,fg_color=BG_SIDEBAR,width=228,corner_radius=0)
        sb.grid(row=0,column=0,sticky="nsew"); sb.grid_propagate(False)
        sb.grid_rowconfigure(15,weight=1)

        lf=ctk.CTkFrame(sb,fg_color="transparent"); lf.grid(row=0,column=0,padx=20,pady=(22,2),sticky="w")
        ctk.CTkLabel(lf,text="Win",font=("Segoe UI Black",22),text_color=TEXT_PRIMARY).pack(side="left")
        ctk.CTkLabel(lf,text="Tweaker",font=("Segoe UI Black",22),text_color=ORANGE).pack(side="left")
        ctk.CTkLabel(sb,text=S["by"],font=("Segoe UI",11),text_color=TEXT_MUTED).grid(row=1,column=0,padx=20,pady=(0,2),sticky="w")
        ctk.CTkButton(sb,text=S["gh_link"],height=22,font=("Segoe UI",10),
                      fg_color="transparent",hover_color="#1E1E1E",text_color=ORANGE,
                      anchor="w",corner_radius=6,command=lambda:webbrowser.open(GITHUB_PROF)
                      ).grid(row=2,column=0,padx=14,pady=(0,10),sticky="w")

        self.nav_btns={}
        for i,sec in enumerate(self.TWEAK_SECS+self.SPECIAL_SECS):
            btn=ctk.CTkButton(sb,text=f"  {SECTION_ICONS.get(sec,'•')}  {S.get(sec,sec)}",
                              anchor="w",font=("Segoe UI Semibold",13),fg_color="transparent",
                              hover_color="#2A2A2A",text_color=TEXT_SEC,height=42,corner_radius=8,
                              command=lambda s=sec:self._show_section(s))
            btn.grid(row=3+i,column=0,padx=10,pady=2,sticky="ew")
            self.nav_btns[sec]=btn

        af=ctk.CTkFrame(sb,fg_color="transparent")
        af.grid(row=3+len(self.TWEAK_SECS+self.SPECIAL_SECS),column=0,padx=10,pady=14,sticky="sew")
        ctk.CTkButton(af,text=S["apply_all"],font=("Segoe UI Bold",14),fg_color=ORANGE,
                      hover_color=ORANGE_HOVER,text_color="#FFF",height=48,corner_radius=10,
                      command=self._apply_all).pack(fill="x")
        self.admin_badge=ctk.CTkLabel(af,text="",font=("Segoe UI",10),text_color=TEXT_MUTED)
        self.admin_badge.pack(pady=(3,0))

        # RIGHT
        right=ctk.CTkFrame(self,fg_color=BG_PANEL,corner_radius=0)
        right.grid(row=0,column=1,sticky="nsew")
        right.grid_rowconfigure(1,weight=1); right.grid_columnconfigure(0,weight=1)

        hf=ctk.CTkFrame(right,fg_color="transparent")
        hf.grid(row=0,column=0,sticky="ew"); hf.grid_columnconfigure(0,weight=1)
        hi=ctk.CTkFrame(hf,fg_color="transparent")
        hi.grid(row=0,column=0,sticky="ew",padx=24,pady=14); hi.grid_columnconfigure(0,weight=1)
        self.sec_title=ctk.CTkLabel(hi,text="",font=("Segoe UI Bold",18),text_color=TEXT_PRIMARY,anchor="w")
        self.sec_title.grid(row=0,column=0,sticky="w")
        self.admin_dot=ctk.CTkLabel(hi,text=S["admin_ok"],font=("Segoe UI Semibold",11),text_color=GREEN)
        self.admin_dot.grid(row=0,column=1,sticky="e")
        self.apply_sec_btn=ctk.CTkButton(hf,text=S["apply_sec"],
                                          font=("Segoe UI Bold",14),
                                          fg_color=ORANGE,hover_color=ORANGE_HOVER,
                                          text_color="#FFF",height=42,corner_radius=0,
                                          command=self._apply_section)
        self.apply_sec_btn.grid(row=1,column=0,sticky="ew")

        self.scroll=ctk.CTkScrollableFrame(right,fg_color="transparent",
                                            scrollbar_button_color="#2A2A2A",
                                            scrollbar_button_hover_color=ORANGE)
        self.scroll.grid(row=1,column=0,sticky="nsew",padx=16,pady=6)
        self.scroll.grid_columnconfigure(0,weight=1)

        # LOG — small height
        self.log=LogPanel(right,height=62)
        self.log.grid(row=2,column=0,sticky="ew")
        self.log.set_labels(S["log_lbl"],S["log_clear"])

    def _show_section(self,sec):
        self.current_sec=sec
        for s,b in self.nav_btns.items():
            b.configure(fg_color="#2A1A00" if s==sec else "transparent",
                        text_color=ORANGE if s==sec else TEXT_SEC)
        self.sec_title.configure(text=f"{SECTION_ICONS.get(sec,'•')}  {self.S.get(sec,sec)}")

        self.apply_sec_btn.grid() if sec not in self.SPECIAL_SECS else self.apply_sec_btn.grid_remove()
        for w in self.scroll.winfo_children(): w.destroy()

        if sec=="pcinfo":
            p=PCInfoPanel(self.scroll,self.S); p.grid(row=0,column=0,sticky="ew",padx=0,pady=0); self.scroll.grid_rowconfigure(0,weight=0); return
        if sec=="about":
            p=AboutPanel(self.scroll,self.S); p.grid(row=0,column=0,sticky="ew",padx=0,pady=0); self.scroll.grid_rowconfigure(0,weight=0); return
        if sec=="uwp":
            p=UWPPanel(self.scroll,self.S,self.log.log); p.grid(row=0,column=0,sticky="ew",padx=0,pady=0); self.scroll.grid_rowconfigure(0,weight=0); return

        if sec=="performance":
            ExtremeCard(self.scroll,self.S,self._apply_extreme).grid(sticky="ew",pady=4)
            self.scroll.grid_columnconfigure(0,weight=1)
        for t in self.TD.get(sec,[]):
            TweakCard(self.scroll,t,self.settings,self._on_toggle).grid(sticky="ew",pady=4)
            self.scroll.grid_columnconfigure(0,weight=1)

    def _on_toggle(self,tweak,state):
        self.settings[tweak["id"]]=state; save_cfg(self.settings)
        cmd=tweak["enable"] if state else tweak["disable"]
        self.log.log(f"{self.S['enable'] if state else self.S['disable']}: {tweak['name']}...")
        threading.Thread(target=self._run,args=(tweak["name"],cmd),daemon=True).start()

    def _run(self,name,cmd):
        ok,out=run_cmd(cmd)
        self.after(0,lambda:self.log.log(f"{'OK' if ok else 'ERR'}: {out[:80]}",ok))

    def _apply_section(self):
        tweaks=self.TD.get(self.current_sec,[])
        threading.Thread(target=self._bulk,args=(tweaks,),daemon=True).start()

    def _apply_all(self):
        threading.Thread(target=self._bulk,args=([t for s in self.TD.values() for t in s],),daemon=True).start()

    def _bulk(self,tweaks):
        for t in tweaks:
            cmd=t["enable"] if self.settings.get(t["id"],False) else t["disable"]
            self.after(0,lambda n=t["name"]:self.log.log(f"► {n}..."))
            ok,out=run_cmd(cmd)
            self.after(0,lambda o=out,s=ok:self.log.log(f"  {o[:80]}",s))

    def _apply_extreme(self):
        self.log.log("⚠️ EXTREME optimization running...")
        def _do():
            ok, out = run_cmd(EXTREME_CMD)
            msg = "Extreme: OK — reboot recommended!" if ok else f"Extreme ERR: {out[:60]}"
            self.after(0, lambda: self.log.log(msg, ok))
        threading.Thread(target=_do, daemon=True).start()

    def _update_admin(self):
        S=self.S
        if self.admin_ok:
            self.admin_dot.configure(text=S["admin_ok"],text_color=GREEN)
            self.admin_badge.configure(text="")
        else:
            self.admin_dot.configure(text=S["admin_no"],text_color=RED)
            self.admin_badge.configure(text=S["admin_req"],text_color=RED)
            self.log.log(S["no_admin"],False)


# ══════════════════════════════════════════════════════════
if __name__=="__main__":
    cfg=load_cfg(); lang=cfg.get("lang",None)
    if lang is None:
        root=ctk.CTk(); root.withdraw()
        dlg=LanguageDialog(); lang=dlg.result or "ru"
        root.destroy(); cfg["lang"]=lang; save_cfg(cfg)
    WinTweakerApp(lang=lang).mainloop()
