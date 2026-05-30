using System.IO;
using System.Security.Principal;
using System.Text.Json;

namespace WinTweaker.Services;

public sealed class Settings
{
    public string Lang { get; set; } = "ru";
    public Dictionary<string, bool> Tweaks { get; set; } = new();

    /// <summary>
    /// Settings live in %AppData%\WinTweaker\wintweaker_settings.json — a
    /// per-user, always-writable location (works even when the app is
    /// installed to Program Files).
    /// </summary>
    private static string FilePath
    {
        get
        {
            string dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "WinTweaker");
            try { Directory.CreateDirectory(dir); } catch { /* ignore */ }
            return Path.Combine(dir, "wintweaker_settings.json");
        }
    }

    private static readonly JsonSerializerOptions Opts = new()
    {
        WriteIndented = true
    };

    public static Settings Load()
    {
        try
        {
            if (File.Exists(FilePath))
            {
                string json = File.ReadAllText(FilePath);
                return JsonSerializer.Deserialize<Settings>(json) ?? new Settings();
            }
        }
        catch { /* ignore corrupt config */ }
        return new Settings();
    }

    public void Save()
    {
        try
        {
            File.WriteAllText(FilePath, JsonSerializer.Serialize(this, Opts));
        }
        catch { /* ignore */ }
    }

    public bool GetTweak(string id) => Tweaks.TryGetValue(id, out var v) && v;
    public void SetTweak(string id, bool value) => Tweaks[id] = value;

    public static bool IsAdministrator()
    {
        try
        {
            using var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }
        catch { return false; }
    }
}
