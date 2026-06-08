namespace WinTweaker.Services;

public sealed record StartupItem(string Name, string Command, string RunPath, bool Enabled);

/// <summary>Lists and toggles Windows startup (Run) entries via the
/// StartupApproved registry state (same mechanism Task Manager uses).</summary>
public static class StartupApps
{
    private const string ScanScript = @"
$runs = @('HKCU:\Software\Microsoft\Windows\CurrentVersion\Run','HKLM:\Software\Microsoft\Windows\CurrentVersion\Run')
foreach($p in $runs){
  $item = Get-Item $p -ErrorAction SilentlyContinue
  if($item){
    foreach($n in $item.Property){
      $cmd = (Get-ItemProperty -Path $p -Name $n).$n
      $approved = $p -replace 'CurrentVersion\\Run','CurrentVersion\Explorer\StartupApproved\Run'
      $state = '1'
      try { $b = (Get-ItemProperty -Path $approved -Name $n -ErrorAction Stop).$n; if($b[0] -band 1){ $state='0' } } catch {}
      ($n + '|' + $cmd + '|' + $p + '|' + $state)
    }
  }
}
";

    public static Task<List<StartupItem>> ScanAsync() => Task.Run(Scan);

    private static List<StartupItem> Scan()
    {
        string raw = CommandRunner.RunPowerShell(ScanScript, 20_000);
        var list = new List<StartupItem>();
        foreach (var line in raw.Split('\n'))
        {
            var s = line.Trim();
            if (s.Length == 0 || !s.Contains('|')) continue;
            var p = s.Split('|');
            if (p.Length < 4) continue;
            string name = p[0].Trim();
            bool en = p[^1].Trim() == "1";
            string path = p[^2].Trim();
            string cmd = string.Join("|", p[1..^2]).Trim();
            if (name.Length == 0) continue;
            list.Add(new StartupItem(name, cmd, path, en));
        }
        return list;
    }

    public static Task SetEnabledAsync(StartupItem item, bool enabled) =>
        Task.Run(() => SetEnabled(item, enabled));

    private static void SetEnabled(StartupItem item, bool enabled)
    {
        string approved = item.RunPath.Replace(
            @"CurrentVersion\Run", @"CurrentVersion\Explorer\StartupApproved\Run");
        string val = enabled ? "2,0,0,0,0,0,0,0,0,0,0,0" : "3,0,0,0,0,0,0,0,0,0,0,0";
        string name = item.Name.Replace("'", "''");
        string script =
            $"New-Item -Path '{approved}' -Force | Out-Null; " +
            $"New-ItemProperty -Path '{approved}' -Name '{name}' -Value ([byte[]]({val})) -PropertyType Binary -Force | Out-Null";
        CommandRunner.RunPowerShell(script, 15_000);
    }
}
