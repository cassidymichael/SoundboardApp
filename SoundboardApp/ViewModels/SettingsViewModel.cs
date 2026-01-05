using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using Soundboard.Models;
using Soundboard.Models.Settings;
using Soundboard.Services.Interfaces;
using System.Collections.ObjectModel;
using System.Windows;

namespace Soundboard.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly IConfigService _configService;
    private readonly MainViewModel _mainViewModel;
    private const string StartupRegistryKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
    private const string AppName = "Soundboard";

    public ObservableCollection<SettingsCategory> Categories { get; } = new();

    [ObservableProperty]
    private SettingsCategory? _selectedCategory;

    // Grid layout settings (stored for resize confirmation)
    private ChoiceSetting<int>? _gridColumnsSetting;
    private ChoiceSetting<int>? _gridRowsSetting;

    // Callbacks for window management
    public Action<bool>? RequestClose { get; set; }
    public Action? OnFactoryReset { get; set; }
    public Func<Window>? GetOwnerWindow { get; set; }

    public SettingsViewModel(IConfigService configService, MainViewModel mainViewModel)
    {
        _configService = configService;
        _mainViewModel = mainViewModel;
        BuildCategories();
        LoadAllSettings();
        InitializeSettingDependencies();

        // Select the first category by default
        SelectedCategory = Categories.FirstOrDefault();
    }

    private void InitializeSettingDependencies()
    {
        // Initialize "Start in background" enabled state based on "Keep running in background"
        var general = Categories.FirstOrDefault(c => c.Name == "General");
        if (general == null) return;

        var keepRunning = general.Settings.OfType<ToggleSetting>().FirstOrDefault(s => s.Key == "CloseToTray");
        var startInBackground = general.Settings.OfType<ToggleSetting>().FirstOrDefault(s => s.Key == "StartMinimized");

        if (keepRunning != null && startInBackground != null)
        {
            startInBackground.IsEnabled = keepRunning.Value;
        }
    }

    private void BuildCategories()
    {
        // General category
        var general = new SettingsCategory { Name = "General" };

        var keepRunning = new ToggleSetting
        {
            Key = "CloseToTray",
            Title = "Keep running in background",
            Description = "Minimize to system tray instead of quitting when you close the window",
            Getter = c => c.CloseToTray,
            Setter = (c, v) => c.CloseToTray = v
        };

        var startInBackground = new ToggleSetting
        {
            Key = "StartMinimized",
            Title = "Start in background",
            Description = "Start hidden in the system tray",
            Getter = c => c.StartMinimized,
            Setter = (c, v) => c.StartMinimized = v
        };

        // When "Keep running in background" is disabled, disable and turn off "Start in background"
        keepRunning.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(ToggleSetting.Value))
            {
                startInBackground.IsEnabled = keepRunning.Value;
                if (!keepRunning.Value)
                {
                    startInBackground.Value = false;
                }
            }
        };

        general.Settings.Add(keepRunning);
        general.Settings.Add(startInBackground);
        general.Settings.Add(new ToggleSetting
        {
            Key = "StartWithWindows",
            Title = "Start with Windows",
            Description = "Launch Soundboard when you log in",
            Getter = c => c.StartWithWindows,
            Setter = (c, v) => c.StartWithWindows = v
        });

        Categories.Add(general);

        // Grid Layout category
        var gridLayout = new SettingsCategory { Name = "Grid Layout" };

        // Options for 1-8 columns/rows
        var gridSizeOptions = Enumerable.Range(1, 8)
            .Select(i => new ChoiceOption<int>(i, i.ToString()))
            .ToList();

        _gridColumnsSetting = new ChoiceSetting<int>
        {
            Key = "GridColumns",
            Title = "Columns",
            Description = "Number of columns in the tile grid",
            Getter = c => c.GridColumns,
            Setter = (c, v) => { } // Handled in SaveAsync
        };
        foreach (var opt in gridSizeOptions) _gridColumnsSetting.Options.Add(opt);

        _gridRowsSetting = new ChoiceSetting<int>
        {
            Key = "GridRows",
            Title = "Rows",
            Description = "Number of rows in the tile grid",
            Getter = c => c.GridRows,
            Setter = (c, v) => { } // Handled in SaveAsync
        };
        foreach (var opt in gridSizeOptions) _gridRowsSetting.Options.Add(opt);

        gridLayout.Settings.Add(_gridColumnsSetting);
        gridLayout.Settings.Add(_gridRowsSetting);
        Categories.Add(gridLayout);

        // Advanced category
        var advanced = new SettingsCategory { Name = "Advanced" };
        advanced.Settings.Add(new ActionSetting
        {
            Key = "ExportConfig",
            Title = "Export configuration",
            Description = "Save all settings and tile configurations to a file for backup or transfer to another computer.",
            ButtonText = "Export",
            Action = ExportConfigAsync
        });
        advanced.Settings.Add(new ActionSetting
        {
            Key = "ImportConfig",
            Title = "Import configuration",
            Description = "Load settings and tile configurations from a previously exported file. The app will restart to apply changes.",
            ButtonText = "Import",
            Action = ImportConfigAsync
        });
        advanced.Settings.Add(new ActionSetting
        {
            Key = "ResetToDefaults",
            Title = "Reset to defaults",
            Description = "Reset all settings to their default values. Your sounds and hotkeys will be preserved.",
            ButtonText = "Reset Settings",
            Action = ResetToDefaultsAsync
        });
        advanced.Settings.Add(new ActionSetting
        {
            Key = "FactoryReset",
            Title = "Factory reset",
            Description = "Clear all tile configurations, hotkeys, and settings. Your sound files will not be deleted.",
            ButtonText = "Factory Reset",
            Action = FactoryResetAsync
        });
        Categories.Add(advanced);
    }

    private void LoadAllSettings()
    {
        foreach (var category in Categories)
        {
            foreach (var setting in category.Settings)
            {
                setting.LoadFromConfig(_configService.Config);
            }
        }
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        // Check for grid resize before applying
        if (_gridColumnsSetting != null && _gridRowsSetting != null)
        {
            int newColumns = _gridColumnsSetting.SelectedValue;
            int newRows = _gridRowsSetting.SelectedValue;
            int currentColumns = _configService.Config.GridColumns;
            int currentRows = _configService.Config.GridRows;

            // Check if grid dimensions changed
            if (newColumns != currentColumns || newRows != currentRows)
            {
                var affectedTiles = _mainViewModel.GetTilesAffectedByResize(newColumns, newRows);

                if (affectedTiles.Count > 0)
                {
                    // Show confirmation dialog
                    var owner = GetOwnerWindow?.Invoke();
                    if (owner == null)
                    {
                        // Cannot show dialog - restore values and abort
                        _gridColumnsSetting.SelectedValue = currentColumns;
                        _gridRowsSetting.SelectedValue = currentRows;
                        return;
                    }

                    var tileNames = string.Join("\n", affectedTiles.Select(t => $"  - {t.Name}"));
                    var confirmed = Views.ConfirmDialog.Show(
                        owner,
                        "Resize Grid",
                        $"This will remove {affectedTiles.Count} tile(s) with sounds assigned:",
                        tileNames,
                        confirmText: "Remove Tiles",
                        cancelText: "Cancel",
                        isDangerous: true);

                    if (!confirmed)
                    {
                        // Restore original values
                        _gridColumnsSetting.SelectedValue = currentColumns;
                        _gridRowsSetting.SelectedValue = currentRows;
                        return;
                    }
                }

                // Apply grid resize
                await _mainViewModel.ApplyGridResize(newColumns, newRows);
            }
        }

        // Apply all other settings to config
        foreach (var category in Categories)
        {
            foreach (var setting in category.Settings)
            {
                // Skip grid settings - already handled above
                if (setting.Key == "GridColumns" || setting.Key == "GridRows")
                    continue;

                setting.ApplyToConfig(_configService.Config);
            }
        }

        // Handle special case: StartWithWindows needs registry update
        UpdateWindowsStartupRegistry(_configService.Config.StartWithWindows);

        await _configService.SaveAsync();
        RequestClose?.Invoke(true);
    }

    [RelayCommand]
    private void Cancel()
    {
        RequestClose?.Invoke(false);
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

    private async Task ExportConfigAsync()
    {
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Title = "Export Configuration",
            Filter = "Soundboard Config (*.json)|*.json",
            FileName = "soundboard-config.json",
            DefaultExt = ".json"
        };

        if (dialog.ShowDialog() == true)
        {
            try
            {
                await _configService.ExportConfigAsync(dialog.FileName);
                var owner = GetOwnerWindow?.Invoke();
                if (owner != null)
                {
                    Views.ConfirmDialog.ShowInfo(owner, "Export Complete", "Configuration has been exported successfully.");
                }
            }
            catch
            {
                var owner = GetOwnerWindow?.Invoke();
                if (owner != null)
                {
                    Views.ConfirmDialog.ShowInfo(owner, "Export Failed", "Failed to export configuration. Please check the file path and try again.");
                }
            }
        }
    }

    private async Task ImportConfigAsync()
    {
        var owner = GetOwnerWindow?.Invoke();
        if (owner == null) return;

        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "Import Configuration",
            Filter = "Soundboard Config (*.json)|*.json",
            DefaultExt = ".json"
        };

        if (dialog.ShowDialog() != true) return;

        var confirmed = Views.ConfirmDialog.Show(
            owner,
            "Import Configuration",
            "Are you sure you want to import this configuration?",
            "This will replace all your current settings and tiles.\nThe app will restart to apply changes.",
            confirmText: "Import",
            cancelText: "Cancel",
            isDangerous: true);

        if (!confirmed) return;

        var success = await _configService.ImportConfigAsync(dialog.FileName);

        if (success)
        {
            Views.ConfirmDialog.ShowInfo(
                owner,
                "Restart Required",
                "Configuration imported successfully. The application needs to restart to apply changes.");

            OnFactoryReset?.Invoke();
        }
        else
        {
            Views.ConfirmDialog.ShowInfo(
                owner,
                "Import Failed",
                "The selected file is not a valid Soundboard configuration file.");
        }
    }

    private async Task ResetToDefaultsAsync()
    {
        var owner = GetOwnerWindow?.Invoke();
        if (owner == null) return;

        var confirmed = Views.ConfirmDialog.Show(
            owner,
            "Reset Settings",
            "Are you sure you want to reset all settings to their defaults?",
            "Your sounds and hotkeys will be preserved.",
            confirmText: "Reset",
            cancelText: "Cancel",
            isDangerous: false);

        if (!confirmed) return;

        // Create default config and copy only settings (not tiles)
        var defaultConfig = AppConfig.CreateDefault();

        // Reset device settings
        _configService.Config.MonitorDeviceId = defaultConfig.MonitorDeviceId;
        _configService.Config.InjectDeviceId = defaultConfig.InjectDeviceId;
        _configService.Config.MonitorMasterVolume = defaultConfig.MonitorMasterVolume;
        _configService.Config.InjectMasterVolume = defaultConfig.InjectMasterVolume;
        _configService.Config.VolumesLinked = defaultConfig.VolumesLinked;

        // Reset UI settings
        _configService.Config.ClickToPlayEnabled = defaultConfig.ClickToPlayEnabled;

        // Note: Grid dimensions are NOT reset by "Reset to Defaults"
        // to avoid accidentally removing tiles with sounds

        // Reset startup settings
        _configService.Config.StartWithWindows = defaultConfig.StartWithWindows;
        _configService.Config.StartMinimized = defaultConfig.StartMinimized;
        _configService.Config.CloseToTray = defaultConfig.CloseToTray;

        // Update registry for StartWithWindows
        UpdateWindowsStartupRegistry(defaultConfig.StartWithWindows);

        await _configService.SaveAsync();

        // Reload settings in the UI
        LoadAllSettings();
        InitializeSettingDependencies();

        Views.ConfirmDialog.ShowInfo(owner, "Reset Complete", "Settings have been reset to defaults.");
    }

    private async Task FactoryResetAsync()
    {
        var owner = GetOwnerWindow?.Invoke();
        if (owner == null) return;

        var confirmed = Views.ConfirmDialog.Show(
            owner,
            "Factory Reset",
            "Are you sure you want to perform a factory reset?",
            "This will clear all your tile configurations, hotkeys, and settings.\nYour sound files will not be deleted from disk.",
            confirmText: "Reset",
            cancelText: "Cancel",
            isDangerous: true);

        if (!confirmed) return;

        // Second confirmation for destructive action
        var finalConfirm = Views.ConfirmDialog.Show(
            owner,
            "Confirm Factory Reset",
            "This is your last chance to cancel.",
            "All tile configurations and settings will be cleared.",
            confirmText: "Yes, Reset Everything",
            cancelText: "Cancel",
            isDangerous: true);

        if (!finalConfirm) return;

        // Clear StartWithWindows from registry before reset
        UpdateWindowsStartupRegistry(false);

        // Create completely fresh config
        var freshConfig = AppConfig.CreateDefault();

        // Replace the entire config
        _configService.Config.MonitorDeviceId = freshConfig.MonitorDeviceId;
        _configService.Config.InjectDeviceId = freshConfig.InjectDeviceId;
        _configService.Config.MonitorMasterVolume = freshConfig.MonitorMasterVolume;
        _configService.Config.InjectMasterVolume = freshConfig.InjectMasterVolume;
        _configService.Config.VolumesLinked = freshConfig.VolumesLinked;
        _configService.Config.ClickToPlayEnabled = freshConfig.ClickToPlayEnabled;
        _configService.Config.StartWithWindows = freshConfig.StartWithWindows;
        _configService.Config.StartMinimized = freshConfig.StartMinimized;
        _configService.Config.CloseToTray = freshConfig.CloseToTray;
        _configService.Config.StopCurrentHotkey = freshConfig.StopCurrentHotkey;
        _configService.Config.StopAllHotkey = freshConfig.StopAllHotkey;
        _configService.Config.GridColumns = freshConfig.GridColumns;
        _configService.Config.GridRows = freshConfig.GridRows;

        // Reset all tiles
        _configService.Config.Tiles.Clear();
        foreach (var tile in freshConfig.Tiles)
        {
            _configService.Config.Tiles.Add(tile);
        }

        await _configService.SaveAsync();

        // Reload settings in the UI
        LoadAllSettings();
        InitializeSettingDependencies();

        Views.ConfirmDialog.ShowInfo(
            owner,
            "Restart Required",
            "Factory reset complete. The application needs to restart to apply all changes.");

        // Trigger application restart
        OnFactoryReset?.Invoke();
    }
}
