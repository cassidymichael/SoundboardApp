using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Win32;
using Soundboard.Services.Interfaces;

namespace Soundboard.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly IConfigService _configService;
    private const string StartupRegistryKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
    private const string AppName = "Soundboard";

    [ObservableProperty]
    private bool _startWithWindows;

    [ObservableProperty]
    private bool _startMinimized;

    [ObservableProperty]
    private bool _closeToTray;

    public SettingsViewModel(IConfigService configService)
    {
        _configService = configService;

        // Load current settings
        StartWithWindows = _configService.Config.StartWithWindows;
        StartMinimized = _configService.Config.StartMinimized;
        CloseToTray = _configService.Config.CloseToTray;
    }

    partial void OnStartWithWindowsChanged(bool value)
    {
        _configService.Config.StartWithWindows = value;
        UpdateWindowsStartupRegistry(value);
        _ = _configService.SaveAsync();
    }

    partial void OnStartMinimizedChanged(bool value)
    {
        _configService.Config.StartMinimized = value;
        _ = _configService.SaveAsync();
    }

    partial void OnCloseToTrayChanged(bool value)
    {
        _configService.Config.CloseToTray = value;
        _ = _configService.SaveAsync();
    }

    private void UpdateWindowsStartupRegistry(bool enable)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(StartupRegistryKey, writable: true);
            if (key == null) return;

            if (enable)
            {
                var exePath = Environment.ProcessPath;
                if (!string.IsNullOrEmpty(exePath))
                {
                    key.SetValue(AppName, $"\"{exePath}\"");
                }
            }
            else
            {
                key.DeleteValue(AppName, throwOnMissingValue: false);
            }
        }
        catch
        {
            // Silently fail if registry access fails
        }
    }
}
