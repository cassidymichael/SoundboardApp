namespace Soundboard.Models;

public class TileConfig
{
    public int Index { get; set; }
    public string Name { get; set; } = "Empty";

    /// <summary>
    /// Reference to a sound in the library by ID.
    /// If set, the sound is resolved from the library.
    /// If null, FilePath is used directly (for non-library sounds).
    /// </summary>
    public string? SoundEntryId { get; set; }

    /// <summary>
    /// Direct path to the sound file.
    /// Used when SoundEntryId is null (non-library sound) or as fallback.
    /// </summary>
    public string? FilePath { get; set; }

    public float Volume { get; set; } = 1.0f;
    public bool StopOthers { get; set; }
    public bool Protected { get; set; }
    public HotkeyBinding? Hotkey { get; set; }
}
