import customtkinter as ctk
import subprocess
import threading
import json
import os
import sys
import ctypes
from datetime import datetime
from pathlib import Path

# ── настройка тем ──────────────────────────────────────────────────────────────
ctk.set_appearance_mode("dark")
ctk.set_default_color_theme("dark-blue")

ORANGE       = "#FF6B00"
ORANGE_HOVER = "#FF8C00"
ORANGE_DIM   = "#CC5500"
BG_DARK      = "#0F0F0F"
BG_CARD      = "#1A1A1A"
BG_SIDEBAR   = "#141414"
BG_PANEL     = "#161616"
TEXT_PRIMARY  = "#FFFFFF"
TEXT_SECONDARY = "#888888"
TEXT_MUTED   = "#555555"
GREEN        = "#00C853"
RED          = "#FF3D00"

# ── путь для хранения настроек ─────────────────────────────────────────────────
def get_settings_path():
    if getattr(sys, 'frozen', False):
        base = Path(sys.executable).parent
    else:
        base = Path(__file__).parent
    return base / "wintweaker_settings.json"

def load_settings():
    path = get_settings_path()
    if path.exists():
        try:
            with open(path, "r", encoding="utf-8") as f:
                return json.load(f)
        except Exception:
            pass
    return {}

def save_settings(data: dict):
    path = get_settings_path()
    try:
        with open(path, "w", encoding="utf-8") as f:
            json.dump(data, f, ensure_ascii=False, indent=2)
    except Exception as e:
        print(f"Ошибка сохранения настроек: {e}")

# ── проверка прав администратора ───────────────────────────────────────────────
def is_admin():
    try:
        return ctypes.windll.shell32.IsUserAnAdmin()
    except Exception:
        return False

# ── иконка приложения (base64 PNG 64×64) ──────────────────────────────────────
ICON_B64 = (
    "iVBORw0KGgoAAAANSUhEUgAAAEAAAABACAYAAACqaXHeAAAACXBIWXMAAAsTAAALEwEAmpwY"
    "AAAF8WlUWHRYTUw6Y29tLmFkb2JlLnhtcAAAAAAAPD94cGFja2V0IGJlZ2luPSLvu78iIGlk"
    "PSJXNU0wTXBDZWhpSHpyZVN6TlRjemtjOWQiPz4gPHg6eG1wbWV0YSB4bWxuczp4PSJhZG9i"
    "ZTpuczptZXRhLyIgeDp4bXB0az0iQWRvYmUgWE1QIENvcmUgOS4xLWMwMDIgNzkuZjM1NGVm"
    "YywgMjAyMy8xMS8wOS0xMjowNTo1MyAgICAgICAgIj4gPHJkZjpSREYgeG1sbnM6cmRmPSJo"
    "dHRwOi8vd3d3LnczLm9yZy8xOTk5LzAyLzIyLXJkZi1zeW50YXgtbnMjIj4gPHJkZjpEZXNj"
    "cmlwdGlvbiByZGY6YWJvdXQ9IiIgeG1sbnM6eG1wPSJodHRwOi8vbnMuYWRvYmUuY29tL3hh"
    "cC8xLjAvIiB4bWxuczpkYz0iaHR0cDovL3B1cmwub3JnL2RjL2VsZW1lbnRzLzEuMS8iIHht"
    "bG5zOnBob3Rvc2hvcD0iaHR0cDovL25zLmFkb2JlLmNvbS9waG90b3Nob3AvMS4wLyIgeG1s"
    "bnM6eG1wTU09Imh0dHA6Ly9ucy5hZG9iZS5jb20veGFwLzEuMC9tbS8iIHhtbG5zOnN0RXZt"
    "PSJodHRwOi8vbnMuYWRvYmUuY29tL3hhcC8xLjAvc1R5cGUvUmVzb3VyY2VFdmVudCMiPiA8"
)  # укороченная заглушка — реальная иконка генерируется ниже через Pillow

