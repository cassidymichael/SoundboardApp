using Soundboard.Models;

namespace Soundboard.Services.Interfaces;

/// <summary>
/// Manages the Sounds Library - a collection of sound entries that can be assigned to tiles.
/// </summary>
public interface ISoundsLibraryService
{
    /// <summary>
    /// All sounds in the library.
    /// </summary>
    IReadOnlyList<SoundEntry> Sounds { get; }

    /// <summary>
    /// Fired when the library changes (add/remove/update).
    /// </summary>
    event EventHandler? LibraryChanged;

    /// <summary>
    /// Load library from disk.
    /// </summary>
    Task LoadAsync();

    /// <summary>
    /// Save library to disk.
    /// </summary>
    Task SaveAsync();

    /// <summary>
    /// Add a sound file to the library.
    /// </summary>
    /// <param name="filePath">Path to the source sound file.</param>
    /// <param name="displayName">Optional display name (defaults to filename).</param>
    /// <param name="copyToAppFolder">If true, copies the file to the app's Sounds folder.</param>
    /// <returns>The created sound entry.</returns>
    Task<SoundEntry> AddSoundAsync(string filePath, string? displayName = null, bool copyToAppFolder = false);

    /// <summary>
    /// Remove a sound from the library by ID.
    /// If the sound was copied, the file is also deleted.
    /// </summary>
    /// <returns>True if found and removed.</returns>
    bool RemoveSound(string soundId);

    /// <summary>
    /// Update a sound entry's metadata (display name, tags).
    /// </summary>
    void UpdateSound(SoundEntry entry);

    /// <summary>
    /// Get sound by ID.
    /// </summary>
    SoundEntry? GetById(string id);

    /// <summary>
    /// Search/filter sounds by query.
    /// </summary>
    /// <param name="query">Text to search in display name (case-insensitive).</param>
    IEnumerable<SoundEntry> Search(string? query = null);

    /// <summary>
    /// Verify all file paths exist, updating IsMissing flags.
    /// </summary>
    Task ValidateFilesAsync();

    /// <summary>
    /// Resolve the file path for a tile, handling library references.
    /// </summary>
    /// <param name="tile">The tile configuration.</param>
    /// <returns>The resolved file path, or null if not found.</returns>
    string? ResolveFilePath(TileConfig tile);

    /// <summary>
    /// Get the path to the app's Sounds folder where copied files are stored.
    /// </summary>
    string SoundsFolderPath { get; }
}
