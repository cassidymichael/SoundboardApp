using System.IO;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Soundboard.Models;
using Color = System.Windows.Media.Color;
using ColorConverter = System.Windows.Media.ColorConverter;
using Brushes = System.Windows.Media.Brushes;

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
    private double _clipDurationSeconds;

    [ObservableProperty]
    private bool _hasSound;

    // Color properties
    [ObservableProperty]
    private SolidColorBrush _backgroundBrush = new(Color.FromRgb(52, 152, 219)); // #3498DB

    [ObservableProperty]
    private SolidColorBrush _hoverBrush = new(Color.FromRgb(93, 173, 226)); // #5DADE2

    [ObservableProperty]
    private SolidColorBrush _pressedBrush = new(Color.FromRgb(36, 113, 163)); // #2471A3

    [ObservableProperty]
    private SolidColorBrush _playingBrush = new(Color.FromRgb(93, 173, 226)); // Lighter variant

    [ObservableProperty]
    private SolidColorBrush _textBrush = Brushes.White;

    [ObservableProperty]
    private bool _hasCustomColor;

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
        HasSound = !string.IsNullOrEmpty(config.FilePath);
        HotkeyDisplay = config.Hotkey?.GetDisplayString() ?? "";

        if (!string.IsNullOrEmpty(config.FilePath))
        {
            FileName = Path.GetFileName(config.FilePath);
        }

        // Initialize colors
        UpdateColorBrushes();
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

    public void SetSoundFile(string filePath)
    {
        _config.FilePath = filePath;
        FileName = Path.GetFileName(filePath);
        HasSound = !string.IsNullOrEmpty(filePath);
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

    /// <summary>
    /// Sets the tile's background color and updates all color brushes.
    /// </summary>
    /// <param name="hexColor">Hex color string (e.g., "#FF3498DB") or null for default.</param>
    public void SetBackgroundColor(string? hexColor)
    {
        _config.BackgroundColor = hexColor;
        UpdateColorBrushes();
    }

    /// <summary>
    /// Gets the current background color hex string.
    /// </summary>
    public string? GetBackgroundColor() => _config.BackgroundColor;

    private void UpdateColorBrushes()
    {
        Color baseColor;

        if (!string.IsNullOrEmpty(_config.BackgroundColor))
        {
            try
            {
                baseColor = (Color)ColorConverter.ConvertFromString(_config.BackgroundColor);
                HasCustomColor = true;
            }
            catch
            {
                // Invalid color, use default
                baseColor = Color.FromRgb(52, 152, 219); // #3498DB
                HasCustomColor = false;
            }
        }
        else
        {
            // Default blue
            baseColor = Color.FromRgb(52, 152, 219); // #3498DB
            HasCustomColor = false;
        }

        BackgroundBrush = new SolidColorBrush(baseColor);
        HoverBrush = new SolidColorBrush(LightenColor(baseColor, 0.15));
        PressedBrush = new SolidColorBrush(DarkenColor(baseColor, 0.15));
        PlayingBrush = new SolidColorBrush(LightenColor(baseColor, 0.25));
        TextBrush = new SolidColorBrush(GetContrastingTextColor(baseColor));
    }

    private static Color LightenColor(Color color, double factor)
    {
        return Color.FromRgb(
            (byte)Math.Min(255, color.R + (255 - color.R) * factor),
            (byte)Math.Min(255, color.G + (255 - color.G) * factor),
            (byte)Math.Min(255, color.B + (255 - color.B) * factor));
    }

    private static Color DarkenColor(Color color, double factor)
    {
        return Color.FromRgb(
            (byte)(color.R * (1 - factor)),
            (byte)(color.G * (1 - factor)),
            (byte)(color.B * (1 - factor)));
    }

    private static Color GetContrastingTextColor(Color background)
    {
        // Calculate relative luminance (ITU-R BT.709)
        // Threshold at 0.6 to allow slightly lighter colors with white text
        double luminance = (0.2126 * background.R + 0.7152 * background.G + 0.0722 * background.B) / 255;
        return luminance > 0.6 ? Colors.Black : Colors.White;
    }
}
