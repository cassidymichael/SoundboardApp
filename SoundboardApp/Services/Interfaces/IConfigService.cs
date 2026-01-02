using Soundboard.Models;

namespace Soundboard.Services.Interfaces;

public interface IConfigService
{
    string AppDataPath { get; }
    string SoundsPath { get; }
    AppConfig Config { get; }

    Task LoadAsync();
    Task SaveAsync();

    /// <summary>
    /// Imports a sound file by copying it to the app's sounds directory.
    /// </summary>
    /// <returns>The relative path to the imported file.</returns>
    Task<string> ImportSoundAsync(int tileIndex, string sourceFilePath);

    /// <summary>
    /// Gets the full path to a sound file from its relative path.
    /// </summary>
    string GetSoundFullPath(string relativePath);
}
