namespace Soundboard.Models;

public class TileConfig
{
    public int Index { get; set; }
    public string Name { get; set; } = "Empty";
    public string? FileRelativePath { get; set; }
    public float Volume { get; set; } = 1.0f;
    public bool AllowOverlap { get; set; }
    public HotkeyBinding? Hotkey { get; set; }
}