# ── твики ─────────────────────────────────────────────────────────────────────
TWEAKS = {
    "performance": [
        {
            "id": "max_power",
            "name": "Максимальная производительность питания",
            "desc": "Переключает схему питания на «Максимальная производительность»",
            "enable":  'powercfg /setactive 8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c',
            "disable": 'powercfg /setactive 381b4222-f694-41f0-9685-ff5bb260df2e',
        },
        {
            "id": "superfetch",
            "name": "Отключить SysMain (Superfetch)",
            "desc": "Освобождает ОЗУ — полезно если меньше 8 ГБ",
            "enable":  'sc stop SysMain & sc config SysMain start=disabled',
            "disable": 'sc config SysMain start=auto & sc start SysMain',
        },
        {
            "id": "search_index",
            "name": "Отключить индексирование поиска",
            "desc": "Снижает нагрузку на диск — можно отключить если не используешь поиск",
            "enable":  'sc stop WSearch & sc config WSearch start=disabled',
            "disable": 'sc config WSearch start=delayed-auto & sc start WSearch',
        },
        {
            "id": "visual_effects",
            "name": "Отключить визуальные эффекты",
            "desc": "Убирает анимации и тени — ускоряет интерфейс",
            "enable":  r'reg add "HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\VisualEffects" /v VisualFXSetting /t REG_DWORD /d 2 /f',
            "disable": r'reg add "HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\VisualEffects" /v VisualFXSetting /t REG_DWORD /d 0 /f',
        },
        {
            "id": "game_mode",
            "name": "Включить Game Mode",
            "desc": "Оптимизирует ресурсы системы под игры",
            "enable":  r'reg add "HKCU\Software\Microsoft\GameBar" /v AllowAutoGameMode /t REG_DWORD /d 1 /f & reg add "HKCU\Software\Microsoft\GameBar" /v AutoGameModeEnabled /t REG_DWORD /d 1 /f',
            "disable": r'reg add "HKCU\Software\Microsoft\GameBar" /v AutoGameModeEnabled /t REG_DWORD /d 0 /f',
        },
        {
            "id": "fast_startup",
            "name": "Быстрый запуск Windows",
            "desc": "Ускоряет загрузку системы через гибернацию ядра",
            "enable":  r'reg add "HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Power" /v HiberbootEnabled /t REG_DWORD /d 1 /f',
            "disable": r'reg add "HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Power" /v HiberbootEnabled /t REG_DWORD /d 0 /f',
        },
        {
            "id": "meltdown_fix",
            "name": "Отключить Meltdown/Spectre патч (прирост FPS)",
            "desc": "⚠ Снижает безопасность, но даёт прирост производительности до 10%",
            "enable":  r'reg add "HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management" /v FeatureSettingsOverride /t REG_DWORD /d 3 /f & reg add "HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management" /v FeatureSettingsOverrideMask /t REG_DWORD /d 3 /f',
            "disable": r'reg delete "HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management" /v FeatureSettingsOverride /f & reg delete "HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management" /v FeatureSettingsOverrideMask /f',
        },
        {
            "id": "timer_resolution",
            "name": "Высокое разрешение таймера",
            "desc": "Уменьшает задержки — полезно для игр и аудио",
            "enable":  r'reg add "HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile" /v SystemResponsiveness /t REG_DWORD /d 0 /f',
            "disable": r'reg add "HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile" /v SystemResponsiveness /t REG_DWORD /d 20 /f',
        },
    ],
    "telemetry": [
        {
            "id": "telemetry_off",
            "name": "Отключить телеметрию Windows",
            "desc": "Блокирует отправку данных об использовании в Microsoft",
            "enable":  r'reg add "HKLM\SOFTWARE\Policies\Microsoft\Windows\DataCollection" /v AllowTelemetry /t REG_DWORD /d 0 /f & sc stop DiagTrack & sc config DiagTrack start=disabled',
            "disable": r'reg add "HKLM\SOFTWARE\Policies\Microsoft\Windows\DataCollection" /v AllowTelemetry /t REG_DWORD /d 1 /f & sc config DiagTrack start=auto & sc start DiagTrack',
        },
        {
            "id": "activity_history",
            "name": "Отключить историю активности",
            "desc": "Microsoft не будет хранить историю ваших действий",
            "enable":  r'reg add "HKLM\SOFTWARE\Policies\Microsoft\Windows\System" /v EnableActivityFeed /t REG_DWORD /d 0 /f & reg add "HKLM\SOFTWARE\Policies\Microsoft\Windows\System" /v PublishUserActivities /t REG_DWORD /d 0 /f',
            "disable": r'reg add "HKLM\SOFTWARE\Policies\Microsoft\Windows\System" /v EnableActivityFeed /t REG_DWORD /d 1 /f & reg add "HKLM\SOFTWARE\Policies\Microsoft\Windows\System" /v PublishUserActivities /t REG_DWORD /d 1 /f',
        },
        {
            "id": "compat_telemetry",
            "name": "Отключить совместимость (CompatTelRunner)",
            "desc": "Останавливает задачи сбора данных совместимости",
            "enable":  'schtasks /Change /TN "Microsoft\\Windows\\Application Experience\\Microsoft Compatibility Appraiser" /Disable & schtasks /Change /TN "Microsoft\\Windows\\Application Experience\\ProgramDataUpdater" /Disable',
            "disable": 'schtasks /Change /TN "Microsoft\\Windows\\Application Experience\\Microsoft Compatibility Appraiser" /Enable',
        },
        {
            "id": "location",
            "name": "Отключить геолокацию",
            "desc": "Запрещает приложениям доступ к вашему местоположению",
            "enable":  r'reg add "HKLM\SOFTWARE\Policies\Microsoft\Windows\LocationAndSensors" /v DisableLocation /t REG_DWORD /d 1 /f',
            "disable": r'reg add "HKLM\SOFTWARE\Policies\Microsoft\Windows\LocationAndSensors" /v DisableLocation /t REG_DWORD /d 0 /f',
        },
        {
            "id": "feedback",
            "name": "Отключить запросы обратной связи",
            "desc": "Убирает раздражающие опросы Microsoft",
            "enable":  r'reg add "HKCU\Software\Microsoft\Siuf\Rules" /v NumberOfSIUFInPeriod /t REG_DWORD /d 0 /f & reg add "HKCU\Software\Microsoft\Siuf\Rules" /v PeriodInNanoSeconds /t REG_DWORD /d 0 /f',
            "disable": r'reg delete "HKCU\Software\Microsoft\Siuf\Rules" /v NumberOfSIUFInPeriod /f',
        },
        {
            "id": "advertising_id",
            "name": "Отключить рекламный ID",
            "desc": "Запрещает приложениям использовать уникальный рекламный идентификатор",
            "enable":  r'reg add "HKCU\Software\Microsoft\Windows\CurrentVersion\AdvertisingInfo" /v Enabled /t REG_DWORD /d 0 /f',
            "disable": r'reg add "HKCU\Software\Microsoft\Windows\CurrentVersion\AdvertisingInfo" /v Enabled /t REG_DWORD /d 1 /f',
        },
        {
            "id": "cortana",
            "name": "Отключить Cortana",
            "desc": "Полностью деактивирует голосового ассистента Cortana",
            "enable":  r'reg add "HKLM\SOFTWARE\Policies\Microsoft\Windows\Windows Search" /v AllowCortana /t REG_DWORD /d 0 /f',
            "disable": r'reg add "HKLM\SOFTWARE\Policies\Microsoft\Windows\Windows Search" /v AllowCortana /t REG_DWORD /d 1 /f',
        },
        {
            "id": "error_reporting",
            "name": "Отключить отчёты об ошибках",
            "desc": "Прекращает отправку crash-отчётов в Microsoft",
            "enable":  r'sc stop WerSvc & sc config WerSvc start=disabled & reg add "HKLM\SOFTWARE\Microsoft\Windows\Windows Error Reporting" /v Disabled /t REG_DWORD /d 1 /f',
            "disable": r'sc config WerSvc start=demand & reg add "HKLM\SOFTWARE\Microsoft\Windows\Windows Error Reporting" /v Disabled /t REG_DWORD /d 0 /f',
        },
    ],
    "ads": [
        {
            "id": "start_ads",
            "name": "Убрать рекламу в меню Пуск",
            "desc": "Отключает предложения приложений и рекламные плитки",
            "enable":  r'reg add "HKCU\Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager" /v SystemPaneSuggestionsEnabled /t REG_DWORD /d 0 /f & reg add "HKCU\Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager" /v SubscribedContent-338388Enabled /t REG_DWORD /d 0 /f',
            "disable": r'reg add "HKCU\Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager" /v SystemPaneSuggestionsEnabled /t REG_DWORD /d 1 /f',
        },
        {
            "id": "lockscreen_ads",
            "name": "Убрать рекламу на экране блокировки",
            "desc": "Отключает Windows Spotlight и рекламные советы",
            "enable":  r'reg add "HKCU\Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager" /v RotatingLockScreenEnabled /t REG_DWORD /d 0 /f & reg add "HKCU\Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager" /v SubscribedContent-338387Enabled /t REG_DWORD /d 0 /f',
            "disable": r'reg add "HKCU\Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager" /v RotatingLockScreenEnabled /t REG_DWORD /d 1 /f',
        },
        {
            "id": "tips_tricks",
            "name": "Отключить советы и подсказки Windows",
            "desc": "Убирает всплывающие подсказки и уведомления",
            "enable":  r'reg add "HKCU\Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager" /v SoftLandingEnabled /t REG_DWORD /d 0 /f & reg add "HKCU\Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager" /v SubscribedContent-338389Enabled /t REG_DWORD /d 0 /f',
            "disable": r'reg add "HKCU\Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager" /v SoftLandingEnabled /t REG_DWORD /d 1 /f',
        },
        {
            "id": "auto_install",
            "name": "Отключить автоустановку приложений",
            "desc": "Запрещает Microsoft устанавливать bloatware без спроса",
            "enable":  r'reg add "HKCU\Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager" /v SilentInstalledAppsEnabled /t REG_DWORD /d 0 /f',
            "disable": r'reg add "HKCU\Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager" /v SilentInstalledAppsEnabled /t REG_DWORD /d 1 /f',
        },
        {
            "id": "bing_search",
            "name": "Убрать Bing из поиска Windows",
            "desc": "Поиск в меню Пуск не будет обращаться к интернету",
            "enable":  r'reg add "HKCU\Software\Microsoft\Windows\CurrentVersion\Search" /v BingSearchEnabled /t REG_DWORD /d 0 /f & reg add "HKCU\Software\Microsoft\Windows\CurrentVersion\Search" /v CortanaConsent /t REG_DWORD /d 0 /f',
            "disable": r'reg add "HKCU\Software\Microsoft\Windows\CurrentVersion\Search" /v BingSearchEnabled /t REG_DWORD /d 1 /f',
        },
        {
            "id": "edge_ads",
            "name": "Отключить рекламу в Edge",
            "desc": "Убирает рекламные плитки на новой вкладке Edge",
            "enable":  r'reg add "HKLM\SOFTWARE\Policies\Microsoft\Edge" /v NewTabPageContentEnabled /t REG_DWORD /d 0 /f & reg add "HKLM\SOFTWARE\Policies\Microsoft\Edge" /v NewTabPageAllowedBackgroundTypes /t REG_DWORD /d 3 /f',
            "disable": r'reg delete "HKLM\SOFTWARE\Policies\Microsoft\Edge" /v NewTabPageContentEnabled /f',
        },
    ],
    "cleanup": [
        {
            "id": "temp_files",
            "name": "Очистить временные файлы",
            "desc": "Удаляет файлы из %TEMP% и C:\\Windows\\Temp",
            "enable":  'cmd /c "del /q /f /s %TEMP%\\* 2>nul & del /q /f /s C:\\Windows\\Temp\\* 2>nul"',
            "disable": 'echo Отмена не применима',
        },
        {
            "id": "prefetch",
            "name": "Очистить Prefetch",
            "desc": "Удаляет кэш предзагрузки приложений",
            "enable":  'del /q /f /s C:\\Windows\\Prefetch\\* 2>nul',
            "disable": 'echo Отмена не применима',
        },
        {
            "id": "dns_flush",
            "name": "Сбросить DNS-кэш",
            "desc": "Очищает кэш DNS — полезно при проблемах с сетью",
            "enable":  'ipconfig /flushdns',
            "disable": 'echo Отмена не применима',
        },
        {
            "id": "recycle_bin",
            "name": "Очистить корзину",
            "desc": "Удаляет все файлы из корзины",
            "enable":  'PowerShell -Command "Clear-RecycleBin -Force -ErrorAction SilentlyContinue"',
            "disable": 'echo Отмена не применима',
        },
        {
            "id": "event_logs",
            "name": "Очистить журналы событий",
            "desc": "Стирает все системные логи Windows",
            "enable":  'wevtutil el | ForEach-Object { wevtutil cl "$_" } 2>nul',
            "disable": 'echo Отмена не применима',
        },
        {
            "id": "thumbnail_cache",
            "name": "Очистить кэш миниатюр",
            "desc": "Удаляет кэш превью файлов Проводника",
            "enable":  r'del /f /s /q %LocalAppData%\Microsoft\Windows\Explorer\thumbcache_*.db 2>nul',
            "disable": 'echo Отмена не применима',
        },
        {
            "id": "windows_update_cache",
            "name": "Очистить кэш обновлений Windows",
            "desc": "Освобождает место от загруженных обновлений",
            "enable":  'net stop wuauserv 2>nul & rd /s /q C:\\Windows\\SoftwareDistribution\\Download 2>nul & net start wuauserv 2>nul',
            "disable": 'echo Отмена не применима',
        },
    ],
}

