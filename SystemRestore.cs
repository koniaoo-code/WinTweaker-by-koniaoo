namespace WinTweaker.Services;

/// <summary>Creates a Windows System Restore point (needs admin).</summary>
public static class SystemRestore
{
    public static Task<bool> CreateAsync(string description = "WinTweaker") =>
        Task.Run(() => Create(description));

    private static bool Create(string description)
    {
        // Remove the once-per-24h limit, ensure restore is on for C:, then checkpoint.
        string script =
            "try { " +
            "New-ItemProperty -Path 'HKLM:\\Software\\Microsoft\\Windows NT\\CurrentVersion\\SystemRestore' " +
            "-Name 'SystemRestorePointCreationFrequency' -Value 0 -PropertyType DWord -Force | Out-Null; " +
            "Enable-ComputerRestore -Drive 'C:\\' -ErrorAction SilentlyContinue; " +
            "Checkpoint-Computer -Description '" + description.Replace("'", "") + "' " +
            "-RestorePointType 'MODIFY_SETTINGS' -ErrorAction Stop; 'OK' } catch { 'FAIL' }";

        string outp = CommandRunner.RunPowerShell(script, 120_000);
        return outp.Contains("OK");
    }
}
