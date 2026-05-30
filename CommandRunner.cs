using System.Diagnostics;
using System.Text;

namespace WinTweaker.Services;

public static class CommandRunner
{
    /// <summary>
    /// Runs a cmd.exe command with UTF-8 output forced (no кракозябры),
    /// hidden window, 30s timeout. Returns (ok, trimmed output).
    /// </summary>
    public static (bool Ok, string Output) Run(string command)
    {
        try
        {
            var psi = new ProcessStartInfo("cmd.exe", "/c chcp 65001>nul & " + command)
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8,
            };

            using var p = Process.Start(psi);
            if (p is null) return (false, "Failed to start process");

            string stdout = p.StandardOutput.ReadToEnd();
            string stderr = p.StandardError.ReadToEnd();
            if (!p.WaitForExit(30_000))
            {
                try { p.Kill(true); } catch { /* ignore */ }
                return (false, "Timeout");
            }

            string raw = (stdout + stderr).Trim();
            var lines = raw
                .Split('\n')
                .Where(l => !l.TrimStart().StartsWith("Active code page", StringComparison.OrdinalIgnoreCase));
            string outp = string.Join("\n", lines).Trim();

            return (p.ExitCode == 0, string.IsNullOrEmpty(outp) ? "OK" : outp);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    /// <summary>
    /// Runs a PowerShell script via -EncodedCommand (base64 UTF-16) so there
    /// are no quoting headaches, with forced UTF-8 output. Returns stdout.
    /// </summary>
    public static string RunPowerShell(string script, int timeoutMs = 25_000)
    {
        try
        {
            string full =
                "$OutputEncoding=[System.Text.Encoding]::UTF8;" +
                "[Console]::OutputEncoding=[System.Text.Encoding]::UTF8;" +
                "$ErrorActionPreference='SilentlyContinue';" + script;
            string enc = Convert.ToBase64String(Encoding.Unicode.GetBytes(full));

            var psi = new ProcessStartInfo("powershell",
                "-NoProfile -NonInteractive -ExecutionPolicy Bypass -EncodedCommand " + enc)
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8,
            };

            using var p = Process.Start(psi);
            if (p is null) return "";
            string outp = p.StandardOutput.ReadToEnd();
            if (!p.WaitForExit(timeoutMs))
            {
                try { p.Kill(true); } catch { /* ignore */ }
                return "";
            }
            return outp;
        }
        catch { return ""; }
    }

    /// <summary>Reboots the machine immediately.</summary>
    public static void Reboot()
    {
        try
        {
            Process.Start(new ProcessStartInfo("shutdown", "/r /t 0")
            {
                CreateNoWindow = true,
                UseShellExecute = false,
            });
        }
        catch { /* ignore */ }
    }
}
