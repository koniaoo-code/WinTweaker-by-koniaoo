namespace WinTweaker.Data;

/// <summary>App-wide constants: version and developer contacts.</summary>
public static class AppInfo
{
    public const string Version = "1.5";
    public const string Developer = "koniaoo";
    public const string GitHub = "https://github.com/koniaoo-code/WinTweaker-by-koniaoo";
    public const string GitHubProfile = "https://github.com/koniaoo-code";
    public const string Website = "https://koniaoo-code.netlify.app";
    public const string Discord = "kon1xx_04470";
}

/// <summary>
/// UI strings for the two supported languages. Dictionary-based so new
/// keys can be added without touching call sites: use s["key"].
/// </summary>
public sealed class Strings
{
    private readonly Dictionary<string, string> _m;
    public Dictionary<string, string> Sections { get; }

    public string this[string key] => _m.TryGetValue(key, out var v) ? v : key;

    private Strings(Dictionary<string, string> m, Dictionary<string, string> sections)
    {
        _m = m;
        Sections = sections;
    }

    // Segoe Fluent Icons / Segoe MDL2 Assets glyphs (native, monochrome, modern)
    public static readonly Dictionary<string, string> Icons = new()
    {
        ["performance"] = "", ["telemetry"] = "", ["ads"] = "",
        ["cleanup"] = "", ["network"] = "", ["privacy"] = "",
        ["uwp"] = "", ["pcinfo"] = "", ["about"] = "",
        ["dashboard"] = "\uE80F", ["benchmark"] = "\uE916", ["startup"] = "\uE7E8", ["services"] = "\uE71D", ["apps"] = "\uE738",
    };

    /// <summary>Full sidebar order (tweak sections + special pages).</summary>
    public static readonly string[] NavOrder =
        { "dashboard", "performance", "telemetry", "ads", "cleanup", "network", "privacy", "startup", "services", "apps", "uwp", "pcinfo", "benchmark", "about" };

    public static readonly HashSet<string> SpecialSections = new() { "dashboard", "benchmark", "startup", "services", "apps", "uwp", "pcinfo", "about" };

    public static Strings Get(string lang) => lang == "en" ? En : Ru;

