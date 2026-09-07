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
        string lang = "ru";
        try { lang = Settings.Load().Lang; } catch { }
        bool isRu = lang == "ru" || (string.IsNullOrEmpty(lang) && System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName.Equals("ru", StringComparison.OrdinalIgnoreCase));

        string dest = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Programs", "WinTweaker");
        string destExe = Path.Combine(dest, "WinTweaker.exe");

        string title = isRu ? "Установка WinTweaker" : "WinTweaker Setup";
        string askMsg = isRu
            ? $"Установить WinTweaker {AppInfo.Version}?\n\nПапка установки:\n{dest}\n\nБудут созданы ярлыки на рабочем столе и в меню «Пуск»."
            : $"Install WinTweaker {AppInfo.Version}?\n\nDestination folder:\n{dest}\n\nDesktop and Start Menu shortcuts will be created.";

        var ask = MessageBox.Show(askMsg, title, MessageBoxButton.OKCancel, MessageBoxImage.Information);
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

            string doneMsg = isRu
                ? $"WinTweaker {AppInfo.Version} успешно установлен!\nЯрлыки созданы на рабочем столе и в меню «Пуск».\n\nЗапустить программу сейчас?"
                : $"WinTweaker {AppInfo.Version} installed successfully!\nShortcuts created on Desktop and Start Menu.\n\nLaunch WinTweaker now?";
            string doneTitle = isRu ? "Установка завершена" : "Setup Complete";

            var run = MessageBox.Show(doneMsg, doneTitle, MessageBoxButton.YesNo, MessageBoxImage.Information);
            if (run == MessageBoxResult.Yes)
                Process.Start(new ProcessStartInfo(destExe) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            string errMsg = isRu ? ("Ошибка установки:\n" + ex.Message) : ("Installation error:\n" + ex.Message);
            MessageBox.Show(errMsg, title, MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private static void CreateShortcut(string lnkPath, string targetExe)
    {
        string dir = Path.GetDirectoryName(targetExe) ?? "";
        // Escape single quotes so paths with an apostrophe (e.g. a username
        // like O'Brien) don't break the PowerShell string literals.
        string Q(string s) => s.Replace("'", "''");
        string ps =
            "$w = New-Object -ComObject WScript.Shell; " +
            $"$s = $w.CreateShortcut('{Q(lnkPath)}'); " +
            $"$s.TargetPath = '{Q(targetExe)}'; " +
            $"$s.IconLocation = '{Q(targetExe)}'; " +
            $"$s.WorkingDirectory = '{Q(dir)}'; " +
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