SECTION_ICONS = {
    "performance": "⚡",
    "telemetry":   "🔒",
    "ads":         "🚫",
    "cleanup":     "🧹",
}

SECTION_LABELS = {
    "performance": "Производительность",
    "telemetry":   "Телеметрия",
    "ads":         "Реклама",
    "cleanup":     "Очистка",
}

# ── утилиты ───────────────────────────────────────────────────────────────────
def run_cmd(cmd: str) -> tuple[bool, str]:
    """Выполнить команду через cmd.exe, вернуть (успех, вывод)."""
    try:
        result = subprocess.run(
            ["cmd", "/c", cmd],
            capture_output=True, text=True,
            creationflags=subprocess.CREATE_NO_WINDOW,
            timeout=30,
        )
        out = (result.stdout + result.stderr).strip()
        return result.returncode == 0, out or "OK"
    except subprocess.TimeoutExpired:
        return False, "Таймаут выполнения"
    except Exception as e:
        return False, str(e)

# ── виджеты ──────────────────────────────────────────────────────────────────
class AnimatedToggle(ctk.CTkSwitch):
    """Обёртка над CTkSwitch с оранжевым цветом."""
    def __init__(self, master, **kw):
        super().__init__(
            master,
            progress_color=ORANGE,
            button_color="#CCCCCC",
            button_hover_color="#FFFFFF",
            fg_color="#333333",
            width=52, height=26,
            **kw,
        )

