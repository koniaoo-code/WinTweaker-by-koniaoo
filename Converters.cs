using System.Globalization;
using System.Windows.Data;

namespace WinTweaker.Helpers;

/// <summary>Current localized On/Off labels, refreshed on language change.</summary>
public static class Loc
{
    public static string On = "Вкл.";
    public static string Off = "Выкл.";
}

/// <summary>bool -> localized "Вкл."/"Выкл." for the switch state label.</summary>
public sealed class OnOffConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => (value is bool b && b) ? Loc.On : Loc.Off;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => Binding.DoNothing;
}