    private static readonly Strings Ru = new(new()
    {
        ["by"] = "by koniaoo",
        ["apply_sec"] = "⚡  Применить все твики раздела",
        ["apply_all"] = "⚡  Применить всё",
        ["admin_ok"] = "● Администратор",
        ["admin_no"] = "● Нет прав администратора",
        ["admin_req"] = "Требуются права администратора",
        ["no_admin"] = "⚠ Запустите от имени администратора!",
        ["log_lbl"] = "Лог",
        ["log_clear"] = "Очистить",
        ["log_show"] = "📋  Показать лог",
        ["enable"] = "Вкл",
        ["disable"] = "Выкл",
        ["applied_sec"] = "Применены все твики раздела ✓",
        ["applied_all"] = "Применены все твики ✓",
        ["reboot_title"] = "Перезагрузка рекомендуется",
        ["reboot_msg"] = "Часть твиков вступит в силу только после перезагрузки.\nПерезагрузить компьютер сейчас?",
        ["reboot_btn"] = "🔄  Требуется перезагрузка",
        ["egg_logo"] = "🥚 Пасхалка! Сделано с ❤ koniaoo. Спасибо, что пользуешься WinTweaker!",
        ["egg_about"] = "🐱 Мяу! Ты нашёл секретный спин ⚡",
        ["search_hint"] = "🔍  Поиск по всем твикам...",
        ["search_results"] = "🔍  Результаты поиска",
        ["presets"] = "Пресеты",
        ["preset_gaming"] = "🎮  Для игр",
        ["preset_privacy"] = "🔒  Приватность",
        ["preset_minimal"] = "⚡  Минимальный",
        ["preset_applied"] = "Пресет применён ✓",
        ["restore_point"] = "🛡  Точка восстановления",
        ["restore_creating"] = "Создание точки восстановления...",
        ["restore_done"] = "Точка восстановления создана ✓",
        ["restore_fail"] = "Не удалось создать точку (нужны права админа)",
        ["revert_all"] = "↩  Откатить все твики",
        ["reverted"] = "Все твики откачены ✓",
        ["update_available"] = "🔔 Доступна новая версия: ",
        ["update_latest"] = "✓ Установлена последняя версия",
        ["checking_updates"] = "Проверка обновлений...",
        ["check_updates"] = "🔄 Проверить",
        ["update_offline"] = "Не удалось проверить (нет сети или релизов)",
        ["update_download"] = "Скачать ↗",
        ["install_app"] = "📥  Установить (ярлыки + меню Пуск)",
        ["confirm_title"] = "Подтверждение",
        ["confirm_danger"] = "Это рискованное или необратимое действие. Продолжить?",
        ["rp_before_all"] = "Создать точку восстановления перед применением всех твиков?",
        ["export_profile"] = "Экспорт профиля",
        ["import_profile"] = "Импорт профиля",
        ["profile_exported"] = "Профиль экспортирован ✓",
        ["profile_imported"] = "Профиль импортирован ✓",
        ["refresh"] = "🔄  Обновить",
        ["loading"] = "Загрузка...",
        ["startup_scan"] = "🔍  Показать программы автозагрузки",
        ["startup_none"] = "Программы автозагрузки не найдены",
        ["svc_scan"] = "🔍  Показать службы",
        ["apps_scan"] = "🔍  Показать установленные программы",
        ["apps_none"] = "Программы не найдены",
        ["apps_uninstall"] = "Удалить",
        ["found"] = "Найдено: ",
        ["dash_health"] = "Здоровье системы",
        ["dash_ram"] = "ОЗУ",
        ["dash_disk"] = "Свободно на C:",
        ["dash_proc"] = "Процессов",
        ["dash_startup"] = "В автозагрузке",
        ["dash_applied"] = "Включено твиков",
        ["dash_uptime"] = "Аптайм",
        ["dash_quick"] = "Быстрые действия",
        ["qa_minimal"] = "⚡  Применить «Минимальный»",
        ["qa_restore"] = "🛡  Точка восстановления",
        ["qa_clean"] = "🧹  Очистить временные файлы",
        ["welcome_title"] = "Добро пожаловать в WinTweaker!",
        ["welcome_msg"] = "Совет: перед применением твиков создай точку восстановления — кнопка «Точка восстановления» в боковой панели.\n\nПриятного пользования!",
        ["theme_toggle"] = "🌓  Сменить тему",
        ["pc_bench"] = "Бенчмарк (сейчас)",
        ["bench_desc"] = "Тест производительности процессора: одноядерный и многопоточный.",
        ["bench_run"] = "▶  Запустить тест",
        ["bench_running"] = "Идёт тест... подождите",
        ["bench_single"] = "Одноядерный",
        ["bench_multi"] = "Многопоточный",
        ["bench_score"] = "баллов",
        ["uwp_warn"] = "Удаление необратимо. Многие приложения можно вернуть через Microsoft Store.",
        ["uwp_scan"] = "🔍  Сканировать установленные UWP приложения",
        ["uwp_scanning"] = "Сканирование...",
        ["uwp_remove"] = "Удалить",
        ["uwp_removed"] = "Удалено ✓",
        ["uwp_err"] = "Ошибка",
        ["uwp_none"] = "Приложений для удаления не найдено",
        ["uwp_found"] = "Найдено приложений: ",
        ["about_dev"] = "Разработчик",
        ["about_ver"] = "Версия",
        ["about_gh"] = "GitHub проект",
        ["about_dc"] = "Discord",
        ["about_site"] = "Сайт",
        ["open_gh"] = "Открыть GitHub ↗",
        ["open_site"] = "Открыть сайт ↗",
        ["copy_dc"] = "Скопировать Discord",
        ["copied"] = "Скопировано!",
        ["about_desc"] = "Быстрая и безопасная оптимизация Windows.\nВсе твики обратимы переключателями.",
        ["pc_system"] = "Система", ["pc_cpu"] = "Процессор", ["pc_mem"] = "Память",
        ["pc_gpu"] = "Графика", ["pc_disk"] = "Диск C:", ["pc_net"] = "Сеть",
        ["pc_mobo"] = "Материнская плата",
        ["l_os"] = "ОС", ["l_host"] = "Имя ПК", ["l_uptime"] = "Аптайм",
        ["l_cpu"] = "CPU", ["l_cores"] = "Ядра", ["l_threads"] = "Потоки", ["l_mhz"] = "Частота (макс)",
        ["l_ram"] = "ОЗУ всего", ["l_ramfree"] = "ОЗУ свободно", ["l_gpu"] = "GPU", ["l_vram"] = "VRAM",
        ["l_disk"] = "Диск", ["l_ip"] = "IP", ["l_mfr"] = "Производитель", ["l_board"] = "Плата", ["l_bios"] = "BIOS",
    },
    new()
    {
        ["performance"] = "Производительность", ["telemetry"] = "Телеметрия", ["ads"] = "Реклама",
        ["cleanup"] = "Очистка", ["network"] = "Сеть", ["privacy"] = "Приватность",
        ["dashboard"] = "Дашборд", ["benchmark"] = "Бенчмарк", ["startup"] = "Автозагрузка", ["services"] = "Службы", ["apps"] = "Удаление программ",
        ["uwp"] = "Удалить UWP приложения", ["pcinfo"] = "Инфо о ПК", ["about"] = "О программе",
    });

