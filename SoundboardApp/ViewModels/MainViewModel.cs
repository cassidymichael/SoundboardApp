using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using Soundboard.Models;
using Soundboard.Services.Interfaces;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Threading;

namespace Soundboard.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly IConfigService _configService;
    private readonly IDeviceEnumerator _deviceEnumerator;
    private readonly ISoundLibrary _soundLibrary;
    private readonly IAudioEngine _audioEngine;
    private readonly IHotkeyService _hotkeyService;

    private bool _isLearningHotkey;
    private readonly DispatcherTimer _saveDebounceTimer;
    private readonly DispatcherTimer _statusFadeTimer;
    private readonly DispatcherTimer _progressTimer;
    private DateTime _statusSetTime;

    public ObservableCollection<TileViewModel> Tiles { get; } = new();
    public ObservableCollection<AudioDevice> OutputDevices { get; } = new();

    [ObservableProperty]
    private TileViewModel? _selectedTile;

    [ObservableProperty]
    private bool _hasSelectedTile;

    [ObservableProperty]
    private AudioDevice? _selectedMonitorDevice;

    [ObservableProperty]
    private AudioDevice? _selectedInjectDevice;

    [ObservableProperty]
    private int _monitorVolumePercent = 100;

    [ObservableProperty]
    private int _injectVolumePercent = 100;

    [ObservableProperty]
    private string _statusMessage = "";

    [ObservableProperty]
    private double _statusOpacity = 0;

    [ObservableProperty]
    private string _learnHotkeyButtonText = "Set";

    [ObservableProperty]
    private bool _clickToPlayEnabled = true;

    public bool EditModeEnabled
    {
        get => !ClickToPlayEnabled;
        set => ClickToPlayEnabled = !value;
    }

    public MainViewModel(
        IConfigService configService,
        IDeviceEnumerator deviceEnumerator,
        ISoundLibrary soundLibrary,
        IAudioEngine audioEngine,
        IHotkeyService hotkeyService)
    {
        _configService = configService;
        _deviceEnumerator = deviceEnumerator;
        _soundLibrary = soundLibrary;
        _audioEngine = audioEngine;
        _hotkeyService = hotkeyService;

        // Debounced save timer (500ms delay to batch rapid changes)
        _saveDebounceTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
        _saveDebounceTimer.Tick += async (_, _) =>
        {
            _saveDebounceTimer.Stop();
            await _configService.SaveAsync();
        };

        // Status fade timer (updates every 50ms for smooth fade)
        _statusFadeTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(50) };
        _statusFadeTimer.Tick += OnStatusFadeTick;

        // Progress update timer (updates tile progress bars)
        _progressTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(50) };
        _progressTimer.Tick += OnProgressTick;
        _progressTimer.Start();

        // Wire up events
        _audioEngine.TileStarted += OnTileStarted;
        _audioEngine.TileStopped += OnTileStopped;
        _audioEngine.Error += OnAudioError;
        _hotkeyService.TileTriggered += OnTileTriggered;
        _hotkeyService.StopCurrentTriggered += (_, _) => _audioEngine.StopCurrent();
        _hotkeyService.StopAllTriggered += (_, _) => _audioEngine.StopAll();
        _hotkeyService.RegistrationFailed += OnHotkeyRegistrationFailed;
        _deviceEnumerator.DevicesChanged += (_, _) => RefreshDevices();

        Initialize();
    }

    private void Initialize()
    {
        // Load devices
        RefreshDevices();

        // Initialize tiles from config
        foreach (var tileConfig in _configService.Config.Tiles)
        {
            var tileVm = new TileViewModel(tileConfig, OnTileSelected, OnTilePlay, OnTileStop);
            tileVm.PropertyChanged += OnTilePropertyChanged;
            Tiles.Add(tileVm);

            // Register hotkey if configured
            if (tileConfig.Hotkey != null)
            {
                _hotkeyService.RegisterTileHotkey(tileConfig.Index, tileConfig.Hotkey);
            }
        }

        // Set master volumes
        MonitorVolumePercent = (int)(_configService.Config.MonitorMasterVolume * 100);
        InjectVolumePercent = (int)(_configService.Config.InjectMasterVolume * 100);

        // Set UI settings
        ClickToPlayEnabled = _configService.Config.ClickToPlayEnabled;

        // Set selected devices
        SelectedMonitorDevice = OutputDevices.FirstOrDefault(d => d.Id == _configService.Config.MonitorDeviceId)
                               ?? OutputDevices.FirstOrDefault();
        SelectedInjectDevice = OutputDevices.FirstOrDefault(d => d.Id == _configService.Config.InjectDeviceId);

        // Initialize audio engine
        _audioEngine.Initialize(SelectedMonitorDevice?.Id, SelectedInjectDevice?.Id);

        // Register global hotkeys
        if (_configService.Config.StopCurrentHotkey != null)
            _hotkeyService.RegisterStopCurrentHotkey(_configService.Config.StopCurrentHotkey);
        if (_configService.Config.StopAllHotkey != null)
            _hotkeyService.RegisterStopAllHotkey(_configService.Config.StopAllHotkey);

        // Preload sounds
        _ = _soundLibrary.PreloadAsync(_configService.Config.Tiles);
    }

    private void RefreshDevices()
    {
        var currentMonitor = SelectedMonitorDevice?.Id;
        var currentInject = SelectedInjectDevice?.Id;

        OutputDevices.Clear();
        foreach (var device in _deviceEnumerator.GetOutputDevices())
        {
            OutputDevices.Add(device);
        }

        // Restore selection
        SelectedMonitorDevice = OutputDevices.FirstOrDefault(d => d.Id == currentMonitor) ?? OutputDevices.FirstOrDefault();
        SelectedInjectDevice = OutputDevices.FirstOrDefault(d => d.Id == currentInject);
    }

    private void OnTileSelected(TileViewModel tile)
    {
        // Deselect previous
        if (SelectedTile != null)
            SelectedTile.IsSelected = false;

        SelectedTile = tile;
        tile.IsSelected = true;
        HasSelectedTile = true;

        // Cancel hotkey learning if active
        if (_isLearningHotkey)
        {
            _isLearningHotkey = false;
            LearnHotkeyButtonText = "Set";
            _hotkeyService.ResumeAll();
        }
    }

    private void OnTilePlay(TileViewModel tile)
    {
        PlayTile(tile.Index);
    }

    private void OnTileStop(TileViewModel tile)
    {
        _audioEngine.StopTile(tile.Index);
    }

    private void OnProgressTick(object? sender, EventArgs e)
    {
        foreach (var tile in Tiles.Where(t => t.IsPlaying))
        {
            var progress = _audioEngine.GetProgress(tile.Index);
            tile.Progress = progress >= 0 ? progress : 0;
        }
    }

    private void OnTilePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        // Save config when tile properties that persist are changed
        if (e.PropertyName is "Name" or "VolumePercent" or "StopOthers" or "Protected")
        {
            // Restart debounce timer
            _saveDebounceTimer.Stop();
            _saveDebounceTimer.Start();
        }
    }

    [RelayCommand]
    private void StopCurrent()
    {
        _audioEngine.StopCurrent();
    }

    [RelayCommand]
    private void StopAll()
    {
        _audioEngine.StopAll();
    }

    [RelayCommand]
    private async Task ImportSound()
    {
        if (SelectedTile == null) return;

        var dialog = new OpenFileDialog
        {
            Title = "Select Sound File",
            Filter = "Audio Files|*.wav;*.mp3;*.ogg;*.flac|All Files|*.*"
        };

        if (dialog.ShowDialog() == true)
        {
            try
            {
                ShowStatus("Importing sound...");

                var relativePath = await _configService.ImportSoundAsync(SelectedTile.Index, dialog.FileName);
                SelectedTile.SetSoundFile(relativePath);

                // Invalidate cache and reload
                _soundLibrary.Invalidate(relativePath);
                _soundLibrary.GetOrLoad(relativePath);

                await _configService.SaveAsync();
                ShowStatus("Sound imported successfully");
            }
            catch (Exception ex)
            {
                ShowStatus($"Import failed: {ex.Message}");
            }
        }
    }

    [RelayCommand]
    private void LearnHotkey()
    {
        if (SelectedTile == null) return;

        if (_isLearningHotkey)
        {
            // Cancel learning
            _isLearningHotkey = false;
            LearnHotkeyButtonText = "Set";
            _hotkeyService.ResumeAll();
            ShowStatus("Hotkey learning cancelled");
        }
        else
        {
            // Start learning - suspend all hotkeys so we can capture any key
            _isLearningHotkey = true;
            LearnHotkeyButtonText = "Cancel";
            _hotkeyService.SuspendAll();
            ShowStatus("Press a key to assign...");
        }
    }

    public void OnKeyPressed(System.Windows.Input.Key key, System.Windows.Input.ModifierKeys modifiers)
    {
        if (!_isLearningHotkey || SelectedTile == null) return;

        // Ignore modifier-only presses
        if (key == System.Windows.Input.Key.LeftCtrl || key == System.Windows.Input.Key.RightCtrl ||
            key == System.Windows.Input.Key.LeftAlt || key == System.Windows.Input.Key.RightAlt ||
            key == System.Windows.Input.Key.LeftShift || key == System.Windows.Input.Key.RightShift ||
            key == System.Windows.Input.Key.LWin || key == System.Windows.Input.Key.RWin)
            return;

        var binding = new HotkeyBinding(key, modifiers);

        // Check if another tile already has this hotkey - if so, unbind it
        int? conflictingTileIndex = null;
        foreach (var tile in Tiles)
        {
            if (tile.Index != SelectedTile.Index &&
                tile.Config.Hotkey != null &&
                tile.Config.Hotkey.Key == key &&
                tile.Config.Hotkey.Modifiers == modifiers)
            {
                conflictingTileIndex = tile.Index;
                tile.SetHotkey(null);
            }
        }

        // Resume hotkeys
        _hotkeyService.ResumeAll();

        // Unregister the conflicting tile's hotkey (it was re-registered by ResumeAll)
        if (conflictingTileIndex.HasValue)
        {
            _hotkeyService.UnregisterTileHotkey(conflictingTileIndex.Value);
        }

        // Unregister old hotkey for this tile
        _hotkeyService.UnregisterTileHotkey(SelectedTile.Index);

        // Register new hotkey
        if (_hotkeyService.RegisterTileHotkey(SelectedTile.Index, binding))
        {
            SelectedTile.SetHotkey(binding);
            ShowStatus($"Hotkey set to {binding.GetDisplayString()}");
            _ = _configService.SaveAsync();
        }

        _isLearningHotkey = false;
        LearnHotkeyButtonText = "Set";
    }

    [RelayCommand]
    private void ClearHotkey()
    {
        if (SelectedTile == null) return;

        _hotkeyService.UnregisterTileHotkey(SelectedTile.Index);
        SelectedTile.SetHotkey(null);
        _ = _configService.SaveAsync();
        ShowStatus("Hotkey cleared");
    }

    [RelayCommand]
    private void TestSound()
    {
        if (SelectedTile == null || !SelectedTile.HasSound) return;

        PlayTile(SelectedTile.Index);
    }

    private void OnTileTriggered(object? sender, int tileIndex)
    {
        Application.Current?.Dispatcher.InvokeAsync(() => PlayTile(tileIndex));
    }

    private void PlayTile(int tileIndex)
    {
        if (tileIndex < 0 || tileIndex >= Tiles.Count) return;

        var tile = Tiles[tileIndex];
        if (!tile.HasSound) return;

        var config = tile.Config;
        if (string.IsNullOrEmpty(config.FileRelativePath)) return;

        var buffer = _soundLibrary.GetOrLoad(config.FileRelativePath);
        if (buffer == null)
        {
            ShowStatus($"Sound file not found for {tile.Name}");
            return;
        }

        _audioEngine.Play(tileIndex, buffer, tile.Volume, tile.StopOthers, tile.Protected);
    }

    private void OnTileStarted(object? sender, int tileIndex)
    {
        if (tileIndex >= 0 && tileIndex < Tiles.Count)
        {
            Tiles[tileIndex].IsPlaying = true;
        }
    }

    private void OnTileStopped(object? sender, int tileIndex)
    {
        if (tileIndex >= 0 && tileIndex < Tiles.Count)
        {
            Tiles[tileIndex].IsPlaying = false;
            Tiles[tileIndex].Progress = 0;
        }
    }

    private void OnAudioError(object? sender, string message)
    {
        Application.Current?.Dispatcher.InvokeAsync(() => ShowStatus(message));
    }

    private void OnHotkeyRegistrationFailed(object? sender, string message)
    {
        Application.Current?.Dispatcher.InvokeAsync(() => ShowStatus(message));
    }

    partial void OnSelectedMonitorDeviceChanged(AudioDevice? value)
    {
        if (value != null)
        {
            _audioEngine.SetMonitorDevice(value.Id);
            _configService.Config.MonitorDeviceId = value.Id;
            _ = _configService.SaveAsync();
        }
    }

    partial void OnSelectedInjectDeviceChanged(AudioDevice? value)
    {
        _audioEngine.SetInjectDevice(value?.Id);
        _configService.Config.InjectDeviceId = value?.Id;
        _ = _configService.SaveAsync();
    }

    partial void OnMonitorVolumePercentChanged(int value)
    {
        _audioEngine.MonitorMasterVolume = value / 100f;
        _configService.Config.MonitorMasterVolume = value / 100f;
        _saveDebounceTimer.Stop();
        _saveDebounceTimer.Start();
    }

    partial void OnInjectVolumePercentChanged(int value)
    {
        _audioEngine.InjectMasterVolume = value / 100f;
        _configService.Config.InjectMasterVolume = value / 100f;
        _saveDebounceTimer.Stop();
        _saveDebounceTimer.Start();
    }

    partial void OnClickToPlayEnabledChanged(bool value)
    {
        _configService.Config.ClickToPlayEnabled = value;
        _ = _configService.SaveAsync();
        OnPropertyChanged(nameof(EditModeEnabled));
    }

    private void ShowStatus(string message)
    {
        StatusMessage = message;
        StatusOpacity = 1;
        _statusSetTime = DateTime.UtcNow;
        _statusFadeTimer.Start();
    }

    private void OnStatusFadeTick(object? sender, EventArgs e)
    {
        var elapsed = (DateTime.UtcNow - _statusSetTime).TotalMilliseconds;
        const double visibleMs = 10000; // Show for 10 seconds
        const double fadeMs = 500;      // Fade over 0.5 seconds

        if (elapsed < visibleMs)
        {
            // Still visible
            StatusOpacity = 1;
        }
        else if (elapsed < visibleMs + fadeMs)
        {
            // Fading out
            StatusOpacity = 1 - ((elapsed - visibleMs) / fadeMs);
        }
        else
        {
            // Done
            StatusOpacity = 0;
            _statusFadeTimer.Stop();
        }
    }
}
