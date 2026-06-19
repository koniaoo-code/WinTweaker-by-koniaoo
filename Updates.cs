using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using WinTweaker.Data;

namespace WinTweaker.Services;

public sealed record UpdateInfo(string? LatestTag, bool IsNewer);

/// <summary>Checks the latest GitHub release and compares it to the current version.</summary>
public static class Updates
{
    private const string LatestApi =
        "https://api.github.com/repos/koniaoo-code/WinTweaker-by-koniaoo/releases/latest";

    public const string ReleasesPage =
        "https://github.com/koniaoo-code/WinTweaker-by-koniaoo/releases/latest";

    public static async Task<UpdateInfo> CheckAsync()
    {
        string? tag = await LatestTagAsync();
        if (tag == null) return new UpdateInfo(null, false);
        return new UpdateInfo(tag, IsNewer(tag, AppInfo.Version));
    }

    public static async Task<string?> LatestTagAsync()
    {
        try
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(8) };
            http.DefaultRequestHeaders.UserAgent.ParseAdd("WinTweaker");
            string json = await http.GetStringAsync(LatestApi);
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.GetProperty("tag_name").GetString();
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// True only if the remote tag contains a real version number that is
    /// HIGHER than the local one. Non-version tags (e.g. "TWEAKERUPDATE")
    /// are treated as "not newer" so they never cause a false update alert.
    /// </summary>
    private static bool IsNewer(string remoteTag, string local)
    {
        var rv = ParseVersion(remoteTag);
        var lv = ParseVersion(local);
        if (rv != null && lv != null) return rv > lv;
        return false;
    }

    /// <summary>Extracts a version like 1.3 / v1.4.2 / "release-2.0" from any string.</summary>
    private static Version? ParseVersion(string s)
    {
        var m = Regex.Match(s ?? "", @"\d+(\.\d+){0,3}");
        if (!m.Success) return null;
        string v = m.Value;
        int dots = v.Count(c => c == '.');
        v = dots switch { 0 => v + ".0.0", 1 => v + ".0", _ => v };
        return Version.TryParse(v, out var ver) ? ver : null;
    }
}
