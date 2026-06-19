namespace WinTweaker.Services;

public sealed record SvcItem(string Name, string Title, string Desc, bool Running);

/// <summary>Curated set of commonly-tweaked Windows services: status + on/off.</summary>
public static class WinServices
{
    // name, friendly title, short description
    private static readonly (string Name, string Title, string Desc)[] Curated =
    {
        ("DiagTrack",        "Connected User Experiences", "Телеметрия и слежка Microsoft"),
        ("dmwappushservice", "WAP Push Message Routing",   "Передаёт данные телеметрии"),
        ("SysMain",          "SysMain (Superfetch)",       "Предзагрузка приложений в ОЗУ"),
        ("WSearch",          "Windows Search",             "Индексирование поиска (нагрузка на диск)"),
        ("Spooler",          "Print Spooler",              "Очередь печати (не нужна без принтера)"),
        ("Fax",              "Fax",                        "Служба факса"),
        ("RemoteRegistry",   "Remote Registry",            "Удалённый доступ к реестру (риск)"),
        ("MapsBroker",       "Downloaded Maps Manager",    "Офлайн-карты"),
        ("RetailDemo",       "Retail Demo Service",        "Демо-режим магазина"),
        ("WMPNetworkSvc",    "WMP Network Sharing",        "Сетевой доступ Windows Media Player"),
        ("XblGameSave",      "Xbox Live Game Save",        "Сохранения Xbox Live"),
        ("PhoneSvc",         "Phone Service",              "Телефония"),
    };

    public static Task<List<SvcItem>> ScanAsync() => Task.Run(Scan);

    private static List<SvcItem> Scan()
    {
        string names = string.Join(",", Curated.Select(c => "'" + c.Name + "'"));
        string script =
            $"@({names}) | ForEach-Object {{ $s = Get-Service -Name $_ -ErrorAction SilentlyContinue; " +
            "if($s){ $_ + '|' + ($s.Status -eq 'Running') } }";
        string raw = CommandRunner.RunPowerShell(script, 20_000);

        var status = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
        foreach (var line in raw.Split('\n'))
        {
            var s = line.Trim();
            if (!s.Contains('|')) continue;
            var p = s.Split('|');
            status[p[0].Trim()] = p[1].Trim().Equals("True", StringComparison.OrdinalIgnoreCase);
        }

        var list = new List<SvcItem>();
        foreach (var c in Curated)
            if (status.TryGetValue(c.Name, out var running))
                list.Add(new SvcItem(c.Name, c.Title, c.Desc, running));
        return list;
    }

    public static Task SetAsync(string name, bool run) => Task.Run(() =>
    {
        string cmd = run
            ? $"sc config {name} start=auto & sc start {name}"
            : $"sc stop {name} & sc config {name} start=disabled";
        CommandRunner.Run(cmd);
    });
}
