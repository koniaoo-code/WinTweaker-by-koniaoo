using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using WinTweaker.Data;
using WinTweaker.Helpers;
using WinTweaker.Models;
using WinTweaker.Services;

namespace WinTweaker;

public partial class MainWindow : Window
{
    private readonly Settings _settings;
    private Strings _s;
    private string _lang;
    private Dictionary<string, List<Tweak>> _data;
    private string _currentSec = "performance";
    private readonly Dictionary<string, Button> _navButtons = new();

    private Button? _copyBtn;
    private Button? _uwpScanBtn;
    private StackPanel? _uwpList;

    // PC Info: pre-warmed at startup + cached so opening the tab feels instant.
    private Task<Dictionary<string, string>>? _pcInfoTask;
    private Dictionary<string, string>? _pcInfoCache;

    private int _logoClicks;   // easter egg counter

    public MainWindow()
    {
        InitializeComponent();

        _settings = Settings.Load();
        _lang = _settings.Lang;
        _s = Strings.Get(_lang);
        _data = TweakData.Build(_lang, _settings);

        // Start gathering PC info right away so the tab opens instantly later.
        _pcInfoTask = SystemInfo.CollectAsync();

        ApplyTexts();
        BuildNav();
        ShowSection("performance");
        UpdateAdmin();
    }

    // ── Localized texts ──────────────────────────────────
    private void ApplyTexts()
    {
        ByLbl.Text = _s["by"];
        ApplyAllBtn.Content = _s["apply_all"];
        ApplySecBtn.Content = _s["apply_sec"];
        RebootBtn.Content = _s["reboot_btn"];
        LogLbl.Text = _s["log_lbl"];
        ClearBtn.Content = _s["log_clear"];
        LogShowBtn.Content = _s["log_show"];
    }

    // ── Sidebar navigation ───────────────────────────────
    private void BuildNav()
    {
        NavPanel.Children.Clear();
        _navButtons.Clear();
        foreach (var sec in Strings.NavOrder)
        {
            string icon = Strings.Icons.TryGetValue(sec, out var ic) ? ic : "•";
            string name = _s.Sections.TryGetValue(sec, out var n) ? n : sec;
            var btn = new Button
            {
                Content = $"  {icon}   {name}",
                Style = (Style)FindResource("NavButton"),
                Margin = new Thickness(0, 2, 0, 2),
                Tag = sec,
            };
            btn.Click += OnNav;
            NavPanel.Children.Add(btn);
            _navButtons[sec] = btn;
        }
    }

    private void OnNav(object sender, RoutedEventArgs e)
    {
        if (sender is Button b && b.Tag is string sec)
            ShowSection(sec);
    }

    private void ShowSection(string sec)
    {
        _currentSec = sec;
        string icon = Strings.Icons.TryGetValue(sec, out var ic) ? ic : "•";
        string name = _s.Sections.TryGetValue(sec, out var n) ? n : sec;
        SecTitle.Text = $"{icon}  {name}";

        var orange = (Brush)FindResource("OrangeBrush");
        var sec2 = (Brush)FindResource("SecBrush");
        var active = (Brush)FindResource("NavActiveBrush");
        foreach (var (s, btn) in _navButtons)
        {
            bool on = s == sec;
            btn.Foreground = on ? orange : sec2;
            btn.Background = on ? active : Brushes.Transparent;
        }

        ApplySecBtn.Visibility = Strings.SpecialSections.Contains(sec)
            ? Visibility.Collapsed : Visibility.Visible;

        ContentHost.Children.Clear();
        ContentScroll.ScrollToTop();
        SmoothScroll.Reset(ContentScroll);

        switch (sec)
        {
            case "pcinfo": BuildPcInfo(); break;
            case "about": BuildAbout(); break;
            case "uwp": BuildUwp(); break;
            default: BuildTweaks(sec); break;
        }
        FadeInContent();
    }

    private void FadeInContent()
    {
        // Dynamic: slide up + fade in.
        var tt = new TranslateTransform();
        ContentHost.RenderTransform = tt;
        var slide = new DoubleAnimation(18, 0, TimeSpan.FromMilliseconds(280))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };
        tt.BeginAnimation(TranslateTransform.YProperty, slide);