class TweakCard(ctk.CTkFrame):
    def __init__(self, master, tweak: dict, settings: dict, on_toggle, **kw):
        super().__init__(master, fg_color=BG_CARD, corner_radius=10,
                         border_width=1, border_color="#2A2A2A", **kw)
        self.tweak = tweak
        self.on_toggle = on_toggle

        self.columnconfigure(0, weight=1)

        name_lbl = ctk.CTkLabel(self, text=tweak["name"],
                                 font=("Segoe UI Semibold", 13),
                                 text_color=TEXT_PRIMARY, anchor="w")
        name_lbl.grid(row=0, column=0, sticky="w", padx=16, pady=(12, 2))

        desc_lbl = ctk.CTkLabel(self, text=tweak["desc"],
                                 font=("Segoe UI", 11),
                                 text_color=TEXT_SECONDARY, anchor="w",
                                 wraplength=520)
        desc_lbl.grid(row=1, column=0, sticky="w", padx=16, pady=(0, 12))

        val = settings.get(tweak["id"], False)
        self.var = ctk.BooleanVar(value=bool(val))
        self.toggle = AnimatedToggle(self, variable=self.var,
                                      command=self._toggled, text="")
        self.toggle.grid(row=0, column=1, rowspan=2, padx=16, pady=8)

    def _toggled(self):
        self.on_toggle(self.tweak, self.var.get())

