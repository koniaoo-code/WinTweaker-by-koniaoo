using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Threading;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using Microsoft.Win32;
using WinTweaker.Data;
using WinTweaker.Helpers;
using WinTweaker.Models;
using WinTweaker.Services;

namespace WinTweaker;

public partial class MainWindow : Window
{
    private Settings _settings;
    private Strings _s;

    // Risky/irreversible tweaks that ask for confirmation before enabling.
    private static readonly HashSet<string> DangerousTweaks = new()
    {
        "meltdown", "remove_edge", "remove_onedrive", "disable_recall", "disable_copilot", "wu_disable",
    };
    private string _lang;
    private Dictionary<string, List<Tweak>> _data;
    private string _currentSec = "performance";
    private readonly Dictionary<string, Button> _navButtons = new();

    private Button? _copyBtn;
    private Button? _uwpScanBtn;
    private StackPanel? _uwpList;
    private Button? _startupScanBtn;
    private StackPanel? _startupList;
    private Button? _svcScanBtn;
    private StackPanel? _svcList;
    private Button? _appsScanBtn;
    private StackPanel? _appsList;
    private TextBlock? _updateStatus;
    private Button? _updateDownloadBtn;

    // PC Info: pre-warmed at startup + cached so opening the tab feels instant.
    private Task<Dictionary<string, string>>? _pcInfoTask;
    private Dictionary<string, string>? _pcInfoCache;

    private int _logoClicks;   // easter egg counter

