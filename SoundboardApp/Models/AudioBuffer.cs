using NAudio.Wave;

namespace Soundboard.Models;

/// <summary>
/// Holds decoded PCM audio data in memory for low-latency playback.
/// </summary>
public class AudioBuffer
{
    public float[] Data { get; }
    public WaveFormat Format { get; }
    public TimeSpan Duration { get; }

    public AudioBuffer(float[] data, WaveFormat format)
    {
        Data = data;
        Format = format;

        // Calculate duration: samples / (sampleRate * channels)
        var totalSamples = data.Length / format.Channels;
        Duration = TimeSpan.FromSeconds((double)totalSamples / format.SampleRate);
    }

    public long LengthInSamples => Data.Length;

    /// <summary>
    /// Estimated memory usage in bytes.
    /// </summary>
    public long MemoryUsageBytes => Data.Length * sizeof(float);
}
