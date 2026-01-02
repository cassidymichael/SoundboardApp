using NAudio.Wave;
using Soundboard.Models;

namespace Soundboard.Audio;

public enum VoiceState
{
    Playing,
    FadingOut,
    Stopped
}

public class Voice : ISampleProvider
{
    private readonly AudioBuffer _buffer;
    private readonly FadeEnvelope _fade;
    private long _position;

    public int TileId { get; }
    public float TileVolume { get; set; }
    public float MasterVolume { get; set; }
    public VoiceState State { get; private set; }
    public WaveFormat WaveFormat => _buffer.Format;

    public double Progress => _buffer.Data.Length > 0
        ? (double)_position / _buffer.Data.Length
        : 0;

    public Voice(int tileId, AudioBuffer buffer, float tileVolume, float masterVolume)
    {
        TileId = tileId;
        _buffer = buffer;
        TileVolume = tileVolume;
        MasterVolume = masterVolume;
        _fade = new FadeEnvelope(buffer.Format.SampleRate);
        _position = 0;
        State = VoiceState.Playing;

        // Optional slight fade-in to avoid clicks
        _fade.StartFadeIn(TimeSpan.FromMilliseconds(3));
    }

    public void BeginFadeOut()
    {
        if (State == VoiceState.Playing)
        {
            State = VoiceState.FadingOut;
            _fade.StartFadeOut(TimeSpan.FromMilliseconds(15));
        }
    }

    public void Stop()
    {
        State = VoiceState.Stopped;
    }

    public int Read(float[] buffer, int offset, int count)
    {
        if (State == VoiceState.Stopped)
            return 0;

        int samplesRead = 0;

        while (samplesRead < count && _position < _buffer.Data.Length)
        {
            float sample = _buffer.Data[_position++];

            // Apply fade envelope
            float fadeGain = _fade.GetNextSample();

            // Apply volumes
            sample *= TileVolume * MasterVolume * fadeGain;

            buffer[offset + samplesRead++] = sample;

            // Check if fade-out complete
            if (State == VoiceState.FadingOut && _fade.IsComplete)
            {
                State = VoiceState.Stopped;
                break;
            }
        }

        // Natural end of buffer
        if (_position >= _buffer.Data.Length)
        {
            State = VoiceState.Stopped;
        }

        return samplesRead;
    }
}
