using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using Soundboard.Models;
using Soundboard.Services.Interfaces;
using System.Collections.Concurrent;
using System.IO;

namespace Soundboard.Services;

public class SoundLibrary : ISoundLibrary
{
    private readonly ConcurrentDictionary<string, AudioBuffer> _cache = new();

    // Target format for all audio: float32, stereo, 48kHz
    private static readonly WaveFormat TargetFormat = WaveFormat.CreateIeeeFloatWaveFormat(48000, 2);

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
        // Preload all sounds in parallel, ignoring any that fail to load
        var tasks = tiles
            .Where(t => !string.IsNullOrEmpty(t.FilePath))
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

        // Read all samples into memory
        var samples = new List<float>();
        var buffer = new float[TargetFormat.SampleRate * TargetFormat.Channels]; // 1 second buffer

        int samplesRead;
        while ((samplesRead = source.Read(buffer, 0, buffer.Length)) > 0)
        {
            for (int i = 0; i < samplesRead; i++)
            {
                samples.Add(buffer[i]);
            }
        }

        return new AudioBuffer(samples.ToArray(), TargetFormat);
    }
}
