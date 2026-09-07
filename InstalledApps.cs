using System.Diagnostics;

namespace WinTweaker.Services;

public sealed record InstalledApp(string Name, string Uninstall, string Publisher);

/// <summary>Lists installed desktop programs (registry Uninstall keys) and runs their uninstaller.</summary>
public static class InstalledApps
{
    private const string ScanScript = @"
$keys = @(
  'HKLM:\Software\Microsoft\Windows\CurrentVersion\Uninstall\*',
  'HKLM:\Software\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\*',
  'HKCU:\Software\Microsoft\Windows\CurrentVersion\Uninstall\*'
)
Get-ItemProperty $keys -ErrorAction SilentlyContinue |
  Where-Object { $_.DisplayName -and $_.UninstallString -and $_.SystemComponent -ne 1 } |
  Sort-Object DisplayName -Unique |
  ForEach-Object { $_.DisplayName + '|' + $_.UninstallString + '|' + $_.Publisher }
";

    public static Task<List<InstalledApp>> ScanAsync() => Task.Run(Scan);

    private static List<InstalledApp> Scan()
    {
        string raw = CommandRunner.RunPowerShell(ScanScript, 30_000);
        var list = new List<InstalledApp>();
        foreach (var line in raw.Split('\n'))
        {
            var s = line.Trim();
            if (s.Length == 0 || !s.Contains('|')) continue;
            var p = s.Split('|', 3);
            string name = p[0].Trim();
            string unins = p.Length > 1 ? p[1].Trim() : "";
            string pub = p.Length > 2 ? p[2].Trim() : "";
            if (name.Length == 0 || unins.Length == 0) continue;
            list.Add(new InstalledApp(name, unins, pub));
        }
        return list;
    }

    /// <summary>Runs the program's uninstaller (shows the vendor's UI). Returns ok.</summary>
    public static Task<bool> UninstallAsync(InstalledApp app) => Task.Run(() =>
    {
        try
        {
            var psi = new ProcessStartInfo("cmd.exe", "/c " + app.Uninstall)
            {
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using var p = Process.Start(psi);
            if (p == null) return false;
            p.WaitForExit(300_000); // 5 minutes for interactive uninstaller wizards
            return p.ExitCode == 0;
        }
        catch
        {
            var (ok, _) = CommandRunner.Run(app.Uninstall);
            return ok;
        }
    });
}
