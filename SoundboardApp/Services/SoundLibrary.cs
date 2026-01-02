using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using Soundboard.Models;
using Soundboard.Services.Interfaces;
using System.Collections.Concurrent;
using System.IO;

namespace Soundboard.Services;

public class SoundLibrary : ISoundLibrary
{
    private readonly IConfigService _configService;
    private readonly ConcurrentDictionary<string, AudioBuffer> _cache = new();

    // Target format for all audio: float32, stereo, 48kHz
    private static readonly WaveFormat TargetFormat = WaveFormat.CreateIeeeFloatWaveFormat(48000, 2);

    public SoundLibrary(IConfigService configService)
    {
        _configService = configService;
    }

    public AudioBuffer? GetOrLoad(string relativePath)
    {
        if (string.IsNullOrEmpty(relativePath))
            return null;

        return _cache.GetOrAdd(relativePath, path => DecodeFile(path));
    }

    public async Task PreloadAsync(IEnumerable<TileConfig> tiles)
    {
        var tasks = tiles
            .Where(t => !string.IsNullOrEmpty(t.FileRelativePath))
            .Select(t => Task.Run(() => GetOrLoad(t.FileRelativePath!)));

        await Task.WhenAll(tasks);
    }

    public void Invalidate(string relativePath)
    {
        _cache.TryRemove(relativePath, out _);
    }

    public void ClearCache()
    {
        _cache.Clear();
    }

    private AudioBuffer DecodeFile(string relativePath)
    {
        var fullPath = _configService.GetSoundFullPath(relativePath);

        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException($"Sound file not found: {fullPath}");
        }

        using var reader = new AudioFileReader(fullPath);

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
