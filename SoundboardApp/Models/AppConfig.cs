namespace Soundboard.Models;

public class AppConfig
{
    public string Version { get; set; } = "1.0.0";

    // Device settings
    public string MonitorDeviceId { get; set; } = "default";
    public string? InjectDeviceId { get; set; }

    // Master volumes (0.0 - 1.0)
    public float MonitorMasterVolume { get; set; } = 1.0f;
    public float InjectMasterVolume { get; set; } = 1.0f;

    // Global hotkeys
    public HotkeyBinding? StopCurrentHotkey { get; set; }
    public HotkeyBinding? StopAllHotkey { get; set; }

    // Tiles
    public List<TileConfig> Tiles { get; set; } = new();

    public static AppConfig CreateDefault()
    {
        var config = new AppConfig();

        // Create 16 empty tiles
        for (int i = 0; i < 16; i++)
        {
            config.Tiles.Add(new TileConfig
            {
                Index = i,
                Name = $"Tile {i + 1}"
            });
        }

        return config;
    }
}
