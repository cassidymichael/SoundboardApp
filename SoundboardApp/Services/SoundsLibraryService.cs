using Soundboard.Models;
using Soundboard.Services.Interfaces;
using System.Collections.Concurrent;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Soundboard.Services;

/// <summary>
/// Manages the Sounds Library - a collection of sound entries that can be assigned to tiles.
/// </summary>
public class SoundsLibraryService : ISoundsLibraryService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly string _libraryFilePath;
    private readonly ConcurrentDictionary<string, SoundEntry> _soundsById = new();
    private SoundsLibraryData _data = new();

    public string SoundsFolderPath { get; }

    public IReadOnlyList<SoundEntry> Sounds => _data.Sounds.AsReadOnly();

    public event EventHandler? LibraryChanged;

    public SoundsLibraryService(IConfigService configService)
    {
        var appDataPath = configService.AppDataPath;
        _libraryFilePath = Path.Combine(appDataPath, "sounds-library.json");
        SoundsFolderPath = Path.Combine(appDataPath, "Sounds");

        // Ensure directories exist
        Directory.CreateDirectory(appDataPath);
        Directory.CreateDirectory(SoundsFolderPath);
    }

    public async Task LoadAsync()
    {
        if (File.Exists(_libraryFilePath))
        {
            try
            {
                var json = await File.ReadAllTextAsync(_libraryFilePath);
                _data = JsonSerializer.Deserialize<SoundsLibraryData>(json, JsonOptions) ?? new SoundsLibraryData();
            }
            catch
            {
                _data = new SoundsLibraryData();
            }
        }
        else
        {
            _data = new SoundsLibraryData();
        }

        // Build lookup dictionary
        _soundsById.Clear();
        foreach (var sound in _data.Sounds)
        {
            _soundsById[sound.Id] = sound;
        }

    }

    public async Task SaveAsync()
    {
        var json = JsonSerializer.Serialize(_data, JsonOptions);
        await File.WriteAllTextAsync(_libraryFilePath, json);
    }

    public async Task<SoundEntry> AddSoundAsync(string filePath, string? displayName = null, bool copyToAppFolder = false)
    {
        var originalFileName = Path.GetFileName(filePath);
        var entry = new SoundEntry
        {
            Id = Guid.NewGuid().ToString(),
            DisplayName = displayName ?? Path.GetFileNameWithoutExtension(filePath),
            OriginalFileName = originalFileName,
            FilePath = filePath,
            IsCopied = copyToAppFolder,
            DateAdded = DateTime.UtcNow
        };

        if (copyToAppFolder)
        {
            // Copy file to app's Sounds folder, preserving original name
            var destPath = GetUniqueFilePath(SoundsFolderPath, originalFileName);
            File.Copy(filePath, destPath, overwrite: false);
            entry.FilePath = destPath;
        }

        _data.Sounds.Add(entry);
        _soundsById[entry.Id] = entry;

        await SaveAsync();
        LibraryChanged?.Invoke(this, EventArgs.Empty);

        return entry;
    }

    public bool RemoveSound(string soundId)
    {
        if (!_soundsById.TryRemove(soundId, out var entry))
            return false;

        _data.Sounds.Remove(entry);

        // Delete copied file if applicable
        if (entry.IsCopied && File.Exists(entry.FilePath))
        {
            try
            {
                File.Delete(entry.FilePath);
            }
            catch
            {
                // Ignore file deletion errors
            }
        }

        _ = SaveAsync();
        LibraryChanged?.Invoke(this, EventArgs.Empty);

        return true;
    }

    public void UpdateSound(SoundEntry entry)
    {
        if (_soundsById.TryGetValue(entry.Id, out var existing))
        {
            // Update existing entry properties
            existing.DisplayName = entry.DisplayName;
            existing.DurationSeconds = entry.DurationSeconds;
            existing.IsMissing = entry.IsMissing;

            _ = SaveAsync();
            LibraryChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public SoundEntry? GetById(string id)
    {
        return _soundsById.TryGetValue(id, out var entry) ? entry : null;
    }

    public IEnumerable<SoundEntry> Search(string? query = null)
    {
        var results = _data.Sounds.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(query))
        {
            results = results.Where(s => s.DisplayName.Contains(query, StringComparison.OrdinalIgnoreCase));
        }

        return results;
    }

    public async Task ValidateFilesAsync()
    {
        var changed = false;

        await Task.Run(() =>
        {
            foreach (var sound in _data.Sounds)
            {
                var exists = File.Exists(sound.FilePath);
                if (sound.IsMissing != !exists)
                {
                    sound.IsMissing = !exists;
                    changed = true;
                }
            }
        });

        if (changed)
        {
                await SaveAsync();
            LibraryChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public string? ResolveFilePath(TileConfig tile)
    {
        // If tile references a library sound, resolve it
        if (!string.IsNullOrEmpty(tile.SoundEntryId))
        {
            var entry = GetById(tile.SoundEntryId);
            if (entry != null && !entry.IsMissing)
            {
                return entry.FilePath;
            }
        }

        // Fall back to direct path
        return tile.FilePath;
    }

    /// <summary>
    /// Normalizes a filename by replacing invalid characters with underscores.
    /// </summary>
    private static string NormalizeFileName(string fileName)
    {
        // Get invalid filename characters and add some extras we want to avoid
        var invalidChars = Path.GetInvalidFileNameChars();
        var pattern = $"[{Regex.Escape(new string(invalidChars))}]";
        return Regex.Replace(fileName, pattern, "_");
    }

    /// <summary>
    /// Gets a unique file path in the target folder, appending (1), (2), etc. if needed.
    /// </summary>
    private static string GetUniqueFilePath(string folder, string fileName)
    {
        var normalizedName = NormalizeFileName(fileName);
        var baseName = Path.GetFileNameWithoutExtension(normalizedName);
        var extension = Path.GetExtension(normalizedName);

        var destPath = Path.Combine(folder, normalizedName);

        if (!File.Exists(destPath))
            return destPath;

        // File exists, find a unique name by appending (1), (2), etc.
        var counter = 1;
        do
        {
            destPath = Path.Combine(folder, $"{baseName} ({counter}){extension}");
            counter++;
        } while (File.Exists(destPath));

        return destPath;
    }
}