    private static readonly Strings En = new(new()
    {
        ["by"] = "by koniaoo",
        ["apply_sec"] = "⚡  Apply all tweaks in section",
        ["apply_all"] = "⚡  Apply All",
        ["admin_ok"] = "● Administrator",
        ["admin_no"] = "● No admin rights",
        ["admin_req"] = "Administrator rights required",
        ["no_admin"] = "⚠ Run as Administrator!",
        ["log_lbl"] = "Log",
        ["log_clear"] = "Clear",
        ["log_show"] = "📋  Show log",
        ["enable"] = "On",
        ["disable"] = "Off",
        ["applied_sec"] = "All section tweaks applied ✓",
        ["applied_all"] = "All tweaks applied ✓",
        ["reboot_title"] = "Restart recommended",
        ["reboot_msg"] = "Some tweaks take effect only after a restart.\nRestart your computer now?",
        ["reboot_btn"] = "🔄  Restart required",
        ["egg_logo"] = "🥚 Easter egg! Made with ❤ by koniaoo. Thanks for using WinTweaker!",
        ["egg_about"] = "🐱 Meow! You found the secret spin ⚡",
        ["search_hint"] = "🔍  Search all tweaks...",
        ["search_results"] = "🔍  Search results",
        ["presets"] = "Presets",
        ["preset_gaming"] = "🎮  Gaming",
        ["preset_privacy"] = "🔒  Privacy",
        ["preset_minimal"] = "⚡  Minimal",
        ["preset_applied"] = "Preset applied ✓",
        ["restore_point"] = "🛡  Create restore point",
        ["restore_creating"] = "Creating restore point...",
        ["restore_done"] = "Restore point created ✓",
        ["restore_fail"] = "Could not create restore point (needs admin)",
        ["revert_all"] = "↩  Revert all tweaks",
        ["reverted"] = "All tweaks reverted ✓",
        ["update_available"] = "🔔 New version available: ",
        ["update_latest"] = "✓ You have the latest version",
        ["checking_updates"] = "Checking for updates...",
        ["check_updates"] = "🔄 Check",
        ["update_offline"] = "Couldn't check (no network or releases)",
        ["update_download"] = "Download ↗",
        ["install_app"] = "📥  Install (shortcuts + Start Menu)",
        ["confirm_title"] = "Confirm",
        ["confirm_danger"] = "This is a risky or irreversible action. Continue?",
        ["rp_before_all"] = "Create a restore point before applying all tweaks?",
        ["export_profile"] = "Export profile",
        ["import_profile"] = "Import profile",
        ["profile_exported"] = "Profile exported ✓",
        ["profile_imported"] = "Profile imported ✓",
        ["refresh"] = "🔄  Refresh",
        ["loading"] = "Loading...",
        ["startup_scan"] = "🔍  Scan startup programs",
        ["startup_none"] = "No startup programs found",
        ["svc_scan"] = "🔍  Scan services",
        ["apps_scan"] = "🔍  Scan installed programs",
        ["apps_none"] = "No programs found",
        ["apps_uninstall"] = "Uninstall",
        ["found"] = "Found: ",
        ["dash_health"] = "System health",
        ["dash_ram"] = "RAM",
        ["dash_disk"] = "Free on C:",
        ["dash_proc"] = "Processes",
        ["dash_startup"] = "Startup items",
        ["dash_applied"] = "Tweaks enabled",
        ["dash_uptime"] = "Uptime",
        ["dash_quick"] = "Quick actions",
        ["qa_minimal"] = "⚡  Apply Minimal preset",
        ["qa_restore"] = "🛡  Create restore point",
        ["qa_clean"] = "🧹  Clear temp files",
        ["welcome_title"] = "Welcome to WinTweaker!",
        ["welcome_msg"] = "Tip: create a restore point before applying tweaks - the button is in the sidebar.\n\nEnjoy!",
        ["theme_toggle"] = "🌓  Toggle theme",
        ["pc_bench"] = "Benchmark (now)",
        ["bench_desc"] = "CPU performance test: single-core and multi-threaded.",
        ["bench_run"] = "▶  Run test",
        ["bench_running"] = "Running test... please wait",
        ["bench_single"] = "Single-core",
        ["bench_multi"] = "Multi-threaded",
        ["bench_score"] = "points",
        ["uwp_warn"] = "Removal is permanent. Many apps can be restored via Microsoft Store.",
        ["uwp_scan"] = "🔍  Scan installed UWP apps",
        ["uwp_scanning"] = "Scanning...",
        ["uwp_remove"] = "Remove",
        ["uwp_removed"] = "Removed ✓",
        ["uwp_err"] = "Error",
        ["uwp_none"] = "No removable apps found",
        ["uwp_found"] = "Found apps: ",
        ["about_dev"] = "Developer",
        ["about_ver"] = "Version",
        ["about_gh"] = "GitHub project",
        ["about_dc"] = "Discord",
        ["about_site"] = "Website",
        ["open_gh"] = "Open GitHub ↗",
        ["open_site"] = "Open Website ↗",
        ["copy_dc"] = "Copy Discord tag",
        ["copied"] = "Copied!",
        ["about_desc"] = "Fast and safe Windows optimization.\nEvery tweak is reversible with the toggles.",
        ["pc_system"] = "System", ["pc_cpu"] = "Processor", ["pc_mem"] = "Memory",
        ["pc_gpu"] = "Graphics", ["pc_disk"] = "Disk C:", ["pc_net"] = "Network",
        ["pc_mobo"] = "Motherboard",
        ["l_os"] = "OS", ["l_host"] = "PC name", ["l_uptime"] = "Uptime",
        ["l_cpu"] = "CPU", ["l_cores"] = "Cores", ["l_threads"] = "Threads", ["l_mhz"] = "Clock (max)",
        ["l_ram"] = "RAM total", ["l_ramfree"] = "RAM free", ["l_gpu"] = "GPU", ["l_vram"] = "VRAM",
        ["l_disk"] = "Disk", ["l_ip"] = "IP", ["l_mfr"] = "Manufacturer", ["l_board"] = "Board", ["l_bios"] = "BIOS",
    },
    new()
    {
        ["performance"] = "Performance", ["telemetry"] = "Telemetry", ["ads"] = "Ads & Bloat",
        ["cleanup"] = "Cleanup", ["network"] = "Network", ["privacy"] = "Privacy",
        ["dashboard"] = "Dashboard", ["benchmark"] = "Benchmark", ["startup"] = "Startup", ["services"] = "Services", ["apps"] = "Uninstall apps",
        ["uwp"] = "Remove UWP Apps", ["pcinfo"] = "PC Info", ["about"] = "About",
    });
}
