using Soundboard.Models;

namespace Soundboard.Services.Interfaces;

public interface IHotkeyService : IDisposable
{
    bool RegisterTileHotkey(int tileIndex, HotkeyBinding binding);
    bool RegisterStopCurrentHotkey(HotkeyBinding binding);
    bool RegisterStopAllHotkey(HotkeyBinding binding);

    void UnregisterTileHotkey(int tileIndex);
    void UnregisterStopCurrentHotkey();
    void UnregisterStopAllHotkey();
    void UnregisterAll();

    void SuspendAll();
    void ResumeAll();

    event EventHandler<int>? TileTriggered;
    event EventHandler? StopCurrentTriggered;
    event EventHandler? StopAllTriggered;
    event EventHandler<string>? RegistrationFailed;
}
