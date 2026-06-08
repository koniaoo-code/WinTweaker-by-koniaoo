using System.Diagnostics;
using System.IO;
using System.Windows;
using Microsoft.Win32;
using WinTweaker.Data;
using WinTweaker.Services;

namespace WinTweaker;

/// <summary>
/// Self-contained installer. Runs when the exe is named *Setup* — copies the
/// app to %LocalAppData%\Programs\WinTweaker, makes shortcuts and registers an
/// uninstall entry. No external tools (no Inno Setup / IExpress) required.
/// </summary>
public static class Installer
{
    public static void Run(string currentExe)
    {
        string dest = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Programs", "WinTweaker");
        string destExe = Path.Combine(dest, "WinTweaker.exe");

        var ask = MessageBox.Show(
            $"Установить WinTweaker {AppInfo.Version}?\n\nПапка: {dest}\nБудут созданы ярлыки на рабочем столе и в меню Пуск.",
            "Установка WinTweaker", MessageBoxButton.OKCancel, MessageBoxImage.Information);
        if (ask != MessageBoxResult.OK) return;

        try
        {
            Directory.CreateDirectory(dest);
            if (!string.Equals(currentExe, destExe, StringComparison.OrdinalIgnoreCase))
                File.Copy(currentExe, destExe, overwrite: true);

            string desktop = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "WinTweaker.lnk");
            string startMenu = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Programs), "WinTweaker.lnk");

            CreateShortcut(desktop, destExe);
            CreateShortcut(startMenu, destExe);
            RegisterUninstall(dest, destExe, desktop, startMenu);

            var run = MessageBox.Show(
                "WinTweaker установлен!\nЯрлыки созданы на рабочем столе и в меню Пуск.\n\nЗапустить сейчас?",
                "Готово", MessageBoxButton.YesNo, MessageBoxImage.Information);
            if (run == MessageBoxResult.Yes)
                Process.Start(new ProcessStartInfo(destExe) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            MessageBox.Show("Ошибка установки:\n" + ex.Message,
                "Установка WinTweaker", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private static void CreateShortcut(string lnkPath, string targetExe)
    {
        string dir = Path.GetDirectoryName(targetExe) ?? "";
        string ps =
            "$w = New-Object -ComObject WScript.Shell; " +
            $"$s = $w.CreateShortcut('{lnkPath}'); " +
            $"$s.TargetPath = '{targetExe}'; " +
            $"$s.IconLocation = '{targetExe}'; " +
            $"$s.WorkingDirectory = '{dir}'; " +
            "$s.Save()";
        CommandRunner.RunPowerShell(ps, 15_000);
    }

    private static void RegisterUninstall(string dest, string destExe, string desktop, string startMenu)
    {
        try
        {
            using var k = Registry.CurrentUser.CreateSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Uninstall\WinTweaker");
            if (k == null) return;
            k.SetValue("DisplayName", "WinTweaker");
            k.SetValue("DisplayVersion", AppInfo.Version);
            k.SetValue("Publisher", "koniaoo");
            k.SetValue("DisplayIcon", destExe);
            k.SetValue("InstallLocation", dest);
            k.SetValue("NoModify", 1, RegistryValueKind.DWord);
            k.SetValue("NoRepair", 1, RegistryValueKind.DWord);

            string unins =
                "cmd /c del /q \"" + desktop + "\" \"" + startMenu + "\" & " +
                "reg delete \"HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\WinTweaker\" /f & " +
                "rd /s /q \"" + dest + "\"";
            k.SetValue("UninstallString", unins);
        }
        catch { /* ignore */ }
    }
}
