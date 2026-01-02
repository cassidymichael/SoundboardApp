using Soundboard.Models;
using Soundboard.Services.Interfaces;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Soundboard.Services;

public class ConfigService : IConfigService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() }
    };

    public string AppDataPath { get; }
    public string SoundsPath { get; }
    public AppConfig Config { get; private set; } = null!;

    private readonly string _configFilePath;

    public ConfigService()
    {
        AppDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Soundboard");
        SoundsPath = Path.Combine(AppDataPath, "sounds");
        _configFilePath = Path.Combine(AppDataPath, "config.json");

        // Ensure directories exist
        Directory.CreateDirectory(AppDataPath);
        Directory.CreateDirectory(SoundsPath);
    }

    public async Task LoadAsync()
    {
        if (File.Exists(_configFilePath))
        {
            try
            {
                var json = await File.ReadAllTextAsync(_configFilePath);
                Config = JsonSerializer.Deserialize<AppConfig>(json, JsonOptions) ?? AppConfig.CreateDefault();
            }
            catch
            {
                Config = AppConfig.CreateDefault();
            }
        }
        else
        {
            Config = AppConfig.CreateDefault();
            await SaveAsync();
        }

        // Ensure we have 16 tiles
        while (Config.Tiles.Count < 16)
        {
            Config.Tiles.Add(new TileConfig
            {
                Index = Config.Tiles.Count,
                Name = $"Tile {Config.Tiles.Count + 1}"
            });
        }
    }

    public async Task SaveAsync()
    {
        var json = JsonSerializer.Serialize(Config, JsonOptions);
        await File.WriteAllTextAsync(_configFilePath, json);
    }

    public async Task<string> ImportSoundAsync(int tileIndex, string sourceFilePath)
    {
        var tileDir = Path.Combine(SoundsPath, $"tile_{tileIndex:D2}");
        Directory.CreateDirectory(tileDir);

        // Clean up old files in the tile directory
        foreach (var existingFile in Directory.GetFiles(tileDir))
        {
            File.Delete(existingFile);
        }

        // Copy the new file, keeping original filename
        var destFileName = Path.GetFileName(sourceFilePath);
        var destPath = Path.Combine(tileDir, destFileName);

        await Task.Run(() => File.Copy(sourceFilePath, destPath, overwrite: true));

        // Return relative path from sounds directory
        return Path.GetRelativePath(SoundsPath, destPath);
    }

    public string GetSoundFullPath(string relativePath)
    {
        return Path.Combine(SoundsPath, relativePath);
    }
}