class LogPanel(ctk.CTkFrame):
    def __init__(self, master, **kw):
        super().__init__(master, fg_color="#0D0D0D", corner_radius=0, **kw)
        self.columnconfigure(0, weight=1)
        self.rowconfigure(1, weight=1)

        hdr = ctk.CTkFrame(self, fg_color="transparent")
        hdr.grid(row=0, column=0, sticky="ew", padx=12, pady=(8, 0))
        ctk.CTkLabel(hdr, text="Лог", font=("Segoe UI", 11),
                     text_color=TEXT_SECONDARY).pack(side="left")
        ctk.CTkButton(hdr, text="Очистить", width=70, height=24,
                      fg_color="#2A2A2A", hover_color="#333333",
                      text_color=TEXT_SECONDARY, font=("Segoe UI", 11),
                      command=self.clear, corner_radius=6).pack(side="right")

        self.box = ctk.CTkTextbox(self, fg_color="transparent",
                                   text_color="#AAAAAA",
                                   font=("Consolas", 11),
                                   activate_scrollbars=True,
                                   border_width=0)
        self.box.grid(row=1, column=0, sticky="nsew", padx=12, pady=(4, 8))
        self.box.configure(state="disabled")

    def log(self, msg: str, success: bool | None = None):
        ts = datetime.now().strftime("%H:%M:%S")
        icon = "✓" if success is True else ("✗" if success is False else "►")
        col = GREEN if success is True else (RED if success is False else ORANGE)
        self.box.configure(state="normal")
        self.box.insert("end", f"[{ts}] {icon} {msg}\n")
        self.box.see("end")
        self.box.configure(state="disabled")

    def clear(self):
        self.box.configure(state="normal")
        self.box.delete("1.0", "end")
        self.box.configure(state="disabled")

