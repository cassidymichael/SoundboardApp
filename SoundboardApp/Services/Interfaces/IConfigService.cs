using Soundboard.Models;

namespace Soundboard.Services.Interfaces;

public interface IConfigService
{
    string AppDataPath { get; }
    AppConfig Config { get; }

    Task LoadAsync();
    Task SaveAsync();
    Task ExportConfigAsync(string filePath);
    Task<bool> ImportConfigAsync(string filePath);
}
