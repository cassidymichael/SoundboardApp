namespace Soundboard.Audio;

public class FadeEnvelope
{
    private readonly int _sampleRate;
    private float _currentGain = 1.0f;
    private float _targetGain = 1.0f;
    private float _gainDelta;
    private int _samplesToTarget;
    private int _sampleCounter;

    public bool IsComplete => _samplesToTarget <= 0 || _sampleCounter >= _samplesToTarget;
    public bool IsFadingOut => _targetGain < _currentGain && !IsComplete;

    public FadeEnvelope(int sampleRate)
    {
        _sampleRate = sampleRate;
    }

    public void StartFadeIn(TimeSpan duration)
    {
        _currentGain = 0f;
        _targetGain = 1f;
        _samplesToTarget = (int)(duration.TotalSeconds * _sampleRate);
        _gainDelta = (_targetGain - _currentGain) / Math.Max(1, _samplesToTarget);
        _sampleCounter = 0;
    }

    public void StartFadeOut(TimeSpan duration)
    {
        _targetGain = 0f;
        _samplesToTarget = (int)(duration.TotalSeconds * _sampleRate);
        _gainDelta = (_targetGain - _currentGain) / Math.Max(1, _samplesToTarget);
        _sampleCounter = 0;
    }

    public float GetNextSample()
    {
        if (_sampleCounter < _samplesToTarget)
        {
            _currentGain += _gainDelta;
            _sampleCounter++;
        }
        else
        {
            _currentGain = _targetGain;
        }

        return Math.Clamp(_currentGain, 0f, 1f);
    }

    public void Reset()
    {
        _currentGain = 1.0f;
        _targetGain = 1.0f;
        _gainDelta = 0;
        _samplesToTarget = 0;
        _sampleCounter = 0;
    }
}