# ── главное окно ──────────────────────────────────────────────────────────────
class WinTweakerApp(ctk.CTk):
    def __init__(self):
        super().__init__()
        self.title("WinTweaker — by koniaoo")
        self.geometry("1060x680")
        self.minsize(860, 560)
        self.configure(fg_color=BG_DARK)

        self._set_icon()

        self.settings: dict = load_settings()
        self.current_section = "performance"
        self.cards: dict[str, list[TweakCard]] = {}

        self._build_ui()
        self._show_section("performance")
        self.admin_ok = is_admin()
        self._update_admin_badge()

    # ── иконка окна ──────────────────────────────────────────────────────────
    def _set_icon(self):
        try:
            from PIL import Image, ImageDraw
            import io, base64, tempfile

            img = Image.new("RGBA", (64, 64), (0, 0, 0, 0))
            d = ImageDraw.Draw(img)
            # фон — тёмный круг
            d.ellipse([2, 2, 62, 62], fill=(20, 20, 20, 255))
            # оранжевая молния
            pts = [(32,6),(20,34),(30,34),(22,58),(46,28),(34,28),(42,6)]
            d.polygon(pts, fill=(255, 107, 0, 255))

            tmp = tempfile.NamedTemporaryFile(suffix=".ico", delete=False)
            img.save(tmp.name, format="ICO", sizes=[(64, 64)])
            tmp.close()
            self.iconbitmap(tmp.name)
        except Exception:
            pass

    # ── UI ────────────────────────────────────────────────────────────────────
    def _build_ui(self):
        self.grid_columnconfigure(1, weight=1)
        self.grid_rowconfigure(0, weight=1)

        # сайдбар
        sidebar = ctk.CTkFrame(self, fg_color=BG_SIDEBAR, width=220,
                                corner_radius=0)
        sidebar.grid(row=0, column=0, sticky="nsew")
        sidebar.grid_propagate(False)
        sidebar.grid_rowconfigure(10, weight=1)

        # логотип
        logo_frame = ctk.CTkFrame(sidebar, fg_color="transparent")
        logo_frame.grid(row=0, column=0, padx=20, pady=(24, 4), sticky="w")
        w = ctk.CTkLabel(logo_frame, text="Win", font=("Segoe UI Black", 22),
                          text_color=TEXT_PRIMARY)
        w.pack(side="left")
        t = ctk.CTkLabel(logo_frame, text="Tweaker", font=("Segoe UI Black", 22),
                          text_color=ORANGE)
        t.pack(side="left")
        ctk.CTkLabel(sidebar, text="by koniaoo", font=("Segoe UI", 11),
                     text_color=TEXT_MUTED).grid(row=1, column=0,
                     padx=20, pady=(0, 24), sticky="w")

        # кнопки разделов
        self.nav_btns: dict[str, ctk.CTkButton] = {}
        for i, (sec_id, label) in enumerate(SECTION_LABELS.items()):
            icon = SECTION_ICONS[sec_id]
            btn = ctk.CTkButton(
                sidebar,
                text=f"  {icon}  {label}",
                anchor="w",
                font=("Segoe UI", 13),
                fg_color="transparent",
                hover_color="#2A2A2A",
                text_color=TEXT_SECONDARY,
                height=42,
                corner_radius=8,
                command=lambda s=sec_id: self._show_section(s),
            )
            btn.grid(row=i + 2, column=0, padx=10, pady=2, sticky="ew")
            self.nav_btns[sec_id] = btn

        # кнопка «Применить всё»
        apply_frame = ctk.CTkFrame(sidebar, fg_color="transparent")
        apply_frame.grid(row=11, column=0, padx=10, pady=16, sticky="sew")
        ctk.CTkButton(
            apply_frame,
            text="  ⚡  Применить всё",
            font=("Segoe UI Bold", 13),
            fg_color=ORANGE,
            hover_color=ORANGE_HOVER,
            text_color="#FFFFFF",
            height=48,
            corner_radius=10,
            command=self._apply_all,
        ).pack(fill="x")
        self.admin_badge = ctk.CTkLabel(
            apply_frame,
            text="",
            font=("Segoe UI", 10),
            text_color=TEXT_MUTED,
        )
        self.admin_badge.pack(pady=(4, 0))

        # правая часть
        right = ctk.CTkFrame(self, fg_color=BG_PANEL, corner_radius=0)
        right.grid(row=0, column=1, sticky="nsew")
        right.grid_rowconfigure(1, weight=1)
        right.grid_columnconfigure(0, weight=1)

        # заголовок
        self.header_frame = ctk.CTkFrame(right, fg_color="transparent")
        self.header_frame.grid(row=0, column=0, sticky="ew", padx=0, pady=0)
        self.header_frame.grid_columnconfigure(0, weight=1)

        hdr_inner = ctk.CTkFrame(self.header_frame, fg_color="transparent")
        hdr_inner.grid(row=0, column=0, sticky="ew", padx=24, pady=16)
        hdr_inner.grid_columnconfigure(0, weight=1)

        self.section_title = ctk.CTkLabel(
            hdr_inner, text="", font=("Segoe UI Black", 18),
            text_color=TEXT_PRIMARY, anchor="w",
        )
        self.section_title.grid(row=0, column=0, sticky="w")

        self.admin_dot = ctk.CTkLabel(
            hdr_inner, text="● Администратор",
            font=("Segoe UI", 11), text_color=GREEN,
        )
        self.admin_dot.grid(row=0, column=1, sticky="e")

        # кнопка «применить раздел»
        self.apply_section_btn = ctk.CTkButton(
            self.header_frame,
            text="  ⚡  Применить все твики раздела",
            font=("Segoe UI Bold", 13),
            fg_color=ORANGE, hover_color=ORANGE_HOVER,
            text_color="#FFFFFF",
            height=38, corner_radius=0,
            command=self._apply_section,
        )
        self.apply_section_btn.grid(row=1, column=0, sticky="ew", padx=0)

        # прокрутка с карточками
        self.scroll = ctk.CTkScrollableFrame(
            right, fg_color="transparent", scrollbar_button_color="#2A2A2A",
            scrollbar_button_hover_color=ORANGE,
        )
        self.scroll.grid(row=1, column=0, sticky="nsew", padx=16, pady=8)
        self.scroll.grid_columnconfigure(0, weight=1)

        # лог
        self.log_panel = LogPanel(right, height=150)
        self.log_panel.grid(row=2, column=0, sticky="ew")

    # ── навигация ─────────────────────────────────────────────────────────────
    def _show_section(self, section: str):
        self.current_section = section

        # подсветка кнопки
        for sid, btn in self.nav_btns.items():
            if sid == section:
                btn.configure(fg_color="#2A1A00", text_color=ORANGE)
            else:
                btn.configure(fg_color="transparent", text_color=TEXT_SECONDARY)

        # заголовок
        icon = SECTION_ICONS[section]
        self.section_title.configure(
            text=f"{icon}  {SECTION_LABELS[section]}")

        # очищаем скролл
        for w in self.scroll.winfo_children():
            w.destroy()

        # строим карточки
        if section not in self.cards:
            self.cards[section] = []
        self.cards[section].clear()

        for tweak in TWEAKS[section]:
            card = TweakCard(
                self.scroll, tweak, self.settings,
                on_toggle=self._on_toggle,
            )
            card.grid(sticky="ew", pady=4)
            self.scroll.grid_columnconfigure(0, weight=1)
            self.cards[section].append(card)

    # ── переключение одного твика ─────────────────────────────────────────────
    def _on_toggle(self, tweak: dict, state: bool):
        self.settings[tweak["id"]] = state
        save_settings(self.settings)
        cmd = tweak["enable"] if state else tweak["disable"]
        action = "Включить" if state else "Выключить"
        self.log_panel.log(f"{action}: {tweak['name']}...")
        threading.Thread(target=self._run_tweak, args=(tweak["name"], cmd),
                         daemon=True).start()

    def _run_tweak(self, name: str, cmd: str):
        ok, out = run_cmd(cmd)
        self.after(0, lambda: self.log_panel.log(f"Готово: {out[:80]}", ok))

    # ── применить раздел ──────────────────────────────────────────────────────
    def _apply_section(self):
        tweaks = TWEAKS[self.current_section]
        threading.Thread(target=self._bulk_run, args=(tweaks,),
                         daemon=True).start()

    # ── применить всё ─────────────────────────────────────────────────────────
    def _apply_all(self):
        all_tweaks = [t for section in TWEAKS.values() for t in section]
        threading.Thread(target=self._bulk_run, args=(all_tweaks,),
                         daemon=True).start()

    def _bulk_run(self, tweaks: list):
        for tweak in tweaks:
            state = self.settings.get(tweak["id"], False)
            cmd = tweak["enable"] if state else tweak["disable"]
            self.after(0, lambda n=tweak["name"]: self.log_panel.log(f"► {n}..."))
            ok, out = run_cmd(cmd)
            self.after(0, lambda o=out, s=ok: self.log_panel.log(f"  {o[:80]}", s))

    # ── значок прав ──────────────────────────────────────────────────────────
    def _update_admin_badge(self):
        if self.admin_ok:
            self.admin_dot.configure(text="● Администратор", text_color=GREEN)
            self.admin_badge.configure(text="")
        else:
            self.admin_dot.configure(text="● Нет прав администратора",
                                     text_color=RED)
            self.admin_badge.configure(
                text="Требуются права\nадминистратора",
                text_color=RED,
            )
            self.log_panel.log(
                "⚠ Запустите от имени администратора для применения твиков!",
                False,
            )


if __name__ == "__main__":
    app = WinTweakerApp()
    app.mainloop()
