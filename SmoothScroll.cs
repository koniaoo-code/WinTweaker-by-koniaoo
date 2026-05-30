using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;

namespace WinTweaker.Helpers;

/// <summary>
/// Smooth, controlled mouse-wheel scrolling for a ScrollViewer.
/// Fixes the "scrolls too fast / jumpy" feel by animating the offset
/// in small eased steps. Attach via SmoothScroll.Enabled="True" in XAML.
/// </summary>
public static class SmoothScroll
{
    // Pixels moved per wheel notch (smaller = slower).
    private const double StepPerNotch = 90.0;
    private const int DurationMs = 260;

    private static readonly DependencyProperty TargetProperty =
        DependencyProperty.RegisterAttached("Target", typeof(double), typeof(SmoothScroll),
            new PropertyMetadata(0.0));

    // Animatable proxy for ScrollViewer.VerticalOffset.
    private static readonly DependencyProperty OffsetProperty =
        DependencyProperty.RegisterAttached("Offset", typeof(double), typeof(SmoothScroll),
            new PropertyMetadata(0.0, OnOffsetChanged));

    public static readonly DependencyProperty EnabledProperty =
        DependencyProperty.RegisterAttached("Enabled", typeof(bool), typeof(SmoothScroll),
            new PropertyMetadata(false, OnEnabledChanged));

    public static void SetEnabled(DependencyObject o, bool v) => o.SetValue(EnabledProperty, v);
    public static bool GetEnabled(DependencyObject o) => (bool)o.GetValue(EnabledProperty);

    private static void OnEnabledChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
    {
        if (o is not ScrollViewer sv) return;
        if ((bool)e.NewValue)
            sv.PreviewMouseWheel += OnWheel;
        else
            sv.PreviewMouseWheel -= OnWheel;
    }

    private static void OnOffsetChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
    {
        if (o is ScrollViewer sv) sv.ScrollToVerticalOffset((double)e.NewValue);
    }

    private static void OnWheel(object sender, MouseWheelEventArgs e)
    {
        if (sender is not ScrollViewer sv) return;
        if (sv.ScrollableHeight <= 0) return;

        double current = sv.VerticalOffset;
        double target = (double)sv.GetValue(TargetProperty);

        // Re-sync the running target if the view moved by other means.
        if (Math.Abs(target - current) > sv.ViewportHeight) target = current;

        target -= Math.Sign(e.Delta) * StepPerNotch * (Math.Abs(e.Delta) / 120.0);
        target = Math.Max(0, Math.Min(sv.ScrollableHeight, target));
        sv.SetValue(TargetProperty, target);

        var anim = new DoubleAnimation(current, target, TimeSpan.FromMilliseconds(DurationMs))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };
        sv.BeginAnimation(OffsetProperty, anim);
        e.Handled = true;
    }

    /// <summary>Resets the internal target (call after navigating / ScrollToTop).</summary>
    public static void Reset(ScrollViewer sv)
    {
        sv.BeginAnimation(OffsetProperty, null);
        sv.SetValue(TargetProperty, 0.0);
    }
}
