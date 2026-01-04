using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using Soundboard.Models;
using Soundboard.Services.Interfaces;
using System.Collections.Concurrent;
using System.IO;

namespace Soundboard.Services;

/// <summary>
/// Manages audio file decoding and caching.
/// Audio files are decoded to a standardized format (float32, stereo, 48kHz) and cached in memory.
/// </summary>
public class AudioCache : IAudioCache
{
    private readonly ConcurrentDictionary<string, AudioBuffer> _cache = new();

    // Target format for all audio: float32, stereo, 48kHz
    private static readonly WaveFormat TargetFormat = WaveFormat.CreateIeeeFloatWaveFormat(48000, 2);

    // Safety cap for preloading - on-demand loading still works beyond this limit
    private const int MaxPreloadCount = 50;

    public AudioBuffer? GetOrLoad(string filePath)
    {
        if (string.IsNullOrEmpty(filePath))
            return null;

        // Check cache first
        if (_cache.TryGetValue(filePath, out var cached))
            return cached;

        // Try to load the file
        try
        {
            var buffer = DecodeFile(filePath);
            _cache.TryAdd(filePath, buffer);
            return buffer;
        }
        catch (FileNotFoundException)
        {
            // File was moved or deleted - return null so caller can handle it
            return null;
        }
    }

    public async Task PreloadAsync(IEnumerable<TileConfig> tiles)
    {
        // Preload sounds in parallel, limited to MaxPreloadCount
        // Additional sounds will load on-demand when played
        var tasks = tiles
            .Where(t => !string.IsNullOrEmpty(t.FilePath))
            .Take(MaxPreloadCount)
            .Select(t => Task.Run(() =>
            {
                try
                {
                    GetOrLoad(t.FilePath!);
                }
                catch
                {
                    // Ignore failures during preload - will be handled when played
                }
            }));

        await Task.WhenAll(tasks);
    }

    public void Invalidate(string filePath)
    {
        _cache.TryRemove(filePath, out _);
    }

    public void ClearCache()
    {
        _cache.Clear();
    }

    /// <summary>
    /// Calculates expected sample count after conversion to target format.
    /// Includes 5% buffer for resampling variance.
    /// </summary>
    private static int CalculateExpectedSampleCount(AudioFileReader reader)
    {
        var durationSeconds = reader.TotalTime.TotalSeconds;
        var expectedSamples = (int)Math.Ceiling(durationSeconds * TargetFormat.SampleRate * TargetFormat.Channels);
        return expectedSamples + (expectedSamples / 20);
    }

    private AudioBuffer DecodeFile(string filePath)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Sound file not found: {filePath}");
        }

        using var reader = new AudioFileReader(filePath);

        // Convert to target format
        ISampleProvider source = reader;

        // Convert to stereo if needed
        if (reader.WaveFormat.Channels == 1)
        {
            source = new MonoToStereoSampleProvider(source);
        }
        else if (reader.WaveFormat.Channels > 2)
        {
            // For multi-channel, just take first two channels
            source = new MultiplexingSampleProvider(new[] { source }, 2);
        }

        // Resample if needed
        if (source.WaveFormat.SampleRate != TargetFormat.SampleRate)
        {
            source = new WdlResamplingSampleProvider(source, TargetFormat.SampleRate);
        }

        // Pre-allocate buffer based on expected sample count (avoids List reallocations)
        int expectedSamples = CalculateExpectedSampleCount(reader);
        var samples = new float[expectedSamples];

        var readBuffer = new float[TargetFormat.SampleRate * TargetFormat.Channels]; // 1 second chunks
        int totalSamplesRead = 0;
        int samplesRead;

        while ((samplesRead = source.Read(readBuffer, 0, readBuffer.Length)) > 0)
        {
            // Expand if resampling produced more than expected (rare edge case)
            if (totalSamplesRead + samplesRead > samples.Length)
            {
                Array.Resize(ref samples, samples.Length + samples.Length / 4);
            }

            Array.Copy(readBuffer, 0, samples, totalSamplesRead, samplesRead);
            totalSamplesRead += samplesRead;
        }

        // Trim to exact size if we over-allocated
        if (totalSamplesRead < samples.Length)
        {
            Array.Resize(ref samples, totalSamplesRead);
        }

        return new AudioBuffer(samples, TargetFormat);
    }
}
