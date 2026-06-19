namespace WinTweaker.Services;

public sealed record UwpApp(string Name, string Package)
{
    /// <summary>Friendly display name (strip vendor prefixes).</summary>
    public string Display =>
        Name.Replace("Microsoft.", "").Replace("MicrosoftCorporationII.", "");
}

public static class UwpApps
{
    private const string ScanScript = @"
Get-AppxPackage | Where-Object {
    $_.NonRemovable -ne $true -and
    $_.IsFramework -ne $true -and
    $_.Name -notlike 'Microsoft.UI*' -and
    $_.Name -notlike 'Microsoft.VCLibs*' -and
    $_.Name -notlike 'Microsoft.NET*' -and
    $_.Name -notlike 'Microsoft.Services*' -and
    $_.Name -notlike '*DesktopAppInstaller*' -and
    $_.Name -ne 'Microsoft.WindowsStore'
} | Sort-Object Name | ForEach-Object { $_.Name + '|' + $_.PackageFullName }
";

    public static Task<List<UwpApp>> ScanAsync() => Task.Run(Scan);

    private static List<UwpApp> Scan()
    {
        string raw = CommandRunner.RunPowerShell(ScanScript, 30_000);
        var apps = new List<UwpApp>();
        foreach (var line in raw.Split('\n'))
        {
            var s = line.Trim();
            if (s.Length == 0 || !s.Contains('|')) continue;
            var parts = s.Split('|', 2);
            string name = parts[0].Trim();
            string pkg = parts.Length > 1 ? parts[1].Trim() : "";
            if (name.Length == 0) continue;
            apps.Add(new UwpApp(name, pkg));
        }
        return apps;
    }

    /// <summary>Removes an app for current + all users. Returns true if gone afterwards.</summary>
    public static Task<bool> RemoveAsync(UwpApp app) => Task.Run(() => Remove(app));

    private static bool Remove(UwpApp app)
    {
        string name = app.Name.Replace("'", "").Replace("\"", "");
        string script =
            $"Get-AppxPackage -Name '{name}' | Remove-AppxPackage -ErrorAction SilentlyContinue; " +
            $"Get-AppxPackage -AllUsers -Name '{name}' | Remove-AppxPackage -AllUsers -ErrorAction SilentlyContinue";
        CommandRunner.RunPowerShell(script, 60_000);

        // Verify it is gone
        string check = CommandRunner.RunPowerShell(
            $"(Get-AppxPackage -Name '{name}' | Select-Object -First 1).Name");
        return !check.Trim().Equals(name, StringComparison.OrdinalIgnoreCase);
    }
}
