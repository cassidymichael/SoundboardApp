namespace Soundboard.Models;

/// <summary>
/// Represents a sound file in the Sounds Library.
/// </summary>
public class SoundEntry
{
    /// <summary>
    /// Unique identifier for the sound entry (GUID string).
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// User-friendly display name for the sound.
    /// </summary>
    public string DisplayName { get; set; } = "";

    /// <summary>
    /// Absolute path to the sound file on disk.
    /// For copied sounds, this points to the app's Sounds folder.
    /// For referenced sounds, this points to the original location.
    /// </summary>
    public string FilePath { get; set; } = "";

    /// <summary>
    /// True if the file was copied to the app's Sounds folder.
    /// False if it's a reference to an external file.
    /// </summary>
    public bool IsCopied { get; set; }

    /// <summary>
    /// Original filename (preserved when file is copied).
    /// </summary>
    public string OriginalFileName { get; set; } = "";

    /// <summary>
    /// When the sound was added to the library.
    /// </summary>
    public DateTime DateAdded { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Cached duration in seconds (populated after first load).
    /// </summary>
    public double? DurationSeconds { get; set; }

    /// <summary>
    /// Flag indicating the file was not found at last validation.
    /// </summary>
    public bool IsMissing { get; set; }
}
