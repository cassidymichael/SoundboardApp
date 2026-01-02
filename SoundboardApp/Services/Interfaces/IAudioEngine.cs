using Soundboard.Models;

namespace Soundboard.Services.Interfaces;

public interface IAudioEngine : IDisposable
{
    float MonitorMasterVolume { get; set; }
    float InjectMasterVolume { get; set; }

    bool IsMonitorEnabled { get; }
    bool IsInjectEnabled { get; }

    int ActiveVoiceCount { get; }

    void Initialize(string? monitorDeviceId, string? injectDeviceId);

    void SetMonitorDevice(string? deviceId);
    void SetInjectDevice(string? deviceId);

    void Play(int tileId, AudioBuffer buffer, float tileVolume, bool stopOthers, bool isProtected);

    void StopTile(int tileId);
    void StopCurrent();
    void StopAll();

    double GetProgress(int tileId);

    event EventHandler<int>? TileStarted;
    event EventHandler<int>? TileStopped;
    event EventHandler<string>? Error;
}
