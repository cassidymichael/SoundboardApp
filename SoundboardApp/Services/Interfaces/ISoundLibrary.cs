using Soundboard.Models;

namespace Soundboard.Services.Interfaces;

public interface ISoundLibrary
{
    /// <summary>
    /// Gets a cached audio buffer, loading and decoding the file if not already cached.
    /// </summary>
    AudioBuffer? GetOrLoad(string filePath);

    /// <summary>
    /// Preloads all sounds for the given tiles.
    /// </summary>
    Task PreloadAsync(IEnumerable<TileConfig> tiles);

    /// <summary>
    /// Clears the cache for a specific file.
    /// </summary>
    void Invalidate(string filePath);

    /// <summary>
    /// Clears all cached audio.
    /// </summary>
    void ClearCache();
}