        var fade = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(240))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };
        ContentHost.BeginAnimation(OpacityProperty, fade);
    }

    // ── Tweak section ────────────────────────────────────
    private void BuildTweaks(string sec)
    {
        var ic = new ItemsControl
        {
            ItemsSource = _data.TryGetValue(sec, out var list) ? list : new List<Tweak>(),
            ItemTemplate = (DataTemplate)Resources["TweakCardTemplate"],
        };
        ContentHost.Children.Add(ic);
    }

    private async void OnToggleClick(object sender, RoutedEventArgs e)
    {
        if (sender is not ToggleButton tb || tb.DataContext is not Tweak t) return;

        bool state = t.IsEnabled;
        _settings.SetTweak(t.Id, state);
        _settings.Save();

        Log($"{(state ? _s["enable"] : _s["disable"])}: {t.Name}...", null);
        string cmd = state ? t.Enable : t.Disable;
        var (ok, outp) = await Task.Run(() => CommandRunner.Run(cmd));
        Log($"  {ShortOut(outp)}", ok);
        ShowRebootButton();          // tweak applied -> reveal reboot button
    }

    // ── Bulk apply (always ENABLES + flips toggles) ──────
    private async void OnApplySection(object sender, RoutedEventArgs e)
        => await ApplyBulk(_data[_currentSec], _s["applied_sec"]);

    private async void OnApplyAll(object sender, RoutedEventArgs e)
        => await ApplyBulk(_data.Values.SelectMany(x => x), _s["applied_all"]);

    private async Task ApplyBulk(IEnumerable<Tweak> tweaks, string doneMsg)
    {
        foreach (var t in tweaks.ToList())
        {
            t.IsEnabled = true;                 // flips the visible switch ON (animated)
            _settings.SetTweak(t.Id, true);
            Log($"► {t.Name}...", null);
            var (ok, outp) = await Task.Run(() => CommandRunner.Run(t.Enable));
            Log($"  {ShortOut(outp)}", ok);
        }
        _settings.Save();
        Log(doneMsg, true);
        ShowRebootButton();          // tweaks applied -> reveal reboot button
        PromptReboot();
    }

    // Reboot button is hidden until tweaks are actually applied.
    private void ShowRebootButton()
    {
        if (RebootBtn.Visibility == Visibility.Visible) return;
        RebootBtn.Visibility = Visibility.Visible;
        RebootBtn.BeginAnimation(OpacityProperty,
            new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(280))
            { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } });
    }

    private void PromptReboot()
    {
        var res = MessageBox.Show(this, _s["reboot_msg"], _s["reboot_title"],
            MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (res == MessageBoxResult.Yes)
            CommandRunner.Reboot();
    }

    // ── PC Info (pre-warmed + cached for instant open) ───
    private async void BuildPcInfo()
    {
        if (_pcInfoCache != null)          // already gathered -> render instantly
        {
            RenderPcInfo(_pcInfoCache);
            return;
        }

        ContentHost.Children.Add(new TextBlock
        {
            Text = _s["loading"],
            Foreground = (Brush)FindResource("SecBrush"),
            FontSize = 13,
            Margin = new Thickness(4, 30, 0, 0),
        });

        _pcInfoTask ??= SystemInfo.CollectAsync();
        var info = await _pcInfoTask;
        _pcInfoCache = info;
        if (_currentSec != "pcinfo") return;     // navigated away while loading
        RenderPcInfo(info);
    }

    private void RenderPcInfo(Dictionary<string, string> info)
    {
        ContentHost.Children.Clear();

        var refresh = new Button
        {
            Content = _s["refresh"],
            Style = (Style)FindResource("SmallButton"),
            Height = 28,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(0, 0, 0, 4),
        };
        refresh.Click += (_, _) =>
        {
            _pcInfoCache = null;
            _pcInfoTask = SystemInfo.CollectAsync();
            ContentHost.Children.Clear();
            BuildPcInfo();
        };
        ContentHost.Children.Add(refresh);

        AddPcGroup(_s["pc_system"], (_s["l_os"], info["os"]), (_s["l_host"], info["host"]), (_s["l_uptime"], info["uptime"]));
        AddPcGroup(_s["pc_cpu"], (_s["l_cpu"], info["cpu"]), (_s["l_cores"], info["cores"]), (_s["l_threads"], info["threads"]), (_s["l_mhz"], info["mhz"]));
        AddPcGroup(_s["pc_mem"], (_s["l_ram"], info["ram"]), (_s["l_ramfree"], info["ramfree"]));
        AddPcGroup(_s["pc_gpu"], (_s["l_gpu"], info["gpu"]), (_s["l_vram"], info["vram"]));
        AddPcGroup(_s["pc_disk"], (_s["l_disk"], info["disk"]));
        AddPcGroup(_s["pc_net"], (_s["l_ip"], info["ip"]));
        AddPcGroup(_s["pc_mobo"], (_s["l_board"], info["mobo"]), (_s["l_bios"], info["bios"]));
        FadeInContent();
    }

    private void AddPcGroup(string title, params (string k, string v)[] rows)
    {
        ContentHost.Children.Add(new TextBlock
        {
            Text = title,
            FontWeight = FontWeights.Bold,
            FontSize = 12,
            Foreground = (Brush)FindResource("OrangeBrush"),
            Margin = new Thickness(2, 10, 0, 4),
        });

        var card = InfoCard();
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(140) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        for (int i = 0; i < rows.Length; i++)
        {
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            var kl = new TextBlock
            {
                Text = rows[i].k, FontSize = 11,
                Foreground = (Brush)FindResource("MutedBrush"),
                Margin = new Thickness(14, 5, 6, 5),
            };
            Grid.SetRow(kl, i); Grid.SetColumn(kl, 0); grid.Children.Add(kl);

            var vl = new TextBlock
            {
                Text = rows[i].v, FontSize = 12, FontWeight = FontWeights.Bold,
                Foreground = (Brush)FindResource("PrimaryTextBrush"),
                Margin = new Thickness(6, 5, 12, 5), TextWrapping = TextWrapping.Wrap,
            };
            Grid.SetRow(vl, i); Grid.SetColumn(vl, 1); grid.Children.Add(vl);
        }
        card.Child = grid;
        ContentHost.Children.Add(card);
    }

    // ── About ────────────────────────────────────────────
    private void BuildAbout()
    {
        var logo = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 28, 0, 2) };
        logo.Children.Add(new TextBlock { Text = "Win", FontFamily = new FontFamily("Segoe UI Black"), FontSize = 32, Foreground = (Brush)FindResource("PrimaryTextBrush") });
        logo.Children.Add(new TextBlock { Text = "Tweaker", FontFamily = new FontFamily("Segoe UI Black"), FontSize = 32, Foreground = (Brush)FindResource("OrangeBrush") });
        // Easter egg #2: click the logo to make it spin.
        var rot = new RotateTransform();
        logo.RenderTransform = rot;
        logo.RenderTransformOrigin = new Point(0.5, 0.5);
        logo.Cursor = Cursors.Hand;
        logo.MouseLeftButtonUp += (_, _) =>
        {
            rot.BeginAnimation(RotateTransform.AngleProperty,
                new DoubleAnimation(0, 360, TimeSpan.FromMilliseconds(700))
                { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut } });
            Log(_s["egg_about"], true);
        };
        ContentHost.Children.Add(logo);

        ContentHost.Children.Add(new TextBlock
        {
            Text = $"v{AppInfo.Version}  •  Windows Optimizer",
            FontSize = 12, FontWeight = FontWeights.SemiBold,
            Foreground = (Brush)FindResource("OrangeBrush"),
            HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 4, 0, 4),
        });
        ContentHost.Children.Add(new TextBlock
        {
            Text = _s["about_desc"], FontSize = 11,
            Foreground = (Brush)FindResource("MutedBrush"),
            HorizontalAlignment = HorizontalAlignment.Center,
            TextAlignment = TextAlignment.Center, Margin = new Thickness(0, 0, 0, 16),
        });

        AddAboutTile("👤", _s["about_dev"], AppInfo.Developer);
        AddAboutTile("📦", _s["about_ver"], "v" + AppInfo.Version);
        AddAboutTile("💬", _s["about_dc"], AppInfo.Discord);
        AddAboutTile("🔗", _s["about_gh"], "WinTweaker-by-koniaoo");
        AddAboutTile("🌐", _s["about_site"], "koniaoo-code.netlify.app");

        var bf = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 16, 0, 10) };
        var gh = MakeButton(_s["open_gh"], (Brush)FindResource("OrangeBrush"), 170);
        gh.Click += (_, _) => OpenUrl(AppInfo.GitHub);
        bf.Children.Add(gh);

        var site = MakeButton(_s["open_site"], new SolidColorBrush(Color.FromRgb(0x2A, 0x2A, 0x2A)), 160);
        site.Click += (_, _) => OpenUrl(AppInfo.Website);
        bf.Children.Add(site);

        _copyBtn = MakeButton(_s["copy_dc"], new SolidColorBrush(Color.FromRgb(0x58, 0x65, 0xF2)), 170);
        _copyBtn.Click += OnCopyDiscord;
        bf.Children.Add(_copyBtn);

        ContentHost.Children.Add(bf);
    }

    private void AddAboutTile(string icon, string label, string value)
    {
        var card = InfoCard();
        card.Margin = new Thickness(40, 3, 40, 3);
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var ic = new TextBlock { Text = icon, FontSize = 20, Margin = new Thickness(14, 0, 10, 0), VerticalAlignment = VerticalAlignment.Center };
        Grid.SetColumn(ic, 0); grid.Children.Add(ic);

        var sp = new StackPanel { Margin = new Thickness(0, 10, 10, 10), VerticalAlignment = VerticalAlignment.Center };
        sp.Children.Add(new TextBlock { Text = label, FontSize = 10, Foreground = (Brush)FindResource("MutedBrush") });
        sp.Children.Add(new TextBlock { Text = value, FontSize = 12, FontWeight = FontWeights.Bold, Foreground = (Brush)FindResource("PrimaryTextBrush"), TextWrapping = TextWrapping.Wrap });
        Grid.SetColumn(sp, 1); grid.Children.Add(sp);

        card.Child = grid;
        ContentHost.Children.Add(card);
    }

    private void OnCopyDiscord(object sender, RoutedEventArgs e)
    {
        try
        {
            Clipboard.SetText(AppInfo.Discord);
            if (_copyBtn != null) _copyBtn.Content = _s["copied"];
        }
        catch { /* ignore */ }
    }

    // ── UWP removal ──────────────────────────────────────
    private void BuildUwp()
    {
        ContentHost.Children.Add(new TextBlock
        {
            Text = "⚠  " + _s["uwp_warn"],
            FontSize = 11, Foreground = (Brush)FindResource("RedBrush"),
            TextWrapping = TextWrapping.Wrap, Margin = new Thickness(2, 6, 0, 8),
        });

        _uwpScanBtn = MakeButton(_s["uwp_scan"], (Brush)FindResource("OrangeBrush"), 0);
        _uwpScanBtn.Height = 42;
        _uwpScanBtn.HorizontalAlignment = HorizontalAlignment.Stretch;
        _uwpScanBtn.Margin = new Thickness(0, 0, 0, 10);
        _uwpScanBtn.Click += OnUwpScan;
        ContentHost.Children.Add(_uwpScanBtn);

        _uwpList = new StackPanel();
        ContentHost.Children.Add(_uwpList);
    }

    private async void OnUwpScan(object sender, RoutedEventArgs e)
    {
        if (_uwpScanBtn == null || _uwpList == null) return;
        _uwpScanBtn.Content = _s["uwp_scanning"];
        _uwpScanBtn.IsEnabled = false;
        _uwpList.Children.Clear();

        var apps = await UwpApps.ScanAsync();

        _uwpScanBtn.Content = _s["uwp_scan"];
        _uwpScanBtn.IsEnabled = true;
        if (_currentSec != "uwp") return;

        if (apps.Count == 0)
        {
            _uwpList.Children.Add(new TextBlock
            {
                Text = _s["uwp_none"], Foreground = (Brush)FindResource("SecBrush"),
                FontSize = 13, Margin = new Thickness(0, 16, 0, 0),
            });
            return;
        }

        _uwpList.Children.Add(new TextBlock
        {
            Text = _s["uwp_found"] + apps.Count,
            Foreground = (Brush)FindResource("MutedBrush"),
            FontSize = 11, Margin = new Thickness(0, 0, 0, 6),
        });
        foreach (var app in apps)
            _uwpList.Children.Add(MakeUwpCard(app));
    }

    private Border MakeUwpCard(UwpApp app)
    {
        var card = InfoCard();
        card.Margin = new Thickness(0, 0, 0, 3);

        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var sp = new StackPanel { Margin = new Thickness(14, 8, 8, 8) };
        sp.Children.Add(new TextBlock { Text = app.Display, FontWeight = FontWeights.SemiBold, FontSize = 12, Foreground = (Brush)FindResource("PrimaryTextBrush"), TextWrapping = TextWrapping.Wrap });
        sp.Children.Add(new TextBlock { Text = app.Name, FontSize = 10, Foreground = (Brush)FindResource("MutedBrush"), TextWrapping = TextWrapping.Wrap });
        Grid.SetColumn(sp, 0); grid.Children.Add(sp);

        var btn = MakeButton(_s["uwp_remove"], (Brush)FindResource("RedBrush"), 96);
        btn.Height = 30;
        btn.Margin = new Thickness(8, 8, 12, 8);
        btn.VerticalAlignment = VerticalAlignment.Center;
        btn.Click += async (_, _) => await RemoveUwp(app, btn);
        Grid.SetColumn(btn, 1); grid.Children.Add(btn);

        card.Child = grid;
        return card;
    }

    private async Task RemoveUwp(UwpApp app, Button btn)
    {
        btn.Content = "...";
        btn.IsEnabled = false;
        bool ok = await UwpApps.RemoveAsync(app);
        btn.Content = ok ? _s["uwp_removed"] : _s["uwp_err"];
        btn.Background = ok ? (Brush)FindResource("GreenBrush") : (Brush)FindResource("RedBrush");
        Log($"{app.Display}: {(ok ? _s["uwp_removed"] : _s["uwp_err"])}", ok);
    }

    // ── Shared UI factories ──────────────────────────────
    private Border InfoCard() => new()
    {
        Background = (Brush)FindResource("BgCardBrush"),
        CornerRadius = new CornerRadius(10),
        BorderThickness = new Thickness(1),
        BorderBrush = (Brush)FindResource("CardBorderBrush"),
        Margin = new Thickness(0, 0, 0, 4),
    };

    private Button MakeButton(string text, Brush bg, double width)
    {
        var b = new Button
        {
            Content = text,
            Style = (Style)FindResource("FlatButton"),
            Background = bg,
            Height = 42,
            FontSize = 13,
            Margin = new Thickness(6, 0, 6, 0),
        };
        if (width > 0) b.Width = width;
        return b;
    }

    // ── Admin state ──────────────────────────────────────
    private void UpdateAdmin()
    {
        bool admin = Settings.IsAdministrator();
        var col = admin ? (Brush)FindResource("GreenBrush") : (Brush)FindResource("RedBrush");
        string txt = admin ? _s["admin_ok"] : _s["admin_no"];

        AdminDot.Text = txt;
        AdminDot.Foreground = col;
        AdminStatus.Text = txt;          // always-on indicator (bottom-left)
        AdminStatus.Foreground = col;

        if (!admin) Log(_s["no_admin"], false);
    }

    // ── Reboot recommendation button ─────────────────────
    private void OnRebootClick(object sender, RoutedEventArgs e) => PromptReboot();

    // ── Easter egg #1: tap the sidebar logo ──────────────
    private void OnLogoClick(object sender, MouseButtonEventArgs e)
    {
        if (LogoBox.RenderTransform is ScaleTransform st)
        {
            var pulse = new DoubleAnimation(1, 1.18, TimeSpan.FromMilliseconds(110))
            {
                AutoReverse = true,
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            st.BeginAnimation(ScaleTransform.ScaleXProperty, pulse);
            st.BeginAnimation(ScaleTransform.ScaleYProperty, pulse);
        }

        if (++_logoClicks >= 5)
        {
            _logoClicks = 0;
            Log(_s["egg_logo"], true);
        }
    }

    // ── Log panel ────────────────────────────────────────
    private void Log(string msg, bool? ok)
    {
        if (!Dispatcher.CheckAccess())
        {
            Dispatcher.Invoke(() => Log(msg, ok));
            return;
        }
        string icon = ok == true ? "✓" : ok == false ? "✗" : "►";
        LogBox.AppendText($"[{DateTime.Now:HH:mm:ss}] {icon} {msg}\n");
        LogBox.ScrollToEnd();
    }

    private static string ShortOut(string outp)
    {
        outp = outp.Replace("\r", " ").Replace("\n", " ").Trim();
        return outp.Length > 80 ? outp[..80] : outp;
    }

    private void OnClearLog(object sender, RoutedEventArgs e) => LogBox.Clear();

    private void OnCloseLog(object sender, RoutedEventArgs e)
    {
        LogPanel.Visibility = Visibility.Collapsed;
        LogShowBtn.Visibility = Visibility.Visible;
    }

    private void OnShowLog(object sender, RoutedEventArgs e)
    {
        LogShowBtn.Visibility = Visibility.Collapsed;
        LogPanel.Visibility = Visibility.Visible;
    }

    // ── Language switch ──────────────────────────────────
    private void OnToggleLang(object sender, RoutedEventArgs e)
    {
        _lang = _lang == "ru" ? "en" : "ru";
        _settings.Lang = _lang;
        _settings.Save();

        _s = Strings.Get(_lang);
        _data = TweakData.Build(_lang, _settings);
        ApplyTexts();
        BuildNav();
        ShowSection(_currentSec);
        UpdateAdmin();
    }

    private static void OpenUrl(string url)
    {
        try { Process.Start(new ProcessStartInfo(url) { UseShellExecute = true }); }
        catch { /* ignore */ }
    }
}
