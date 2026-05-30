namespace WinTweaker.Data;

/// <summary>App-wide constants: version and developer contacts.</summary>
public static class AppInfo
{
    public const string Version = "1.16";
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

    public static readonly Dictionary<string, string> Icons = new()
    {
        ["performance"] = "⚡", ["telemetry"] = "🔒", ["ads"] = "🚫",
        ["cleanup"] = "🧹", ["network"] = "🌐", ["privacy"] = "🔏",
        ["uwp"] = "🗑", ["pcinfo"] = "💻", ["about"] = "ℹ️",
    };

    /// <summary>Full sidebar order (tweak sections + special pages).</summary>
    public static readonly string[] NavOrder =
        { "performance", "telemetry", "ads", "cleanup", "network", "privacy", "uwp", "pcinfo", "about" };

    public static readonly HashSet<string> SpecialSections = new() { "uwp", "pcinfo", "about" };

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
        ["refresh"] = "🔄  Обновить",
        ["loading"] = "Загрузка...",
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
        ["pc_system"] = "🖥  Система", ["pc_cpu"] = "⚡  Процессор", ["pc_mem"] = "🧠  Память",
        ["pc_gpu"] = "🎮  Графика", ["pc_disk"] = "💾  Диск C:", ["pc_net"] = "🌐  Сеть",
        ["pc_mobo"] = "🔩  Материнская плата",
        ["l_os"] = "ОС", ["l_host"] = "Имя ПК", ["l_uptime"] = "Аптайм",
        ["l_cpu"] = "CPU", ["l_cores"] = "Ядра", ["l_threads"] = "Потоки", ["l_mhz"] = "Частота (макс)",
        ["l_ram"] = "ОЗУ всего", ["l_ramfree"] = "ОЗУ свободно", ["l_gpu"] = "GPU", ["l_vram"] = "VRAM",
        ["l_disk"] = "Диск", ["l_ip"] = "IP", ["l_board"] = "Плата", ["l_bios"] = "BIOS",
    },
    new()
    {
        ["performance"] = "Производительность", ["telemetry"] = "Телеметрия", ["ads"] = "Реклама",
        ["cleanup"] = "Очистка", ["network"] = "Сеть", ["privacy"] = "Приватность",
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
        ["refresh"] = "🔄  Refresh",
        ["loading"] = "Loading...",
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
        ["pc_system"] = "🖥  System", ["pc_cpu"] = "⚡  Processor", ["pc_mem"] = "🧠  Memory",
        ["pc_gpu"] = "🎮  Graphics", ["pc_disk"] = "💾  Disk C:", ["pc_net"] = "🌐  Network",
        ["pc_mobo"] = "🔩  Motherboard",
        ["l_os"] = "OS", ["l_host"] = "PC name", ["l_uptime"] = "Uptime",
        ["l_cpu"] = "CPU", ["l_cores"] = "Cores", ["l_threads"] = "Threads", ["l_mhz"] = "Clock (max)",
        ["l_ram"] = "RAM total", ["l_ramfree"] = "RAM free", ["l_gpu"] = "GPU", ["l_vram"] = "VRAM",
        ["l_disk"] = "Disk", ["l_ip"] = "IP", ["l_board"] = "Board", ["l_bios"] = "BIOS",
    },
    new()
    {
        ["performance"] = "Performance", ["telemetry"] = "Telemetry", ["ads"] = "Ads & Bloat",
        ["cleanup"] = "Cleanup", ["network"] = "Network", ["privacy"] = "Privacy",
        ["uwp"] = "Remove UWP Apps", ["pcinfo"] = "PC Info", ["about"] = "About",
    });
}
