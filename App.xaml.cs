using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Media;
using WinTweaker.Services;

namespace WinTweaker;

public partial class App : Application
{
    private static ResourceDictionary? _lightDict;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // If this exe is named *Setup* (or launched with --install), act as an
        // installer instead of the main app. Lets a plain copy of the exe work
        // as a self-contained installer with no external tools (no Inno/IExpress).
        string exe = Environment.ProcessPath ?? "";
        string fileName = Path.GetFileName(exe);
        bool setupMode =
            fileName.Contains("Setup", StringComparison.OrdinalIgnoreCase) ||
            (e.Args.Length > 0 && e.Args[0].Equals("--install", StringComparison.OrdinalIgnoreCase));

        if (setupMode)
        {
            Installer.Run(exe);
            Shutdown();
            return;
        }

        FirstRunDotNetCheck();
        ApplyTheme(Settings.Load().Theme);
        new MainWindow().Show();
    }

    // ── Light / dark theme ───────────────────────────────
    public static void ApplyTheme(string theme)
    {
        var dicts = Current.Resources.MergedDictionaries;
        if (_lightDict != null) { dicts.Remove(_lightDict); _lightDict = null; }
        if (theme == "light")
        {
            _lightDict = BuildLight();
            dicts.Add(_lightDict);   // added last -> overrides dark brushes
        }
    }

    public static void ToggleTheme()
    {
        var s = Settings.Load();
        s.Theme = s.Theme == "light" ? "dark" : "light";
        s.Save();
        ApplyTheme(s.Theme);
        var old = Current.MainWindow;
        var w = new MainWindow();
        Current.MainWindow = w;
        w.Show();
        old?.Close();
    }

    private static ResourceDictionary BuildLight()
    {
        var d = new ResourceDictionary();
        void B(string key, byte r, byte g, byte b) =>
            d[key] = new SolidColorBrush(Color.FromRgb(r, g, b));

        B("BgDarkBrush",      0xF2, 0xF3, 0xF7);
        B("BgSidebarBrush",   0xFF, 0xFF, 0xFF);
        B("BgPanelBrush",     0xF6, 0xF7, 0xFA);
        B("BgCardBrush",      0xFF, 0xFF, 0xFF);
        B("BgCardHoverBrush", 0xF0, 0xF1, 0xF6);
        B("CardBorderBrush",  0xE2, 0xE3, 0xEC);
        B("PrimaryTextBrush", 0x1A, 0x1A, 0x24);
        B("SecBrush",         0x5A, 0x5A, 0x6C);
        B("MutedBrush",       0x8A, 0x8A, 0x99);
        B("SwitchOffBrush",   0xCE, 0xD0, 0xDA);
        B("NavHoverBrush",    0xEC, 0xED, 0xF3);
        B("NavActiveBrush",   0xE4, 0xE7, 0xF2);
        B("ScrollThumbBrush", 0xC2, 0xC2, 0xCE);
        B("ChipBrush",        0xE8, 0xE9, 0xEF);
        B("ChipHoverBrush",   0xDC, 0xDD, 0xE6);
        B("SearchBrush",      0xFF, 0xFF, 0xFF);
        B("BtnSecondaryBrush",     0xE8, 0xE9, 0xEF);
        B("BtnSecondaryTextBrush", 0x1A, 0x1A, 0x24);
        return d;
    }

    // On first launch, verify .NET 8 is present. (Self-contained builds bundle
    // it, so this normally passes; it's a safety net + helpful link otherwise.)
    private static void FirstRunDotNetCheck()
    {
        const string DownloadUrl = "https://dotnet.microsoft.com/download/dotnet/8.0";
        try
        {
            var settings = Settings.Load();
            if (settings.FirstRunDone) return;

            settings.FirstRunDone = true;
            // Fresh install: the welcome dialog already greets the user, so mark
            // this version as seen and don't also pop the "What's new" changelog.
            settings.LastSeenVersion = WinTweaker.Data.AppInfo.Version;
            settings.Save();

            if (Environment.Version.Major < 8)
            {
                var r = MessageBox.Show(
                    "Для работы WinTweaker требуется .NET 8.0 или новее, но он не обнаружен.\n\n" +
                    "Открыть страницу загрузки .NET 8.0?",
                    "Требуется .NET 8.0", MessageBoxButton.OKCancel, MessageBoxImage.Warning);
                if (r == MessageBoxResult.OK)
                    Process.Start(new ProcessStartInfo(DownloadUrl) { UseShellExecute = true });
            }

            // Onboarding welcome
            var s = WinTweaker.Data.Strings.Get(settings.Lang);
            MessageBox.Show(s["welcome_msg"], s["welcome_title"],
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch { /* ignore */ }
    }
}
