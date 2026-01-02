using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Soundboard.Models;

namespace Soundboard.ViewModels;

public partial class TileViewModel : ObservableObject
{
    private readonly TileConfig _config;
    private readonly Action<TileViewModel> _onSelect;
    private readonly Action<TileViewModel>? _onPlay;
    private readonly Action<TileViewModel>? _onStop;

    public int Index => _config.Index;

    [ObservableProperty]
    private string _name = "Empty";

    [ObservableProperty]
    private string _fileName = "";

    [ObservableProperty]
    private string _hotkeyDisplay = "";

    [ObservableProperty]
    private int _volumePercent = 100;

    [ObservableProperty]
    private bool _stopOthers;

    [ObservableProperty]
    private bool _protected;

    [ObservableProperty]
    private bool _isPlaying;

    [ObservableProperty]
    private bool _isSelected;

    [ObservableProperty]
    private double _progress;

    [ObservableProperty]
    private bool _hasSound;

    public float Volume => VolumePercent / 100f;

    public TileConfig Config => _config;

    public TileViewModel(TileConfig config,
        Action<TileViewModel> onSelect,
        Action<TileViewModel>? onPlay = null,
        Action<TileViewModel>? onStop = null)
    {
        _config = config;
        _onSelect = onSelect;
        _onPlay = onPlay;
        _onStop = onStop;

        // Initialize from config
        Name = config.Name;
        VolumePercent = (int)(config.Volume * 100);
        StopOthers = config.StopOthers;
        Protected = config.Protected;
        HasSound = !string.IsNullOrEmpty(config.FileRelativePath);
        HotkeyDisplay = config.Hotkey?.GetDisplayString() ?? "";

        if (!string.IsNullOrEmpty(config.FileRelativePath))
        {
            FileName = Path.GetFileName(config.FileRelativePath);
        }
    }

    [RelayCommand]
    private void Select()
    {
        _onSelect(this);
    }

    [RelayCommand]
    private void Play()
    {
        _onPlay?.Invoke(this);
    }

    [RelayCommand]
    private void Stop()
    {
        _onStop?.Invoke(this);
    }

    public void UpdateConfig()
    {
        _config.Name = Name;
        _config.Volume = Volume;
        _config.StopOthers = StopOthers;
        _config.Protected = Protected;
    }

    public void SetHotkey(HotkeyBinding? hotkey)
    {
        _config.Hotkey = hotkey;
        HotkeyDisplay = hotkey?.GetDisplayString() ?? "";
    }

    public void SetSoundFile(string relativePath)
    {
        _config.FileRelativePath = relativePath;
        FileName = Path.GetFileName(relativePath);
        HasSound = !string.IsNullOrEmpty(relativePath);
    }

    partial void OnNameChanged(string value)
    {
        _config.Name = value;
    }

    partial void OnVolumePercentChanged(int value)
    {
        _config.Volume = value / 100f;
    }

    partial void OnStopOthersChanged(bool value)
    {
        _config.StopOthers = value;
    }

    partial void OnProtectedChanged(bool value)
    {
        _config.Protected = value;
    }
}
