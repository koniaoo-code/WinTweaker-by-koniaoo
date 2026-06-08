using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace WinTweaker.Models;

/// <summary>
/// A single reversible tweak. <see cref="Enable"/> / <see cref="Disable"/> are
/// shell command strings (cmd.exe). <see cref="IsEnabled"/> is pure UI state —
/// running the command is driven by the window, not the setter.
/// </summary>
public sealed class Tweak : INotifyPropertyChanged
{
    public string Id { get; init; } = "";
    public string Name { get; set; } = "";
    public string Desc { get; set; } = "";
    public string Enable { get; init; } = "";
    public string Disable { get; init; } = "";

    private bool _isEnabled;
    public bool IsEnabled
    {
        get => _isEnabled;
        set
        {
            if (_isEnabled == value) return;
            _isEnabled = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsEnabled)));
        }
    }

    /// <summary>Set state without raising change notification noise unnecessarily.</summary>
    public void SetEnabledSilent(bool value) => IsEnabled = value;

    public event PropertyChangedEventHandler? PropertyChanged;
}
