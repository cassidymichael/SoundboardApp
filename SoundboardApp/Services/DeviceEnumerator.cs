using NAudio.CoreAudioApi;
using NAudio.CoreAudioApi.Interfaces;
using Soundboard.Models;
using Soundboard.Services.Interfaces;

namespace Soundboard.Services;

public class DeviceEnumerator : IDeviceEnumerator
{
    private readonly MMDeviceEnumerator _enumerator;
    private readonly NotificationClient _notificationClient;
    private bool _disposed;

    public event EventHandler? DevicesChanged;

    public DeviceEnumerator()
    {
        _enumerator = new MMDeviceEnumerator();
        _notificationClient = new NotificationClient(this);
        _enumerator.RegisterEndpointNotificationCallback(_notificationClient);
    }

    public IReadOnlyList<AudioDevice> GetOutputDevices()
    {
        var devices = new List<AudioDevice> { AudioDevice.None, AudioDevice.Default };

        try
        {
            var endpoints = _enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active);
            foreach (var endpoint in endpoints)
            {
                devices.Add(new AudioDevice(endpoint.ID, endpoint.FriendlyName));
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[DeviceEnumerator] Failed to enumerate devices: {ex.Message}");
        }

        return devices;
    }

    public AudioDevice? GetDeviceById(string id)
    {
        if (id == "none")
        {
            return AudioDevice.None;
        }

        if (string.IsNullOrEmpty(id) || id == "default")
        {
            return AudioDevice.Default;
        }

        try
        {
            var device = _enumerator.GetDevice(id);
            if (device != null && device.State == DeviceState.Active)
            {
                return new AudioDevice(device.ID, device.FriendlyName);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[DeviceEnumerator] Failed to get device {id}: {ex.Message}");
        }

        return null;
    }

    public AudioDevice? FindVoicemeeterAux()
    {
        try
        {
            var endpoints = _enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active);
            foreach (var endpoint in endpoints)
            {
                if (endpoint.FriendlyName.Contains("VoiceMeeter Aux", StringComparison.OrdinalIgnoreCase) ||
                    endpoint.FriendlyName.Contains("Voicemeeter AUX", StringComparison.OrdinalIgnoreCase))
                {
                    return new AudioDevice(endpoint.ID, endpoint.FriendlyName);
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[DeviceEnumerator] Failed to find VoiceMeeter: {ex.Message}");
        }

        return null;
    }

    internal MMDevice? GetMMDevice(string? id)
    {
        if (string.IsNullOrEmpty(id) || id == "default")
        {
            try
            {
                return _enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DeviceEnumerator] Failed to get default endpoint: {ex.Message}");
                return null;
            }
        }

        try
        {
            return _enumerator.GetDevice(id);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[DeviceEnumerator] Failed to get MMDevice {id}: {ex.Message}");
            return null;
        }
    }

    private void OnDevicesChanged()
    {
        DevicesChanged?.Invoke(this, EventArgs.Empty);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _enumerator.UnregisterEndpointNotificationCallback(_notificationClient);
        _enumerator.Dispose();
    }

    private class NotificationClient : IMMNotificationClient
    {
        private readonly DeviceEnumerator _parent;

        public NotificationClient(DeviceEnumerator parent)
        {
            _parent = parent;
        }

        public void OnDeviceStateChanged(string deviceId, DeviceState newState)
        {
            _parent.OnDevicesChanged();
        }

        public void OnDeviceAdded(string pwstrDeviceId)
        {
            _parent.OnDevicesChanged();
        }

        public void OnDeviceRemoved(string deviceId)
        {
            _parent.OnDevicesChanged();
        }

        public void OnDefaultDeviceChanged(DataFlow flow, Role role, string defaultDeviceId)
        {
            if (flow == DataFlow.Render)
            {
                _parent.OnDevicesChanged();
            }
        }

        public void OnPropertyValueChanged(string pwstrDeviceId, PropertyKey key)
        {
            // Ignore property changes
        }
    }
}
