using Soundboard.Interop;
using Soundboard.Models;
using Soundboard.Services.Interfaces;
using System.Runtime.InteropServices;
using System.Windows.Input;

namespace Soundboard.Services;

public class HotkeyService : IHotkeyService
{
    private readonly WindowMessageSink _messageSink;
    private readonly Dictionary<int, HotkeyBinding> _registeredHotkeys = new();
    private readonly Dictionary<int, DateTime> _lastTriggerTime = new();
    private readonly Dictionary<int, int> _tileHotkeyIds = new(); // tileIndex -> hotkeyId
    private int _stopCurrentId = -1;
    private int _stopAllId = -1;

    private const int DebounceMs = 100;
    private const int TileHotkeyIdBase = 1000;
    private const int StopCurrentHotkeyId = 100;
    private const int StopAllHotkeyId = 101;

    public event EventHandler<int>? TileTriggered;
    public event EventHandler? StopCurrentTriggered;
    public event EventHandler? StopAllTriggered;
    public event EventHandler<string>? RegistrationFailed;

    private bool _disposed;

    public HotkeyService()
    {
        _messageSink = new WindowMessageSink();
        _messageSink.HotkeyPressed += OnHotkeyPressed;
    }

    public bool RegisterTileHotkey(int tileIndex, HotkeyBinding binding)
    {
        UnregisterTileHotkey(tileIndex);

        int id = TileHotkeyIdBase + tileIndex;
        if (RegisterHotkeyInternal(id, binding))
        {
            _tileHotkeyIds[tileIndex] = id;
            return true;
        }
        return false;
    }

    public bool RegisterStopCurrentHotkey(HotkeyBinding binding)
    {
        UnregisterStopCurrentHotkey();

        if (RegisterHotkeyInternal(StopCurrentHotkeyId, binding))
        {
            _stopCurrentId = StopCurrentHotkeyId;
            return true;
        }
        return false;
    }

    public bool RegisterStopAllHotkey(HotkeyBinding binding)
    {
        UnregisterStopAllHotkey();

        if (RegisterHotkeyInternal(StopAllHotkeyId, binding))
        {
            _stopAllId = StopAllHotkeyId;
            return true;
        }
        return false;
    }

    public void UnregisterTileHotkey(int tileIndex)
    {
        if (_tileHotkeyIds.TryGetValue(tileIndex, out int id))
        {
            UnregisterHotkeyInternal(id);
            _tileHotkeyIds.Remove(tileIndex);
        }
    }

    public void UnregisterStopCurrentHotkey()
    {
        if (_stopCurrentId >= 0)
        {
            UnregisterHotkeyInternal(_stopCurrentId);
            _stopCurrentId = -1;
        }
    }

    public void UnregisterStopAllHotkey()
    {
        if (_stopAllId >= 0)
        {
            UnregisterHotkeyInternal(_stopAllId);
            _stopAllId = -1;
        }
    }

    public void UnregisterAll()
    {
        foreach (var id in _registeredHotkeys.Keys.ToList())
        {
            UnregisterHotkeyInternal(id);
        }
        _tileHotkeyIds.Clear();
        _stopCurrentId = -1;
        _stopAllId = -1;
    }

    private readonly Dictionary<int, HotkeyBinding> _suspendedHotkeys = new();

    public void SuspendAll()
    {
        // Temporarily unregister all hotkeys (for learning mode)
        _suspendedHotkeys.Clear();
        foreach (var kvp in _registeredHotkeys.ToList())
        {
            _suspendedHotkeys[kvp.Key] = kvp.Value;
            NativeMethods.UnregisterHotKey(_messageSink.Handle, kvp.Key);
        }
        _registeredHotkeys.Clear();
    }

    public void ResumeAll()
    {
        // Re-register all suspended hotkeys
        foreach (var kvp in _suspendedHotkeys)
        {
            RegisterHotkeyInternal(kvp.Key, kvp.Value);
        }
        _suspendedHotkeys.Clear();
    }

    private bool RegisterHotkeyInternal(int id, HotkeyBinding binding)
    {
        uint modifiers = ConvertModifiers(binding.Modifiers);
        uint vk = (uint)KeyInterop.VirtualKeyFromKey(binding.Key);

        // Add MOD_NOREPEAT to prevent auto-repeat
        modifiers |= NativeMethods.MOD_NOREPEAT;

        bool success = NativeMethods.RegisterHotKey(_messageSink.Handle, id, modifiers, vk);

        if (success)
        {
            _registeredHotkeys[id] = binding;
            return true;
        }
        else
        {
            int error = Marshal.GetLastWin32Error();
            string message = error == NativeMethods.ERROR_HOTKEY_ALREADY_REGISTERED
                ? $"Hotkey {binding.GetDisplayString()} is already in use by another application"
                : $"Failed to register hotkey {binding.GetDisplayString()}: error {error}";

            RegistrationFailed?.Invoke(this, message);
            return false;
        }
    }

    private void UnregisterHotkeyInternal(int id)
    {
        NativeMethods.UnregisterHotKey(_messageSink.Handle, id);
        _registeredHotkeys.Remove(id);
        _lastTriggerTime.Remove(id);
    }

    private void OnHotkeyPressed(int id)
    {
        // Debounce check (in addition to MOD_NOREPEAT for extra safety)
        if (_lastTriggerTime.TryGetValue(id, out var lastTime))
        {
            if ((DateTime.UtcNow - lastTime).TotalMilliseconds < DebounceMs)
                return;
        }
        _lastTriggerTime[id] = DateTime.UtcNow;

        // Determine which hotkey was pressed
        if (id == _stopCurrentId)
        {
            StopCurrentTriggered?.Invoke(this, EventArgs.Empty);
        }
        else if (id == _stopAllId)
        {
            StopAllTriggered?.Invoke(this, EventArgs.Empty);
        }
        else if (id >= TileHotkeyIdBase)
        {
            int tileIndex = id - TileHotkeyIdBase;
            TileTriggered?.Invoke(this, tileIndex);
        }
    }

    private static uint ConvertModifiers(ModifierKeys modifiers)
    {
        uint result = NativeMethods.MOD_NONE;

        if (modifiers.HasFlag(ModifierKeys.Alt))
            result |= NativeMethods.MOD_ALT;
        if (modifiers.HasFlag(ModifierKeys.Control))
            result |= NativeMethods.MOD_CONTROL;
        if (modifiers.HasFlag(ModifierKeys.Shift))
            result |= NativeMethods.MOD_SHIFT;
        if (modifiers.HasFlag(ModifierKeys.Windows))
            result |= NativeMethods.MOD_WIN;

        return result;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        UnregisterAll();
        _messageSink.Dispose();
    }
}