    private ItemsControl? _currentItemsControl;   // for live search filtering
    private List<Tweak>? _currentTweakList;

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
        ShowSection("dashboard");
        UpdateAdmin();
        _ = CheckUpdatesAsync();
    }

    private async Task CheckUpdatesAsync()
    {
        var info = await Updates.CheckAsync();
        if (info.IsNewer && info.LatestTag != null)
            Log(_s["update_available"] + info.LatestTag, null);
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
        SearchHint.Text = _s["search_hint"];
        PresetsLbl.Text = _s["presets"];
        GamingBtn.Content = _s["preset_gaming"];
        PrivacyBtn.Content = _s["preset_privacy"];
        MinimalBtn.Content = _s["preset_minimal"];
        RestoreBtn.Content = _s["restore_point"];
        RevertBtn.Content = _s["revert_all"];
    }

    // ── Sidebar navigation ───────────────────────────────
    private void BuildNav()
    {
        NavPanel.Children.Clear();
        _navButtons.Clear();
        foreach (var sec in Strings.NavOrder)
        {
            string glyph = Strings.Icons.TryGetValue(sec, out var ic) ? ic : "";
            string name = _s.Sections.TryGetValue(sec, out var n) ? n : sec;

            var row = new StackPanel { Orientation = Orientation.Horizontal };
            row.Children.Add(new TextBlock
            {
                Text = glyph,
                FontFamily = new FontFamily("Segoe Fluent Icons, Segoe MDL2 Assets"),
                FontSize = 16,
                Width = 26,
                TextAlignment = TextAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
            });
            row.Children.Add(new TextBlock { Text = name, VerticalAlignment = VerticalAlignment.Center });

            var btn = new Button
            {
                Content = row,
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
        SecTitle.Text = SectionTitle(sec);
        if (SearchBox.Text.Length > 0) SearchBox.Text = "";   // reset search on navigation

        var grad = (Brush)FindResource("OrangeGradient");
        var sec2 = (Brush)FindResource("SecBrush");
        var white = (Brush)FindResource("PrimaryTextBrush");
        foreach (var (s, btn) in _navButtons)
        {
            bool on = s == sec;
            btn.Foreground = on ? white : sec2;        // active = gradient pill + white text
            btn.Background = on ? grad : Brushes.Transparent;
        }

        bool special = Strings.SpecialSections.Contains(sec);
        ApplySecBtn.Visibility = special ? Visibility.Collapsed : Visibility.Visible;
        SearchHost.Visibility = special ? Visibility.Collapsed : Visibility.Visible;

        _currentItemsControl = null;
        _currentTweakList = null;
        _dashTimer?.Stop();                 // stop live dashboard updates when leaving
        _scrollAnim?.Stop();
        ContentHost.Children.Clear();
        ContentScroll.ScrollToTop();

        switch (sec)
        {
            case "dashboard": BuildDashboard(); break;
            case "benchmark": BuildBenchmark(); break;
            case "pcinfo": BuildPcInfo(); break;
            case "about": BuildAbout(); break;
            case "uwp": BuildUwp(); break;
            case "startup": BuildStartup(); break;
            case "services": BuildServices(); break;
            case "apps": BuildApps(); break;
            default: BuildTweaks(sec); break;
        }
        FadeInContent();
    }

    // Smooth, eased wheel scroll on a single light timer (glides, low overhead).
    private double _scrollTarget;
    private DispatcherTimer? _scrollAnim;

    private void OnContentWheel(object sender, MouseWheelEventArgs e)
    {
        if (sender is not ScrollViewer sv) return;
        e.Handled = true;

        if (_scrollAnim == null || !_scrollAnim.IsEnabled)
            _scrollTarget = sv.VerticalOffset;     // resync when idle

        _scrollTarget -= Math.Sign(e.Delta) * 55;  // step per notch (smaller = slower)
        _scrollTarget = Math.Max(0, Math.Min(sv.ScrollableHeight, _scrollTarget));

        if (_scrollAnim == null)
        {
            _scrollAnim = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(15) };
            _scrollAnim.Tick += (_, _) =>
            {
                double cur = sv.VerticalOffset;
                double diff = _scrollTarget - cur;
                if (Math.Abs(diff) < 0.5) { sv.ScrollToVerticalOffset(_scrollTarget); _scrollAnim!.Stop(); return; }
                sv.ScrollToVerticalOffset(cur + diff * 0.2);   // ease toward target
            };
        }
        _scrollAnim.Start();
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
        _currentTweakList = _data.TryGetValue(sec, out var list) ? list : new List<Tweak>();
        _currentItemsControl = new ItemsControl
        {
            ItemTemplate = (DataTemplate)Resources["TweakCardTemplate"],
        };
        ContentHost.Children.Add(_currentItemsControl);
        ApplyFilter();
    }

    private void OnSearch(object sender, TextChangedEventArgs e)
    {
        string q = SearchBox.Text.Trim();
        if (SearchHint != null)
            SearchHint.Visibility = q.Length == 0 ? Visibility.Visible : Visibility.Collapsed;

        // Reflect search vs section in the title
        if (q.Length > 0)
            SecTitle.Text = _s["search_results"];
        else
            SecTitle.Text = SectionTitle(_currentSec);

        ApplyFilter();
    }

    // Search is GLOBAL: when there is a query, it matches tweaks from EVERY
    // section, not just the current one. Empty query -> current section only.
    private void ApplyFilter()
    {
        if (_currentItemsControl == null) return;
        string q = SearchBox.Text.Trim();
        if (q.Length == 0)
        {
            _currentItemsControl.ItemsSource = _currentTweakList;
            return;
        }
        _currentItemsControl.ItemsSource = _data.Values.SelectMany(x => x)
            .Where(t => t.Name.Contains(q, StringComparison.OrdinalIgnoreCase)
                     || t.Desc.Contains(q, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    private string SectionTitle(string sec)
        => _s.Sections.TryGetValue(sec, out var n) ? n : sec;

    private async void OnToggleClick(object sender, RoutedEventArgs e)
    {
        if (sender is not ToggleButton tb || tb.DataContext is not Tweak t) return;

        bool state = t.IsEnabled;
        if (state && DangerousTweaks.Contains(t.Id))
        {
            var ans = MessageBox.Show(this, t.Name + "\n\n" + _s["confirm_danger"],
                _s["confirm_title"], MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (ans != MessageBoxResult.Yes)
            {
                t.IsEnabled = false;                 // revert the toggle
                _settings.SetTweak(t.Id, false);
                _settings.Save();
                return;
            }
        }

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
    {
        var rp = MessageBox.Show(this, _s["rp_before_all"], _s["restore_point"],
            MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
        if (rp == MessageBoxResult.Cancel) return;
        if (rp == MessageBoxResult.Yes)
        {
            Log(_s["restore_creating"], null);
            bool ok = await SystemRestore.CreateAsync();
            Log(ok ? _s["restore_done"] : _s["restore_fail"], ok);
        }
        await ApplyBulk(_data.Values.SelectMany(x => x), _s["applied_all"]);
    }

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

    // ── Presets (one-click profiles) ─────────────────────
    private async void OnPreset(object sender, RoutedEventArgs e)
    {
        if (sender is Button b && b.Tag is string key && Presets.Sets.TryGetValue(key, out var ids))
        {
            var tweaks = _data.Values.SelectMany(x => x).Where(t => ids.Contains(t.Id)).ToList();
            await ApplyBulk(tweaks, _s["preset_applied"]);
        }
    }

    // ── Revert all enabled tweaks ────────────────────────
    private async void OnRevertAll(object sender, RoutedEventArgs e)
    {
        var enabled = _data.Values.SelectMany(x => x).Where(t => t.IsEnabled).ToList();
        if (enabled.Count == 0) { Log(_s["reverted"], true); return; }

        foreach (var t in enabled)
        {
            t.IsEnabled = false;                 // flip toggle OFF
            _settings.SetTweak(t.Id, false);
            Log($"↩ {t.Name}...", null);
            var (ok, outp) = await Task.Run(() => CommandRunner.Run(t.Disable));
            Log($"  {ShortOut(outp)}", ok);
        }
        _settings.Save();
        Log(_s["reverted"], true);
        ShowRebootButton();
        PromptReboot();
    }

    // ── Create a System Restore point ────────────────────
    private async void OnRestorePoint(object sender, RoutedEventArgs e)
    {
        RestoreBtn.IsEnabled = false;
        Log(_s["restore_creating"], null);
        bool ok = await SystemRestore.CreateAsync();
        Log(ok ? _s["restore_done"] : _s["restore_fail"], ok);
        RestoreBtn.IsEnabled = true;
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
        AddPcGroup(_s["pc_mobo"], (_s["l_mfr"], info["mobo_mfr"]), (_s["l_board"], info["mobo"]), (_s["l_bios"], info["bios"]));
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

        // Reliable in-app install (copies the app + makes shortcuts; no separate Setup.exe needed)
        var install = MakeButton(_s["install_app"], (Brush)FindResource("OrangeBrush"), 0);
        install.Height = 42;
        install.HorizontalAlignment = HorizontalAlignment.Stretch;
        install.Margin = new Thickness(60, 18, 60, 0);
        install.Click += (_, _) => Installer.Run(Environment.ProcessPath ?? "");
        ContentHost.Children.Add(install);

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

        // ── Profile export / import ──
        var pf = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 0, 0, 6) };
        var exp = MakeButton(_s["export_profile"], new SolidColorBrush(Color.FromRgb(0x2A, 0x2A, 0x2A)), 0);
        exp.Height = 32; exp.FontSize = 11; exp.Padding = new Thickness(14, 0, 14, 0); exp.Margin = new Thickness(0);
        exp.Click += OnExportProfile;
        var imp = MakeButton(_s["import_profile"], new SolidColorBrush(Color.FromRgb(0x2A, 0x2A, 0x2A)), 0);
        imp.Height = 32; imp.FontSize = 11; imp.Padding = new Thickness(14, 0, 14, 0); imp.Margin = new Thickness(8, 0, 0, 0);
        imp.Click += OnImportProfile;
        var thm = MakeButton(_s["theme_toggle"], new SolidColorBrush(Color.FromRgb(0x2A, 0x2A, 0x2A)), 0);
        thm.Height = 32; thm.FontSize = 11; thm.Padding = new Thickness(14, 0, 14, 0); thm.Margin = new Thickness(8, 0, 0, 0);
        thm.Click += (_, _) => App.ToggleTheme();
        pf.Children.Add(exp);
        pf.Children.Add(imp);
        pf.Children.Add(thm);
        ContentHost.Children.Add(pf);

        // ── Update checker ──
        var upRow = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 4, 0, 16) };
        _updateStatus = new TextBlock { FontSize = 12, Foreground = (Brush)FindResource("MutedBrush"), VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 10, 0) };
        var checkBtn = MakeButton(_s["check_updates"], new SolidColorBrush(Color.FromRgb(0x2A, 0x2A, 0x2A)), 0);
        checkBtn.Height = 30; checkBtn.FontSize = 11; checkBtn.Padding = new Thickness(14, 0, 14, 0); checkBtn.Margin = new Thickness(0);
        checkBtn.Click += async (_, _) => await DoUpdateCheck();
        _updateDownloadBtn = MakeButton(_s["update_download"], (Brush)FindResource("OrangeBrush"), 0);
        _updateDownloadBtn.Height = 30; _updateDownloadBtn.FontSize = 11; _updateDownloadBtn.Padding = new Thickness(14, 0, 14, 0); _updateDownloadBtn.Margin = new Thickness(8, 0, 0, 0);
        _updateDownloadBtn.Visibility = Visibility.Collapsed;
        _updateDownloadBtn.Click += (_, _) => OpenUrl(Updates.ReleasesPage);
        upRow.Children.Add(_updateStatus);
        upRow.Children.Add(checkBtn);
        upRow.Children.Add(_updateDownloadBtn);
        ContentHost.Children.Add(upRow);

        _ = DoUpdateCheck();
    }

    private async Task DoUpdateCheck()
    {
        if (_updateStatus == null) return;
        _updateStatus.Text = _s["checking_updates"];
        _updateStatus.Foreground = (Brush)FindResource("MutedBrush");
        if (_updateDownloadBtn != null) _updateDownloadBtn.Visibility = Visibility.Collapsed;

        var info = await Updates.CheckAsync();
        if (_currentSec != "about" || _updateStatus == null) return;   // navigated away

        if (info.LatestTag == null)
        {
            _updateStatus.Text = _s["update_offline"];
            _updateStatus.Foreground = (Brush)FindResource("MutedBrush");
        }
        else if (info.IsNewer)
        {
            _updateStatus.Text = _s["update_available"] + info.LatestTag;
            _updateStatus.Foreground = (Brush)FindResource("OrangeBrush");
            if (_updateDownloadBtn != null) _updateDownloadBtn.Visibility = Visibility.Visible;
        }
        else
        {
            _updateStatus.Text = _s["update_latest"];
            _updateStatus.Foreground = (Brush)FindResource("GreenBrush");
        }
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

    // ── Profile export / import ──────────────────────────
    private void OnExportProfile(object sender, RoutedEventArgs e)
    {
        var dlg = new SaveFileDialog { FileName = "wintweaker_profile.json", Filter = "JSON|*.json", DefaultExt = ".json" };
        if (dlg.ShowDialog() == true)
        {
            try { Settings.ExportTo(dlg.FileName); Log(_s["profile_exported"], true); }
            catch (Exception ex) { Log(ex.Message, false); }
        }
    }

    private void OnImportProfile(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFileDialog { Filter = "JSON|*.json" };
        if (dlg.ShowDialog() != true) return;
        try
        {
            Settings.ImportFrom(dlg.FileName);
            _settings = Settings.Load();
            _lang = _settings.Lang;
            _s = Strings.Get(_lang);
            _data = TweakData.Build(_lang, _settings);
            ApplyTexts();
            BuildNav();
            ShowSection(_currentSec);
            UpdateAdmin();
            Log(_s["profile_imported"], true);
        }
        catch (Exception ex) { Log(ex.Message, false); }
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

    // ── Startup manager ──────────────────────────────────
    private void BuildStartup()
    {
        _startupScanBtn = MakeButton(_s["startup_scan"], (Brush)FindResource("OrangeGradient"), 0);
        _startupScanBtn.Height = 42; _startupScanBtn.HorizontalAlignment = HorizontalAlignment.Stretch;
        _startupScanBtn.Margin = new Thickness(0, 0, 0, 10);
        _startupScanBtn.Click += OnStartupScan;
        ContentHost.Children.Add(_startupScanBtn);
        _startupList = new StackPanel();
        ContentHost.Children.Add(_startupList);
    }

    private async void OnStartupScan(object sender, RoutedEventArgs e)
    {
        if (_startupScanBtn == null || _startupList == null) return;
        _startupScanBtn.Content = _s["uwp_scanning"]; _startupScanBtn.IsEnabled = false;
        _startupList.Children.Clear();
        var items = await StartupApps.ScanAsync();
        _startupScanBtn.Content = _s["startup_scan"]; _startupScanBtn.IsEnabled = true;
        if (_currentSec != "startup") return;
        if (items.Count == 0) { _startupList.Children.Add(NoteLabel(_s["startup_none"])); return; }
        _startupList.Children.Add(FoundLabel(_s["found"] + items.Count));
        foreach (var it in items)
        {
            var card = TwoLineToggleCard(it.Name, it.Command, it.Enabled, async on =>
            {
                await StartupApps.SetEnabledAsync(it, on);
                Log((on ? _s["enable"] : _s["disable"]) + ": " + it.Name, true);
            });
            _startupList.Children.Add(card);
        }
    }

    // ── Services manager ─────────────────────────────────
    private void BuildServices()
    {
        _svcScanBtn = MakeButton(_s["svc_scan"], (Brush)FindResource("OrangeGradient"), 0);
        _svcScanBtn.Height = 42; _svcScanBtn.HorizontalAlignment = HorizontalAlignment.Stretch;
        _svcScanBtn.Margin = new Thickness(0, 0, 0, 10);
        _svcScanBtn.Click += OnServicesScan;
        ContentHost.Children.Add(_svcScanBtn);
        _svcList = new StackPanel();
        ContentHost.Children.Add(_svcList);
    }

    private async void OnServicesScan(object sender, RoutedEventArgs e)
    {
        if (_svcScanBtn == null || _svcList == null) return;
        _svcScanBtn.Content = _s["uwp_scanning"]; _svcScanBtn.IsEnabled = false;
        _svcList.Children.Clear();
        var items = await WinServices.ScanAsync();
        _svcScanBtn.Content = _s["svc_scan"]; _svcScanBtn.IsEnabled = true;
        if (_currentSec != "services") return;
        _svcList.Children.Add(FoundLabel(_s["found"] + items.Count));
        foreach (var it in items)
        {
            var card = TwoLineToggleCard(it.Title, it.Desc, it.Running, async on =>
            {
                await WinServices.SetAsync(it.Name, on);
                Log((on ? _s["enable"] : _s["disable"]) + ": " + it.Title, true);
            });
            _svcList.Children.Add(card);
        }
    }

    // ── Uninstall programs ───────────────────────────────
    private void BuildApps()
    {
        _appsScanBtn = MakeButton(_s["apps_scan"], (Brush)FindResource("OrangeGradient"), 0);
        _appsScanBtn.Height = 42; _appsScanBtn.HorizontalAlignment = HorizontalAlignment.Stretch;
        _appsScanBtn.Margin = new Thickness(0, 0, 0, 10);
        _appsScanBtn.Click += OnAppsScan;
        ContentHost.Children.Add(_appsScanBtn);
        _appsList = new StackPanel();
        ContentHost.Children.Add(_appsList);
    }

    private async void OnAppsScan(object sender, RoutedEventArgs e)
    {
        if (_appsScanBtn == null || _appsList == null) return;
        _appsScanBtn.Content = _s["uwp_scanning"]; _appsScanBtn.IsEnabled = false;
        _appsList.Children.Clear();
        var items = await InstalledApps.ScanAsync();
        _appsScanBtn.Content = _s["apps_scan"]; _appsScanBtn.IsEnabled = true;
        if (_currentSec != "apps") return;
        if (items.Count == 0) { _appsList.Children.Add(NoteLabel(_s["apps_none"])); return; }
        _appsList.Children.Add(FoundLabel(_s["found"] + items.Count));
        foreach (var app in items)
        {
            var card = InfoCard(); card.Margin = new Thickness(0, 0, 0, 3);
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            var sp = new StackPanel { Margin = new Thickness(14, 8, 8, 8) };
            sp.Children.Add(new TextBlock { Text = app.Name, FontWeight = FontWeights.SemiBold, FontSize = 12, Foreground = (Brush)FindResource("PrimaryTextBrush"), TextWrapping = TextWrapping.Wrap });
            if (app.Publisher.Length > 0)
                sp.Children.Add(new TextBlock { Text = app.Publisher, FontSize = 10, Foreground = (Brush)FindResource("MutedBrush"), TextWrapping = TextWrapping.Wrap });
            Grid.SetColumn(sp, 0); grid.Children.Add(sp);
            var btn = MakeButton(_s["apps_uninstall"], (Brush)FindResource("RedBrush"), 96);
            btn.Height = 30; btn.Margin = new Thickness(8, 8, 12, 8); btn.VerticalAlignment = VerticalAlignment.Center;
            btn.Click += async (_, _) =>
            {
                btn.Content = "..."; btn.IsEnabled = false;
                bool ok = await InstalledApps.UninstallAsync(app);
                btn.Content = ok ? _s["uwp_removed"] : _s["uwp_err"];
                btn.Background = ok ? (Brush)FindResource("GreenBrush") : (Brush)FindResource("RedBrush");
                Log($"{app.Name}: {(ok ? _s["uwp_removed"] : _s["uwp_err"])}", ok);
            };
            Grid.SetColumn(btn, 1); grid.Children.Add(btn);
            card.Child = grid;
            _appsList.Children.Add(card);
        }
    }

    // Card with title + subtitle + an on/off Switch toggle.
    private Border TwoLineToggleCard(string title, string subtitle, bool on, Func<bool, Task> onToggle)
    {
        var card = InfoCard(); card.Margin = new Thickness(0, 0, 0, 3);
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        var sp = new StackPanel { Margin = new Thickness(14, 10, 8, 10) };
        sp.Children.Add(new TextBlock { Text = title, FontWeight = FontWeights.SemiBold, FontSize = 12, Foreground = (Brush)FindResource("PrimaryTextBrush"), TextWrapping = TextWrapping.Wrap });
        if (!string.IsNullOrEmpty(subtitle))
            sp.Children.Add(new TextBlock { Text = subtitle, FontSize = 10, Foreground = (Brush)FindResource("MutedBrush"), TextWrapping = TextWrapping.Wrap });
        Grid.SetColumn(sp, 0); grid.Children.Add(sp);
        var tg = new ToggleButton { Style = (Style)FindResource("Switch"), IsChecked = on, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(8, 0, 14, 0) };
        tg.Click += async (_, _) => { try { await onToggle(tg.IsChecked == true); } catch { /* ignore */ } };
        Grid.SetColumn(tg, 1); grid.Children.Add(tg);
        card.Child = grid;
        return card;
    }

    private TextBlock NoteLabel(string text) => new()
    {
        Text = text, Foreground = (Brush)FindResource("SecBrush"),
        FontSize = 13, Margin = new Thickness(0, 16, 0, 0),
    };

    private TextBlock FoundLabel(string text) => new()
    {
        Text = text, Foreground = (Brush)FindResource("MutedBrush"),
        FontSize = 11, Margin = new Thickness(0, 0, 0, 6),
    };

    // ── Dashboard (health score + live monitoring + quick actions) ──
    private DispatcherTimer? _dashTimer;
    private TextBlock? _dashRam, _dashProc, _dashScore;

    private void BuildDashboard()
    {
        var scoreCard = InfoCard(); scoreCard.Margin = new Thickness(0, 4, 0, 10);
        var ssp = new StackPanel { Margin = new Thickness(20, 18, 20, 18), HorizontalAlignment = HorizontalAlignment.Center };
        _dashScore = new TextBlock { Text = "—", FontSize = 48, FontWeight = FontWeights.Bold, HorizontalAlignment = HorizontalAlignment.Center, Foreground = (Brush)FindResource("GreenBrush") };
        ssp.Children.Add(_dashScore);
        ssp.Children.Add(new TextBlock { Text = _s["dash_health"], FontSize = 13, Foreground = (Brush)FindResource("SecBrush"), HorizontalAlignment = HorizontalAlignment.Center });
        scoreCard.Child = ssp;
        ContentHost.Children.Add(scoreCard);

        var card = InfoCard();
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(160) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        _dashRam = AddDashRow(grid, 0, _s["dash_ram"]);
        _dashProc = AddDashRow(grid, 1, _s["dash_proc"]);
        AddDashRowStatic(grid, 2, _s["dash_disk"], DiskFreeText());
        AddDashRowStatic(grid, 3, _s["dash_uptime"], UptimeText());
        AddDashRowStatic(grid, 4, _s["dash_applied"], AppliedCount().ToString());
        card.Child = grid;
        ContentHost.Children.Add(card);

        ContentHost.Children.Add(new TextBlock { Text = _s["dash_quick"], FontWeight = FontWeights.Bold, FontSize = 12, Foreground = (Brush)FindResource("OrangeBrush"), Margin = new Thickness(2, 14, 0, 6) });

        var qa1 = MakeButton(_s["qa_minimal"], (Brush)FindResource("OrangeGradient"), 0);
        qa1.HorizontalAlignment = HorizontalAlignment.Stretch; qa1.Margin = new Thickness(0, 0, 0, 6);
        qa1.Click += async (_, _) =>
        {
            var tw = _data.Values.SelectMany(x => x).Where(t => Presets.Sets["minimal"].Contains(t.Id)).ToList();
            await ApplyBulk(tw, _s["preset_applied"]);
        };
        ContentHost.Children.Add(qa1);

        var qa2 = MakeButton(_s["qa_restore"], new SolidColorBrush(Color.FromRgb(0x14, 0x30, 0x4A)), 0);
        qa2.HorizontalAlignment = HorizontalAlignment.Stretch; qa2.Margin = new Thickness(0, 0, 0, 6);
        qa2.Click += async (_, _) =>
        {
            Log(_s["restore_creating"], null);
            bool ok = await SystemRestore.CreateAsync();
            Log(ok ? _s["restore_done"] : _s["restore_fail"], ok);
        };
        ContentHost.Children.Add(qa2);

        var qa3 = MakeButton(_s["qa_clean"], new SolidColorBrush(Color.FromRgb(0x2A, 0x2A, 0x2A)), 0);
        qa3.HorizontalAlignment = HorizontalAlignment.Stretch;
        qa3.Click += async (_, _) =>
        {
            Log(_s["qa_clean"], null);
            var (ok, _) = await Task.Run(() => CommandRunner.Run(@"cmd /c ""del /q /f /s %TEMP%\* 2>nul"""));
            Log("Temp", ok);
        };
        ContentHost.Children.Add(qa3);

        UpdateDashLive();
        _dashTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
        _dashTimer.Tick += (_, _) => UpdateDashLive();
        _dashTimer.Start();
    }

    private void UpdateDashLive()
    {
        var (load, _, _) = Native.Memory();
        int proc = Process.GetProcesses().Length;
        int disk = DiskFreePercent();
        if (_dashRam != null) _dashRam.Text = load + " %";
        if (_dashProc != null) _dashProc.Text = proc.ToString();
        if (_dashScore != null)
        {
            int score = ComputeScore(load, disk, proc);
            _dashScore.Text = score.ToString();
            _dashScore.Foreground = score >= 75 ? (Brush)FindResource("GreenBrush")
                : score >= 50 ? (Brush)FindResource("OrangeBrush") : (Brush)FindResource("RedBrush");
        }
    }

    private static int ComputeScore(int ramLoad, int diskFree, int proc)
    {
        int s = 100;
        if (ramLoad > 85) s -= 15; else if (ramLoad > 70) s -= 8;
        if (diskFree < 10) s -= 15; else if (diskFree < 20) s -= 8;
        if (proc > 250) s -= 10; else if (proc > 180) s -= 5;
        return Math.Clamp(s, 0, 100);
    }

    private static int DiskFreePercent()
    {
        try { var d = new DriveInfo("C"); return (int)(d.AvailableFreeSpace * 100.0 / d.TotalSize); }
        catch { return 100; }
    }

    private static string DiskFreeText()
    {
        try { var d = new DriveInfo("C"); return $"{DiskFreePercent()} %  ({d.AvailableFreeSpace / 1073741824.0:0.0} ГБ)"; }
        catch { return "—"; }
    }

    private static string UptimeText()
    {
        var u = TimeSpan.FromMilliseconds(Environment.TickCount64);
        return $"{u.Days}d {u.Hours}h {u.Minutes}m";
    }

    private int AppliedCount() => _settings.Tweaks.Count(kv => kv.Value);

    private TextBlock AddDashRow(Grid g, int row, string label)
    {
        g.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        var k = new TextBlock { Text = label, FontSize = 12, Foreground = (Brush)FindResource("MutedBrush"), Margin = new Thickness(14, 7, 6, 7) };
        Grid.SetRow(k, row); Grid.SetColumn(k, 0); g.Children.Add(k);
        var v = new TextBlock { Text = "—", FontSize = 13, FontWeight = FontWeights.Bold, Foreground = (Brush)FindResource("PrimaryTextBrush"), Margin = new Thickness(6, 7, 12, 7) };
        Grid.SetRow(v, row); Grid.SetColumn(v, 1); g.Children.Add(v);
        return v;
    }

    private void AddDashRowStatic(Grid g, int row, string label, string value)
    {
        var v = AddDashRow(g, row, label);
        v.Text = value;
    }

    // ── CPU Benchmark ────────────────────────────────────
    private Button? _benchBtn;
    private StackPanel? _benchResult;

    private void BuildBenchmark()
    {
        ContentHost.Children.Add(new TextBlock
        {
            Text = _s["bench_desc"], FontSize = 13, Foreground = (Brush)FindResource("SecBrush"),
            TextWrapping = TextWrapping.Wrap, Margin = new Thickness(2, 6, 0, 12),
        });
        _benchBtn = MakeButton(_s["bench_run"], (Brush)FindResource("OrangeGradient"), 0);
        _benchBtn.Height = 44; _benchBtn.HorizontalAlignment = HorizontalAlignment.Stretch;
        _benchBtn.Margin = new Thickness(0, 0, 0, 12);
        _benchBtn.Click += OnRunBenchmark;
        ContentHost.Children.Add(_benchBtn);
        _benchResult = new StackPanel();
        ContentHost.Children.Add(_benchResult);
    }

    private async void OnRunBenchmark(object sender, RoutedEventArgs e)
    {
        if (_benchBtn == null || _benchResult == null) return;
        _benchBtn.Content = _s["bench_running"]; _benchBtn.IsEnabled = false;
        _benchResult.Children.Clear();
        var (single, multi) = await Benchmark.RunAsync();
        _benchBtn.Content = _s["bench_run"]; _benchBtn.IsEnabled = true;
        if (_currentSec != "benchmark") return;
        _benchResult.Children.Add(BenchCard(_s["bench_single"], single));
        _benchResult.Children.Add(BenchCard(_s["bench_multi"], multi));
    }

    private Border BenchCard(string label, long score)
    {
        var card = InfoCard();
        var sp = new StackPanel { Margin = new Thickness(18, 14, 18, 14), HorizontalAlignment = HorizontalAlignment.Center };
        sp.Children.Add(new TextBlock { Text = score.ToString(), FontSize = 34, FontWeight = FontWeights.Bold, Foreground = (Brush)FindResource("OrangeBrush"), HorizontalAlignment = HorizontalAlignment.Center });
        sp.Children.Add(new TextBlock { Text = label + " · " + _s["bench_score"], FontSize = 12, Foreground = (Brush)FindResource("SecBrush"), HorizontalAlignment = HorizontalAlignment.Center });
        card.Child = sp;
        return card;
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
