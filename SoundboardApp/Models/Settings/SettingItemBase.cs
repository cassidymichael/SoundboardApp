using CommunityToolkit.Mvvm.ComponentModel;

namespace Soundboard.Models.Settings;

public abstract partial class SettingItemBase : ObservableObject
{
    public required string Key { get; init; }
    public required string Title { get; init; }
    public string? Description { get; init; }

    [ObservableProperty]
    private bool _isVisible = true;

    [ObservableProperty]
    private bool _isEnabled = true;

    public abstract void ApplyToConfig(AppConfig config);
    public abstract void LoadFromConfig(AppConfig config);
}
