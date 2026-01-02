using NAudio.CoreAudioApi;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace Soundboard.Audio;

public class OutputBus : IDisposable
{
    private WasapiOut? _output;
    private MixingSampleProvider? _mixer;
    private readonly object _lock = new();
    private bool _disposed;

    public bool IsActive => _output != null && _output.PlaybackState == PlaybackState.Playing;
    public string? DeviceId { get; private set; }

    private static readonly WaveFormat OutputFormat = WaveFormat.CreateIeeeFloatWaveFormat(48000, 2);

    public void Initialize(MMDevice device)
    {
        lock (_lock)
        {
            Cleanup();

            DeviceId = device.ID;

            _mixer = new MixingSampleProvider(OutputFormat)
            {
                ReadFully = true
            };

            _output = new WasapiOut(device, AudioClientShareMode.Shared, useEventSync: true, latency: 50);
            _output.Init(_mixer);
            _output.Play();
        }
    }

    public void AddVoice(Voice voice)
    {
        lock (_lock)
        {
            _mixer?.AddMixerInput(voice);
        }
    }

    public void RemoveVoice(Voice voice)
    {
        lock (_lock)
        {
            _mixer?.RemoveMixerInput(voice);
        }
    }

    public IEnumerable<Voice> GetActiveVoices()
    {
        lock (_lock)
        {
            if (_mixer == null)
                return Enumerable.Empty<Voice>();

            // MixingSampleProvider doesn't expose inputs directly,
            // so we track voices externally in AudioEngine
            return Enumerable.Empty<Voice>();
        }
    }

    public void Disable()
    {
        lock (_lock)
        {
            Cleanup();
        }
    }

    private void Cleanup()
    {
        if (_output != null)
        {
            try
            {
                _output.Stop();
                _output.Dispose();
            }
            catch
            {
                // Ignore cleanup errors
            }
            _output = null;
        }

        _mixer = null;
        DeviceId = null;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        lock (_lock)
        {
            Cleanup();
        }
    }
}
