using WinTweaker.Models;
using WinTweaker.Services;

namespace WinTweaker.Data;

/// <summary>
/// All tweaks, ported 1:1 from the Python version. Commands are cmd.exe strings.
/// EN/RU names &amp; descriptions share the same commands.
/// </summary>
public static class TweakData
{
    public static readonly string[] Order =
        { "performance", "telemetry", "ads", "cleanup", "network", "privacy" };

    /// <summary>Builds the section -> tweaks map for a language, applying saved states.</summary>
    public static Dictionary<string, List<Tweak>> Build(string lang, Settings settings)
    {
        bool ru = lang != "en";
        var data = new Dictionary<string, List<Tweak>>();

        Tweak T(string id, string ruN, string ruD, string enN, string enD, string en, string dis) => new()
        {
            Id = id,
            Name = ru ? ruN : enN,
            Desc = ru ? ruD : enD,
            Enable = en,
            Disable = dis,
            IsEnabled = settings.GetTweak(id),
        };

        data["performance"] = new()
        {
            T("max_power", "Максимальная производительность питания", "Переключает схему питания на «Максимальная производительность»",
              "Max Performance Power Plan", "Switches to Maximum Performance power plan",
              @"powercfg /setactive 8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c",
              @"powercfg /setactive 381b4222-f694-41f0-9685-ff5bb260df2e"),
            T("superfetch", "Отключить SysMain (Superfetch)", "Освобождает ОЗУ — полезно если меньше 8 ГБ",
              "Disable SysMain", "Frees RAM — useful with 8GB or less",
              @"sc stop SysMain & sc config SysMain start=disabled",
              @"sc config SysMain start=auto & sc start SysMain"),
            T("search_idx", "Отключить индексирование поиска", "Снижает нагрузку на диск",
              "Disable Search Indexing", "Reduces disk load",
              @"sc stop WSearch & sc config WSearch start=disabled",
              @"sc config WSearch start=delayed-auto & sc start WSearch"),
            T("visual_fx", "Отключить визуальные эффекты", "Убирает анимации и тени — интерфейс отзывчивее",
              "Disable Visual Effects", "Removes animations and shadows",
              @"reg add ""HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\VisualEffects"" /v VisualFXSetting /t REG_DWORD /d 2 /f",
              @"reg add ""HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\VisualEffects"" /v VisualFXSetting /t REG_DWORD /d 0 /f"),
            T("game_mode", "Включить Game Mode", "Оптимизирует ресурсы CPU/GPU под игры",
              "Enable Game Mode", "Prioritizes CPU/GPU for games",
              @"reg add ""HKCU\Software\Microsoft\GameBar"" /v AllowAutoGameMode /t REG_DWORD /d 1 /f",
              @"reg add ""HKCU\Software\Microsoft\GameBar"" /v AutoGameModeEnabled /t REG_DWORD /d 0 /f"),
            T("fast_startup", "Быстрый запуск Windows", "Ускоряет загрузку системы через гибернацию ядра",
              "Fast Startup", "Speeds up boot via kernel hibernation",
              @"reg add ""HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Power"" /v HiberbootEnabled /t REG_DWORD /d 1 /f",
              @"reg add ""HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Power"" /v HiberbootEnabled /t REG_DWORD /d 0 /f"),
            T("timer_res", "Высокое разрешение таймера", "Уменьшает задержки — полезно для игр и аудио",
              "High Timer Resolution", "Reduces latency for gaming/audio",
              @"reg add ""HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile"" /v SystemResponsiveness /t REG_DWORD /d 0 /f",
              @"reg add ""HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile"" /v SystemResponsiveness /t REG_DWORD /d 20 /f"),
            T("hags", "Аппаратное ускорение GPU (HAGS)", "Снижает задержку GPU в DirectX 12 (Win10 2004+)",
              "GPU Hardware Scheduling", "Reduces GPU latency in DX12 (Win10 2004+)",
              @"reg add ""HKLM\SYSTEM\CurrentControlSet\Control\GraphicsDrivers"" /v HwSchMode /t REG_DWORD /d 2 /f",
              @"reg add ""HKLM\SYSTEM\CurrentControlSet\Control\GraphicsDrivers"" /v HwSchMode /t REG_DWORD /d 1 /f"),
            T("cpu_prio", "Приоритет CPU для активных программ", "ОС отдаёт больше ресурсов активному окну",
              "CPU Priority for Active Apps", "OS gives more CPU to foreground window",
              @"reg add ""HKLM\SYSTEM\CurrentControlSet\Control\PriorityControl"" /v Win32PrioritySeparation /t REG_DWORD /d 38 /f",
              @"reg add ""HKLM\SYSTEM\CurrentControlSet\Control\PriorityControl"" /v Win32PrioritySeparation /t REG_DWORD /d 2 /f"),
            T("meltdown", "Отключить Meltdown/Spectre патч ⚠", "Снижает безопасность, но даёт +10% CPU — только домашние ПК",
              "Disable Meltdown/Spectre Patch ⚠", "Reduces security but gains ~10% CPU perf",
              @"reg add ""HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management"" /v FeatureSettingsOverride /t REG_DWORD /d 3 /f & reg add ""HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management"" /v FeatureSettingsOverrideMask /t REG_DWORD /d 3 /f",
              @"reg delete ""HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management"" /v FeatureSettingsOverride /f"),
            T("hibernate", "Отключить гибернацию", "Удаляет hiberfil.sys — освобождает место",
              "Disable Hibernation", "Removes hiberfil.sys — frees disk space",
              @"powercfg /hibernate off", @"powercfg /hibernate on"),
            T("transparency", "Отключить эффекты прозрачности", "Убирает полупрозрачность окон и поверхностей — чище и быстрее",
              "Disable Transparency Effects", "Removes window transparency — cleaner & faster",
              @"reg add ""HKCU\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize"" /v EnableTransparency /t REG_DWORD /d 0 /f",
              @"reg add ""HKCU\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize"" /v EnableTransparency /t REG_DWORD /d 1 /f"),
            T("dark_mode_p", "Включить тёмную тему системы", "Тёмный режим для всех окон и системных элементов Windows",
              "Enable System Dark Theme", "Dark mode for all windows and system elements",
              @"reg add ""HKCU\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize"" /v AppsUseLightTheme /t REG_DWORD /d 0 /f & reg add ""HKCU\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize"" /v SystemUsesLightTheme /t REG_DWORD /d 0 /f",
              @"reg add ""HKCU\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize"" /v AppsUseLightTheme /t REG_DWORD /d 1 /f & reg add ""HKCU\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize"" /v SystemUsesLightTheme /t REG_DWORD /d 1 /f"),
            T("hide_search", "Скрыть поиск на панели задач", "Убирает поле/значок поиска с панели задач — чище интерфейс",
              "Hide Taskbar Search", "Removes the search box/icon from the taskbar",
              @"reg add ""HKCU\Software\Microsoft\Windows\CurrentVersion\Search"" /v SearchboxTaskbarMode /t REG_DWORD /d 0 /f",
              @"reg add ""HKCU\Software\Microsoft\Windows\CurrentVersion\Search"" /v SearchboxTaskbarMode /t REG_DWORD /d 2 /f"),
            T("hide_widgets", "Скрыть виджеты на панели задач", "Убирает кнопку виджетов (погода/новости) с панели задач",
              "Hide Taskbar Widgets", "Removes the Widgets (weather/news) button",
              @"reg add ""HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced"" /v TaskbarDa /t REG_DWORD /d 0 /f",
              @"reg add ""HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced"" /v TaskbarDa /t REG_DWORD /d 1 /f"),
        };

        data["telemetry"] = new()
        {
            T("tel_main", "Отключить телеметрию Windows", "Блокирует отправку данных в Microsoft",
              "Disable Windows Telemetry", "Blocks usage data from Microsoft",
              @"reg add ""HKLM\SOFTWARE\Policies\Microsoft\Windows\DataCollection"" /v AllowTelemetry /t REG_DWORD /d 0 /f & sc stop DiagTrack & sc config DiagTrack start=disabled",
              @"reg add ""HKLM\SOFTWARE\Policies\Microsoft\Windows\DataCollection"" /v AllowTelemetry /t REG_DWORD /d 1 /f & sc config DiagTrack start=auto"),
            T("activity", "Отключить историю активности", "Microsoft не хранит историю ваших действий",
              "Disable Activity History", "Won't store activity history",
              @"reg add ""HKLM\SOFTWARE\Policies\Microsoft\Windows\System"" /v EnableActivityFeed /t REG_DWORD /d 0 /f",
              @"reg add ""HKLM\SOFTWARE\Policies\Microsoft\Windows\System"" /v EnableActivityFeed /t REG_DWORD /d 1 /f"),
            T("diagtrack", "Отключить DiagTrack (Connected UE)", "Останавливает главную службу слежки",
              "Disable DiagTrack", "Stops main tracking service",
              @"sc stop DiagTrack & sc config DiagTrack start=disabled",
              @"sc config DiagTrack start=auto & sc start DiagTrack"),
            T("compat_tel", "Отключить CompatTelRunner", "Останавливает задачи сбора данных совместимости",
              "Disable CompatTelRunner", "Stops compat data collection",
              @"schtasks /Change /TN ""Microsoft\Windows\Application Experience\Microsoft Compatibility Appraiser"" /Disable",
              @"schtasks /Change /TN ""Microsoft\Windows\Application Experience\Microsoft Compatibility Appraiser"" /Enable"),
            T("location", "Отключить геолокацию", "Запрещает приложениям доступ к местоположению",
              "Disable Location", "No app location access",
              @"reg add ""HKLM\SOFTWARE\Policies\Microsoft\Windows\LocationAndSensors"" /v DisableLocation /t REG_DWORD /d 1 /f",
              @"reg add ""HKLM\SOFTWARE\Policies\Microsoft\Windows\LocationAndSensors"" /v DisableLocation /t REG_DWORD /d 0 /f"),
            T("feedback", "Отключить запросы обратной связи", "Убирает опросы Microsoft",
              "Disable Feedback Requests", "Removes Microsoft survey popups",
              @"reg add ""HKCU\Software\Microsoft\Siuf\Rules"" /v NumberOfSIUFInPeriod /t REG_DWORD /d 0 /f",
              @"reg delete ""HKCU\Software\Microsoft\Siuf\Rules"" /v NumberOfSIUFInPeriod /f"),
            T("adv_id", "Отключить рекламный ID", "Запрещает использовать рекламный идентификатор",
              "Disable Advertising ID", "No advertising identifier",
              @"reg add ""HKCU\Software\Microsoft\Windows\CurrentVersion\AdvertisingInfo"" /v Enabled /t REG_DWORD /d 0 /f",
              @"reg add ""HKCU\Software\Microsoft\Windows\CurrentVersion\AdvertisingInfo"" /v Enabled /t REG_DWORD /d 1 /f"),
            T("cortana", "Отключить Cortana", "Деактивирует голосового ассистента",
              "Disable Cortana", "Disables Cortana voice assistant",
              @"reg add ""HKLM\SOFTWARE\Policies\Microsoft\Windows\Windows Search"" /v AllowCortana /t REG_DWORD /d 0 /f",
              @"reg add ""HKLM\SOFTWARE\Policies\Microsoft\Windows\Windows Search"" /v AllowCortana /t REG_DWORD /d 1 /f"),
            T("wer", "Отключить отчёты об ошибках", "Прекращает отправку crash-отчётов в Microsoft",
              "Disable Error Reporting", "Stops crash reports to Microsoft",
              @"sc stop WerSvc & sc config WerSvc start=disabled & reg add ""HKLM\SOFTWARE\Microsoft\Windows\Windows Error Reporting"" /v Disabled /t REG_DWORD /d 1 /f",
              @"sc config WerSvc start=demand"),
        };

        data["ads"] = new()
        {
            T("start_ads", "Убрать рекламу в меню Пуск", "Отключает рекламные плитки и предложения",
              "Remove Start Menu Ads", "Disables ad tiles",
              @"reg add ""HKCU\Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager"" /v SystemPaneSuggestionsEnabled /t REG_DWORD /d 0 /f",
              @"reg add ""HKCU\Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager"" /v SystemPaneSuggestionsEnabled /t REG_DWORD /d 1 /f"),
            T("lock_ads", "Убрать рекламу на экране блокировки", "Отключает Windows Spotlight и советы",
              "Remove Lock Screen Ads", "Disables Windows Spotlight",
              @"reg add ""HKCU\Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager"" /v RotatingLockScreenEnabled /t REG_DWORD /d 0 /f",
              @"reg add ""HKCU\Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager"" /v RotatingLockScreenEnabled /t REG_DWORD /d 1 /f"),
            T("tips", "Отключить советы Windows", "Убирает всплывающие подсказки",
              "Disable Tips", "Removes popups",
              @"reg add ""HKCU\Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager"" /v SoftLandingEnabled /t REG_DWORD /d 0 /f",
              @"reg add ""HKCU\Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager"" /v SoftLandingEnabled /t REG_DWORD /d 1 /f"),
            T("auto_inst", "Отключить автоустановку приложений", "Запрещает тихую установку bloatware",
              "Disable Silent App Install", "Prevents silent bloatware",
              @"reg add ""HKCU\Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager"" /v SilentInstalledAppsEnabled /t REG_DWORD /d 0 /f",
              @"reg add ""HKCU\Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager"" /v SilentInstalledAppsEnabled /t REG_DWORD /d 1 /f"),
            T("bing", "Убрать Bing из поиска", "Поиск в Пуске не лезет в интернет",
              "Remove Bing from Search", "Start search won't use internet",
              @"reg add ""HKCU\Software\Microsoft\Windows\CurrentVersion\Search"" /v BingSearchEnabled /t REG_DWORD /d 0 /f",
              @"reg add ""HKCU\Software\Microsoft\Windows\CurrentVersion\Search"" /v BingSearchEnabled /t REG_DWORD /d 1 /f"),
            T("edge_ads", "Отключить рекламу в Edge", "Убирает рекламные плитки на новой вкладке Edge",
              "Disable Edge Ads", "Removes ad tiles in Edge",
              @"reg add ""HKLM\SOFTWARE\Policies\Microsoft\Edge"" /v NewTabPageContentEnabled /t REG_DWORD /d 0 /f",
              @"reg delete ""HKLM\SOFTWARE\Policies\Microsoft\Edge"" /v NewTabPageContentEnabled /f"),
        };

        data["cleanup"] = new()
        {
            T("temp", "Очистить временные файлы", "Удаляет файлы из %TEMP% и Windows\\Temp",
              "Clear Temp Files", "Deletes from %TEMP% and Windows Temp",
              @"cmd /c ""del /q /f /s %TEMP%\* 2>nul & del /q /f /s C:\Windows\Temp\* 2>nul""", @"echo N/A"),
            T("prefetch", "Очистить Prefetch", "Удаляет кэш предзагрузки",
              "Clear Prefetch", "Deletes app preload cache",
              @"del /q /f /s C:\Windows\Prefetch\* 2>nul", @"echo N/A"),
            T("dns_flush", "Сбросить DNS-кэш", "Очищает кэш DNS",
              "Flush DNS Cache", "Clears DNS cache",
              @"ipconfig /flushdns", @"echo N/A"),
            T("recycle", "Очистить корзину", "Удаляет все файлы из корзины",
              "Empty Recycle Bin", "Deletes all recycle bin files",
              @"PowerShell -Command ""Clear-RecycleBin -Force -EA SilentlyContinue""", @"echo N/A"),
            T("event_logs", "Очистить журналы событий", "Стирает все системные логи",
              "Clear Event Logs", "Wipes all event logs",
              @"PowerShell -Command ""wevtutil el | ForEach-Object { wevtutil cl $_ }""", @"echo N/A"),
            T("thumb", "Очистить кэш миниатюр", "Удаляет кэш превью Проводника",
              "Clear Thumbnail Cache", "Deletes Explorer preview cache",
              @"del /f /s /q %LocalAppData%\Microsoft\Windows\Explorer\thumbcache_*.db 2>nul", @"echo N/A"),
            T("upd_cache", "Очистить кэш обновлений", "Освобождает место от загруженных обновлений",
              "Clear Update Cache", "Frees space from update packages",
              @"net stop wuauserv 2>nul & rd /s /q C:\Windows\SoftwareDistribution\Download 2>nul & net start wuauserv 2>nul", @"echo N/A"),
        };

        data["network"] = new()
        {
            T("tcp_auto", "TCP автотюнинг", "Авто-оптимизация TCP буфера — быстрее скачивание",
              "TCP Autotuning", "Auto TCP buffer optimization",
              @"netsh int tcp set global autotuninglevel=normal",
              @"netsh int tcp set global autotuninglevel=disabled"),
            T("tcp_fast", "TCP Fast Open", "Ускоряет установку соединений",
              "TCP Fast Open", "Faster connection setup",
              @"netsh int tcp set global fastopen=enabled",
              @"netsh int tcp set global fastopen=disabled"),
            T("dns_goog", "DNS Google (8.8.8.8)", "Google DNS для всех адаптеров — быстрее резолв",
              "Google DNS", "Google DNS for all adapters",
              @"PowerShell -Command ""Get-NetAdapter | Where Status -eq Up | Set-DnsClientServerAddress -ServerAddresses ('8.8.8.8','8.8.4.4')""",
              @"PowerShell -Command ""Get-NetAdapter | Where Status -eq Up | Set-DnsClientServerAddress -ResetServerAddresses"""),
            T("ipv6_off", "Отключить IPv6", "Упрощает стек сети",
              "Disable IPv6", "Simplifies network stack",
              @"reg add ""HKLM\SYSTEM\CurrentControlSet\Services\Tcpip6\Parameters"" /v DisabledComponents /t REG_DWORD /d 255 /f",
              @"reg add ""HKLM\SYSTEM\CurrentControlSet\Services\Tcpip6\Parameters"" /v DisabledComponents /t REG_DWORD /d 0 /f"),
            T("bw_limit", "Снять лимит полосы (обновления)", "Убирает резервирование 20% канала под обновления",
              "Remove BW Limit", "Remove 20% bandwidth reservation",
              @"reg add ""HKLM\SOFTWARE\Policies\Microsoft\Windows\Psched"" /v NonBestEffortLimit /t REG_DWORD /d 0 /f",
              @"reg delete ""HKLM\SOFTWARE\Policies\Microsoft\Windows\Psched"" /v NonBestEffortLimit /f"),
            T("nagle", "Отключить алгоритм Нагла", "Снижает latency в онлайн-играх",
              "Disable Nagle Algorithm", "Less latency in online games",
              @"reg add ""HKLM\SYSTEM\CurrentControlSet\Services\Tcpip\Parameters"" /v TCPNoDelay /t REG_DWORD /d 1 /f",
              @"reg delete ""HKLM\SYSTEM\CurrentControlSet\Services\Tcpip\Parameters"" /v TCPNoDelay /f"),
        };

        data["privacy"] = new()
        {
            T("priv_mic", "Отключить доступ к микрофону", "Запрет для UWP-приложений",
              "Disable Microphone Access", "Deny mic for UWP apps",
              @"reg add ""HKCU\Software\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\microphone"" /v Value /t REG_SZ /d Deny /f",
              @"reg add ""HKCU\Software\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\microphone"" /v Value /t REG_SZ /d Allow /f"),
            T("priv_cam", "Отключить доступ к камере", "Запрет для UWP-приложений",
              "Disable Camera Access", "Deny webcam for UWP apps",
              @"reg add ""HKCU\Software\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\webcam"" /v Value /t REG_SZ /d Deny /f",
              @"reg add ""HKCU\Software\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\webcam"" /v Value /t REG_SZ /d Allow /f"),
            T("priv_clip", "Отключить синхронизацию буфера обмена", "Содержимое не уходит в облако Microsoft",
              "Disable Clipboard Sync", "Clipboard won't sync to cloud",
              @"reg add ""HKCU\Software\Microsoft\Clipboard"" /v EnableClipboardHistory /t REG_DWORD /d 0 /f",
              @"reg add ""HKCU\Software\Microsoft\Clipboard"" /v EnableClipboardHistory /t REG_DWORD /d 1 /f"),
            T("priv_apps", "Отключить отслеживание запуска", "Windows не ведёт статистику запускаемых приложений",
              "Disable App Launch Tracking", "Won't track app launches",
              @"reg add ""HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced"" /v Start_TrackProgs /t REG_DWORD /d 0 /f",
              @"reg add ""HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced"" /v Start_TrackProgs /t REG_DWORD /d 1 /f"),
            T("show_ext", "Показывать расширения файлов", "Файлы покажут .exe .py .txt",
              "Show File Extensions", "Show .exe .py .txt etc",
              @"reg add ""HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced"" /v HideFileExt /t REG_DWORD /d 0 /f",
              @"reg add ""HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced"" /v HideFileExt /t REG_DWORD /d 1 /f"),
            T("show_hide", "Показывать скрытые файлы", "Скрытые и системные файлы видны в Проводнике",
              "Show Hidden Files", "Show hidden/system files",
              @"reg add ""HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced"" /v Hidden /t REG_DWORD /d 1 /f",
              @"reg add ""HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced"" /v Hidden /t REG_DWORD /d 2 /f"),
            T("dark_mode", "Тёмная тема системы", "Тёмный режим для всех элементов и приложений",
              "Enable Dark Mode", "Dark mode for all elements",
              @"reg add ""HKCU\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize"" /v AppsUseLightTheme /t REG_DWORD /d 0 /f & reg add ""HKCU\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize"" /v SystemUsesLightTheme /t REG_DWORD /d 0 /f",
              @"reg add ""HKCU\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize"" /v AppsUseLightTheme /t REG_DWORD /d 1 /f"),
        };

        return data;
    }
}
