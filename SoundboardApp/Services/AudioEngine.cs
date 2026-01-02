using NAudio.CoreAudioApi;
using Soundboard.Audio;
using Soundboard.Models;
using Soundboard.Services.Interfaces;
using System.Windows;

namespace Soundboard.Services;

public class AudioEngine : IAudioEngine
{
    private readonly IDeviceEnumerator _deviceEnumerator;
    private readonly OutputBus _monitorBus;
    private readonly OutputBus _injectBus;
    private readonly List<VoicePair> _activeVoices = new();
    private readonly object _voiceLock = new();
    private readonly System.Timers.Timer _cleanupTimer;

    private const int MaxVoices = 4;

    public float MonitorMasterVolume { get; set; } = 1.0f;
    public float InjectMasterVolume { get; set; } = 1.0f;

    public bool IsMonitorEnabled => _monitorBus.IsActive;
    public bool IsInjectEnabled => _injectBus.IsActive;

    public int ActiveVoiceCount
    {
        get
        {
            lock (_voiceLock)
            {
                return _activeVoices.Count(v => v.MonitorVoice.State != VoiceState.Stopped);
            }
        }
    }

    public event EventHandler<int>? TileStarted;
    public event EventHandler<int>? TileStopped;
    public event EventHandler<string>? Error;

    public AudioEngine(IDeviceEnumerator deviceEnumerator)
    {
        _deviceEnumerator = deviceEnumerator;
        _monitorBus = new OutputBus();
        _injectBus = new OutputBus();

        // Periodic cleanup of stopped voices
        _cleanupTimer = new System.Timers.Timer(100);
        _cleanupTimer.Elapsed += (_, _) => CleanupStoppedVoices();
        _cleanupTimer.Start();
    }

    public void Initialize(string? monitorDeviceId, string? injectDeviceId)
    {
        SetMonitorDevice(monitorDeviceId);
        SetInjectDevice(injectDeviceId);
    }

    public void SetMonitorDevice(string? deviceId)
    {
        try
        {
            var device = GetDevice(deviceId) ?? GetDevice("default");
            if (device != null)
            {
                _monitorBus.Initialize(device);
            }
        }
        catch (Exception ex)
        {
            Error?.Invoke(this, $"Failed to initialize monitor output: {ex.Message}");
        }
    }

    public void SetInjectDevice(string? deviceId)
    {
        if (string.IsNullOrEmpty(deviceId))
        {
            // Try to find Voicemeeter AUX
            var vmDevice = _deviceEnumerator.FindVoicemeeterAux();
            if (vmDevice != null)
            {
                deviceId = vmDevice.Id;
            }
        }

        if (string.IsNullOrEmpty(deviceId))
        {
            // No inject device configured
            return;
        }

        try
        {
            var device = GetDevice(deviceId);
            if (device != null)
            {
                _injectBus.Initialize(device);
            }
            else
            {
                Error?.Invoke(this, $"Inject device not found: {deviceId}");
            }
        }
        catch (Exception ex)
        {
            Error?.Invoke(this, $"Failed to initialize inject output: {ex.Message}");
        }
    }

    public void Play(int tileId, AudioBuffer buffer, float tileVolume, bool allowOverlap)
    {
        lock (_voiceLock)
        {
            // Cut policy: stop existing sounds if overlap not allowed
            if (!allowOverlap)
            {
                FadeOutAll();
            }

            // Stop existing instances of the same tile (restart behavior)
            FadeOutTile(tileId);

            // Enforce voice cap
            while (_activeVoices.Count(v => v.MonitorVoice.State != VoiceState.Stopped) >= MaxVoices)
            {
                KillOldestVoice();
            }

            // Create voice pair for both buses
            var monitorVoice = new Voice(tileId, buffer, tileVolume, MonitorMasterVolume);
            var injectVoice = new Voice(tileId, buffer, tileVolume, InjectMasterVolume);

            var pair = new VoicePair(tileId, monitorVoice, injectVoice);
            _activeVoices.Add(pair);

            // Add to mixers
            _monitorBus.AddVoice(monitorVoice);
            _injectBus.AddVoice(injectVoice);

            // Notify UI
            Application.Current?.Dispatcher.InvokeAsync(() => TileStarted?.Invoke(this, tileId));
        }
    }

    public void StopTile(int tileId)
    {
        lock (_voiceLock)
        {
            FadeOutTile(tileId);
        }
    }

    public void StopCurrent()
    {
        lock (_voiceLock)
        {
            // Stop the most recently started voice
            var mostRecent = _activeVoices
                .LastOrDefault(v => v.MonitorVoice.State == VoiceState.Playing);

            mostRecent?.BeginFadeOut();
        }
    }

    public void StopAll()
    {
        lock (_voiceLock)
        {
            FadeOutAll();
        }
    }

    private void FadeOutTile(int tileId)
    {
        foreach (var pair in _activeVoices.Where(v => v.TileId == tileId))
        {
            pair.BeginFadeOut();
        }
    }

    private void FadeOutAll()
    {
        foreach (var pair in _activeVoices)
        {
            pair.BeginFadeOut();
        }
    }

    private void KillOldestVoice()
    {
        var oldest = _activeVoices.FirstOrDefault(v => v.MonitorVoice.State == VoiceState.Playing);
        oldest?.BeginFadeOut();
    }

    private void CleanupStoppedVoices()
    {
        List<VoicePair> toRemove;

        lock (_voiceLock)
        {
            toRemove = _activeVoices
                .Where(v => v.MonitorVoice.State == VoiceState.Stopped)
                .ToList();

            foreach (var pair in toRemove)
            {
                _activeVoices.Remove(pair);
                _monitorBus.RemoveVoice(pair.MonitorVoice);
                _injectBus.RemoveVoice(pair.InjectVoice);
            }
        }

        // Notify UI about stopped tiles
        foreach (var pair in toRemove)
        {
            Application.Current?.Dispatcher.InvokeAsync(() => TileStopped?.Invoke(this, pair.TileId));
        }
    }

    private MMDevice? GetDevice(string? id)
    {
        if (_deviceEnumerator is DeviceEnumerator de)
        {
            return de.GetMMDevice(id);
        }
        return null;
    }

    public void Dispose()
    {
        _cleanupTimer.Stop();
        _cleanupTimer.Dispose();

        lock (_voiceLock)
        {
            _activeVoices.Clear();
        }

        _monitorBus.Dispose();
        _injectBus.Dispose();
    }

    private class VoicePair
    {
        public int TileId { get; }
        public Voice MonitorVoice { get; }
        public Voice InjectVoice { get; }

        public VoicePair(int tileId, Voice monitorVoice, Voice injectVoice)
        {
            TileId = tileId;
            MonitorVoice = monitorVoice;
            InjectVoice = injectVoice;
        }

        public void BeginFadeOut()
        {
            MonitorVoice.BeginFadeOut();
            InjectVoice.BeginFadeOut();
        }
    }
}
