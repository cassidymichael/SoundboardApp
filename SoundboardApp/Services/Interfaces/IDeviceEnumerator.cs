using Soundboard.Models;

namespace Soundboard.Services.Interfaces;

public interface IDeviceEnumerator : IDisposable
{
    IReadOnlyList<AudioDevice> GetOutputDevices();

    AudioDevice? GetDeviceById(string id);

    AudioDevice? FindVoicemeeterAux();

    event EventHandler? DevicesChanged;
}
