using CommunityToolkit.Mvvm.ComponentModel;

namespace Soundboard.Models.Settings;

public partial class ToggleSetting : SettingItemBase
{
    [ObservableProperty]
    private bool _value;

    public required Func<AppConfig, bool> Getter { get; init; }
    public required Action<AppConfig, bool> Setter { get; init; }

    public Action<bool>? OnChanged { get; init; }

    partial void OnValueChanged(bool value)
    {
        OnChanged?.Invoke(value);
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
