namespace WinTweaker.Services;

/// <summary>
/// Collects PC info via a single PowerShell call (CIM/WMI). Keys:
/// os, host, cpu, cores, threads, mhz, ram, ramfree, gpu, vram, disk, mobo, bios, uptime, ip.
/// </summary>
public static class SystemInfo
{
    private static readonly string[] Keys =
        { "os", "host", "cpu", "cores", "threads", "mhz", "ram", "ramfree",
          "gpu", "vram", "disk", "mobo", "bios", "uptime", "ip" };

    private const string Script = @"
$sep = '|||'
$os = Get-CimInstance Win32_OperatingSystem
($os.Caption + ' ' + $os.Version); $sep
$env:COMPUTERNAME; $sep
$cpu = Get-CimInstance Win32_Processor | Select-Object -First 1
$cpu.Name; $sep
$cpu.NumberOfCores; $sep
$cpu.NumberOfLogicalProcessors; $sep
$cpu.MaxClockSpeed; $sep
[math]::Round((Get-CimInstance Win32_ComputerSystem).TotalPhysicalMemory/1GB,1); $sep
[math]::Round($os.FreePhysicalMemory/1MB,1); $sep
$g = Get-CimInstance Win32_VideoController | Select-Object -First 1
$g.Name; $sep
[math]::Round($g.AdapterRAM/1GB,1); $sep
$d = Get-CimInstance Win32_LogicalDisk -Filter ""DeviceID='C:'""
([math]::Round($d.Size/1GB,0).ToString() + ' GB total, ' + [math]::Round($d.FreeSpace/1GB,1).ToString() + ' GB free'); $sep
(Get-CimInstance Win32_BaseBoard).Product; $sep
(Get-CimInstance Win32_BIOS).SMBIOSBIOSVersion; $sep
$u = (Get-Date) - $os.LastBootUpTime
('{0}d {1}h {2}m' -f $u.Days, $u.Hours, $u.Minutes); $sep
(Get-NetIPAddress -AddressFamily IPv4 | Where-Object { $_.InterfaceAlias -notlike '*Loopback*' } | Select-Object -First 1).IPAddress
";

    public static Task<Dictionary<string, string>> CollectAsync() => Task.Run(Collect);

    private static Dictionary<string, string> Collect()
    {
        string raw = CommandRunner.RunPowerShell(Script);
        var parts = raw.Split("|||");
        var info = new Dictionary<string, string>();

        for (int i = 0; i < Keys.Length; i++)
        {
            string val = "—";
            if (i < parts.Length)
            {
                var line = parts[i].Split('\n').Select(l => l.Trim()).FirstOrDefault(l => l.Length > 0);
                if (!string.IsNullOrEmpty(line)) val = line;
            }
            string k = Keys[i];
            if ((k == "ram" || k == "vram") && val != "—" && !val.EndsWith("GB")) val += " GB";
            if (k == "mhz" && val != "—" && long.TryParse(val, out _)) val += " MHz";
            info[k] = val;
        }
        return info;
    }
}
