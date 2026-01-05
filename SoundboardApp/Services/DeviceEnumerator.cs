using NAudio.CoreAudioApi;
using NAudio.CoreAudioApi.Interfaces;
using Soundboard.Models;
using Soundboard.Services.Interfaces;
using System.Runtime.InteropServices;

namespace Soundboard.Services;

public class DeviceEnumerator : IDeviceEnumerator
{
    private MMDeviceEnumerator _enumerator;
    private NotificationClient _notificationClient;
    private readonly object _lock = new();
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
        for (int attempt = 0; attempt < 2; attempt++)
        {
            var devices = new List<AudioDevice> { AudioDevice.None, AudioDevice.Default };

            try
            {
                lock (_lock)
                {
                    var endpoints = _enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active);
                    foreach (var endpoint in endpoints)
                    {
                        devices.Add(new AudioDevice(endpoint.ID, endpoint.FriendlyName));
                    }
                }
                return devices;
            }
            catch (Exception ex) when (attempt == 0 && IsStaleComException(ex))
            {
                System.Diagnostics.Debug.WriteLine($"[DeviceEnumerator] Stale COM detected, recreating: {ex.Message}");
                RecreateEnumerator();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DeviceEnumerator] Failed to enumerate devices: {ex.Message}");
                return devices;
            }
        }

        return new List<AudioDevice> { AudioDevice.None, AudioDevice.Default };
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

        for (int attempt = 0; attempt < 2; attempt++)
        {
            try
            {
                lock (_lock)
                {
                    var device = _enumerator.GetDevice(id);
                    if (device != null && device.State == DeviceState.Active)
                    {
                        return new AudioDevice(device.ID, device.FriendlyName);
                    }
                }
                return null;
            }
            catch (Exception ex) when (attempt == 0 && IsStaleComException(ex))
            {
                System.Diagnostics.Debug.WriteLine($"[DeviceEnumerator] Stale COM detected, recreating: {ex.Message}");
                RecreateEnumerator();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DeviceEnumerator] Failed to get device {id}: {ex.Message}");
                return null;
            }
        }

        return null;
    }

    public AudioDevice? FindVoicemeeterAux()
    {
        for (int attempt = 0; attempt < 2; attempt++)
        {
            try
            {
                lock (_lock)
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
                return null;
            }
            catch (Exception ex) when (attempt == 0 && IsStaleComException(ex))
            {
                System.Diagnostics.Debug.WriteLine($"[DeviceEnumerator] Stale COM detected, recreating: {ex.Message}");
                RecreateEnumerator();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DeviceEnumerator] Failed to find VoiceMeeter: {ex.Message}");
                return null;
            }
        }

        return null;
    }

    internal MMDevice? GetMMDevice(string? id)
    {
        for (int attempt = 0; attempt < 2; attempt++)
        {
            try
            {
                lock (_lock)
                {
                    if (string.IsNullOrEmpty(id) || id == "default")
                    {
                        return _enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
                    }

                    return _enumerator.GetDevice(id);
                }
            }
            catch (Exception ex) when (attempt == 0 && IsStaleComException(ex))
            {
                System.Diagnostics.Debug.WriteLine($"[DeviceEnumerator] Stale COM detected, recreating: {ex.Message}");
                RecreateEnumerator();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DeviceEnumerator] Failed to get MMDevice {id}: {ex.Message}");
                return null;
            }
        }

        return null;
    }

    private static bool IsStaleComException(Exception ex)
    {
        if (ex is COMException comEx)
        {
            return comEx.HResult == unchecked((int)0x800706BE)  // RPC_S_CALL_FAILED
                || comEx.HResult == unchecked((int)0x80070726)  // ERROR_UNKNOWN_PROCEDURE
                || comEx.HResult == unchecked((int)0x80004004)  // E_ABORT
                || comEx.HResult == unchecked((int)0x800706BA)  // RPC_S_SERVER_UNAVAILABLE
                || comEx.HResult == unchecked((int)0x80070490); // ERROR_NOT_FOUND (no default device)
        }
        return false;
    }

    private void RecreateEnumerator()
    {
        lock (_lock)
        {
            try
            {
                _enumerator.UnregisterEndpointNotificationCallback(_notificationClient);
            }
            catch { /* ignore cleanup errors */ }

            try
            {
                _enumerator.Dispose();
            }
            catch { /* ignore cleanup errors */ }

            _enumerator = new MMDeviceEnumerator();
            _notificationClient = new NotificationClient(this);
            _enumerator.RegisterEndpointNotificationCallback(_notificationClient);

            System.Diagnostics.Debug.WriteLine("[DeviceEnumerator] Recreated MMDeviceEnumerator after COM failure");
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

        try
        {
            _enumerator.UnregisterEndpointNotificationCallback(_notificationClient);
        }
        catch { /* ignore cleanup errors */ }

        try
        {
            _enumerator.Dispose();
        }
        catch { /* ignore cleanup errors */ }
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
