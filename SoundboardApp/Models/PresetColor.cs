using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using Color = System.Windows.Media.Color;
using ColorConverter = System.Windows.Media.ColorConverter;
using Brushes = System.Windows.Media.Brushes;

namespace Soundboard.Models;

/// <summary>
/// Represents a preset color option for tile customization.
/// </summary>
public partial class PresetColor : ObservableObject
{
    public required string Name { get; init; }
    public required string HexColor { get; init; }
    public SolidColorBrush Brush { get; private set; } = Brushes.Transparent;

    [ObservableProperty]
    private bool _isSelected;

    public void Initialize()
    {
        Brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(HexColor));
    }

    /// <summary>
    /// Returns the default preset color palette.
    /// </summary>
    public static List<PresetColor> GetDefaults()
    {
        var presets = new List<PresetColor>
        {
            new() { Name = "Blue", HexColor = "#FF3498DB" },        // Default
            new() { Name = "Teal", HexColor = "#FF1ABC9C" },
            new() { Name = "Green", HexColor = "#FF27AE60" },
            new() { Name = "Purple", HexColor = "#FF9B59B6" },
            new() { Name = "Pink", HexColor = "#FFE91E63" },
            new() { Name = "Red", HexColor = "#FFE74C3C" },
            new() { Name = "Orange", HexColor = "#FFF39C12" },
            new() { Name = "Yellow", HexColor = "#FFF1C40F" },
            new() { Name = "Brown", HexColor = "#FF795548" },
            new() { Name = "Gray", HexColor = "#FF607D8B" },
            new() { Name = "Dark Blue", HexColor = "#FF2C3E50" },
            new() { Name = "Indigo", HexColor = "#FF3F51B5" }
        };

        foreach (var preset in presets)
        {
            preset.Initialize();
        }

        return presets;
    }
}
