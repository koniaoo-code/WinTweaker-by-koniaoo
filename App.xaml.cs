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
        var old = Current.MainWindow as MainWindow;
        string curSec = old?.CurrentSection ?? "dashboard";
        var w = new MainWindow(curSec);
        Current.MainWindow = w;
        w.Show();
        old?.Close();
    }

    private static ResourceDictionary BuildLight()
    {
        var d = new ResourceDictionary();
        void B(string key, byte r, byte g, byte b) =>
            d[key] = new SolidColorBrush(Color.FromRgb(r, g, b));

        B("BgDarkBrush",           0xF7, 0xF9, 0xFD);
        B("BgSidebarBrush",        0xEE, 0xF2, 0xF8);
        B("BgPanelBrush",          0xF4, 0xF6, 0xFB);
        B("BgCardBrush",           0xFF, 0xFF, 0xFF);
        B("BgCardHoverBrush",      0xEE, 0xF3, 0xFB);
        B("CardBorderBrush",       0xDC, 0xE1, 0xEC);
        B("PrimaryTextBrush",      0x19, 0x1C, 0x20);
        B("SecBrush",              0x43, 0x47, 0x4E);
        B("MutedBrush",            0x73, 0x77, 0x7F);
        B("OrangeBrush",           0x2B, 0x6C, 0xB0);
        B("OrangeHoverBrush",      0x3D, 0x7E, 0xC4);
        B("OnOrangeBrush",         0xFF, 0xFF, 0xFF);
        B("GreenBrush",            0x1B, 0x87, 0x3F);
        B("RedBrush",              0xBA, 0x1A, 0x1A);
        B("SwitchOffBrush",        0xDA, 0xDF, 0xE9);
        B("SwitchOffBorderBrush",  0x73, 0x77, 0x7F);
        B("SwitchOffThumbBrush",   0x73, 0x77, 0x7F);
        B("SwitchOnThumbBrush",    0xFF, 0xFF, 0xFF);
        B("NavHoverBrush",         0xE3, 0xEA, 0xF4);
        B("NavActiveBrush",        0xD7, 0xE3, 0xF4);
        B("NavActiveTextBrush",    0x04, 0x1E, 0x49);
        B("ScrollThumbBrush",      0xC0, 0xC6, 0xD4);
        B("ChipBrush",             0xEA, 0xEE, 0xF6);
        B("ChipHoverBrush",        0xDE, 0xE3, 0xED);
        B("SearchBrush",           0xFF, 0xFF, 0xFF);
        B("BtnSecondaryBrush",     0xE4, 0xE9, 0xF2);
        B("BtnSecondaryHoverBrush",0xD8, 0xDF, 0xEA);
        B("BtnSecondaryTextBrush", 0x19, 0x1C, 0x20);
        B("OrangeContainerBrush",  0xD3, 0xE3, 0xF4);
        B("OnOrangeContainerBrush",0x04, 0x1E, 0x49);
        return d;
    }

    // On first launch, verify .NET 10 is present. (Self-contained builds bundle
    // it, so this normally passes; it's a safety net + helpful link otherwise.)
    private static void FirstRunDotNetCheck()
    {
        const string DownloadUrl = "https://dotnet.microsoft.com/download/dotnet/10.0";
        try
        {
            var settings = Settings.Load();
            if (settings.FirstRunDone) return;

            settings.FirstRunDone = true;
            // Fresh install: the welcome dialog already greets the user, so mark
            // this version as seen and don't also pop the "What's new" changelog.
            settings.LastSeenVersion = WinTweaker.Data.AppInfo.Version;
            settings.Save();

            if (Environment.Version.Major < 10)
            {
                var r = MessageBox.Show(
                    "Для работы WinTweaker рекомендуется .NET 10.0 или новее.\n\n" +
                    "Открыть страницу загрузки .NET 10.0?",
                    "Требуется .NET 10.0", MessageBoxButton.OKCancel, MessageBoxImage.Warning);
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
