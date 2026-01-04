using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Soundboard.Models.Settings;

public partial class ActionSetting : SettingItemBase
{
    public required string ButtonText { get; init; }

    public required Func<Task> Action { get; init; }

    public string? ConfirmationMessage { get; init; }
    public string? ConfirmationTitle { get; init; }

    [ObservableProperty]
    private bool _isExecuting;

    [RelayCommand]
    private async Task ExecuteAsync()
    {
        if (IsExecuting) return;

        try
        {
            IsExecuting = true;
            await Action();
        }
        finally
        {
            IsExecuting = false;
        }
    }

    public override void ApplyToConfig(AppConfig config)
    {
        // Actions don't persist to config
    }

    public override void LoadFromConfig(AppConfig config)
    {
        // Actions don't load from config
    }
}
