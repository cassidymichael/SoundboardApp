namespace Soundboard.Models;

public class TileConfig
{
    public int Index { get; set; }
    public string Name { get; set; } = "Empty";
    public string? FilePath { get; set; }
    public float Volume { get; set; } = 1.0f;
    public bool StopOthers { get; set; }
    public bool Protected { get; set; }
    public HotkeyBinding? Hotkey { get; set; }
}
