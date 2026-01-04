using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Soundboard.Models.Settings;

public partial class HotkeySetting : SettingItemBase
{
    [ObservableProperty]
    private HotkeyBinding? _value;

    [ObservableProperty]
    private bool _isLearning;

    public required Func<AppConfig, HotkeyBinding?> Getter { get; init; }
    public required Action<AppConfig, HotkeyBinding?> Setter { get; init; }

    public Action<HotkeyBinding?>? OnChanged { get; init; }
    public Func<HotkeyBinding, bool>? ValidateHotkey { get; init; }

    public string DisplayValue => IsLearning
        ? "Press a key..."
        : Value?.GetDisplayString() ?? "Not set";

    public string LearnButtonText => IsLearning ? "Cancel" : "Set";

    partial void OnValueChanged(HotkeyBinding? value)
    {
        OnPropertyChanged(nameof(DisplayValue));
        OnChanged?.Invoke(value);
    }

    partial void OnIsLearningChanged(bool value)
    {
        OnPropertyChanged(nameof(DisplayValue));
        OnPropertyChanged(nameof(LearnButtonText));
    }

    [RelayCommand]
    private void ToggleLearn()
    {
        IsLearning = !IsLearning;
    }

    [RelayCommand]
    private void Clear()
    {
        IsLearning = false;
        Value = null;
    }

    public bool TrySetHotkey(HotkeyBinding hotkey)
    {
        if (ValidateHotkey != null && !ValidateHotkey(hotkey))
        {
            return false;
        }

        Value = hotkey;
        IsLearning = false;
        return true;
    }

    public override void ApplyToConfig(AppConfig config)
    {
        Setter(config, Value);
    }

    public override void LoadFromConfig(AppConfig config)
    {
        Value = Getter(config);
    }
}
