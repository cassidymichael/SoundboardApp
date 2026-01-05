namespace Soundboard.Models;

public class AppConfig
{
    // Device settings
    public string MonitorDeviceId { get; set; } = "default";
    public string? InjectDeviceId { get; set; }

    // Master volumes (0.0 - 1.0)
    public float MonitorMasterVolume { get; set; } = 1.0f;
    public float InjectMasterVolume { get; set; } = 1.0f;
    public bool VolumesLinked { get; set; } = false;

    // UI settings (default to Edit mode for new users)
    public bool ClickToPlayEnabled { get; set; } = false;

    // Startup settings
    public bool StartWithWindows { get; set; } = false;
    public bool StartMinimized { get; set; } = false;
    public bool CloseToTray { get; set; } = false;

    // Grid layout
    public int GridColumns { get; set; } = 4;
    public int GridRows { get; set; } = 4;

    // Global hotkeys
    public HotkeyBinding? StopCurrentHotkey { get; set; }
    public HotkeyBinding? StopAllHotkey { get; set; }

    // Tiles
    public List<TileConfig> Tiles { get; set; } = new();

    public static AppConfig CreateDefault()
    {
        var config = new AppConfig();

        // Create tiles based on grid dimensions
        int tileCount = config.GridColumns * config.GridRows;
        for (int i = 0; i < tileCount; i++)
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
