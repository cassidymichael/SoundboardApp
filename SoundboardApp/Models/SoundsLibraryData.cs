namespace Soundboard.Models;

/// <summary>
/// Root container for the sounds library data, persisted to sounds-library.json.
/// </summary>
public class SoundsLibraryData
{
    /// <summary>
    /// Schema version for migration support.
    /// </summary>
    public int Version { get; set; } = 1;

    /// <summary>
    /// All sounds in the library.
    /// </summary>
    public List<SoundEntry> Sounds { get; set; } = new();
}
