# Soundboard App v1 - Specification

## 1. Summary

Windows soundboard application for real-time use in games, Discord, Microsoft Teams, and streaming setups.

**Key Features:**
- 4x4 tile UI (16 sounds) matching a dedicated keypad
- Global hotkeys (F13-F24 pre-mapped externally)
- Dual-output playback:
  - **Inject output**: Voicemeeter AUX Input (routes into microphone/calls)
  - **Monitor output**: System Default (user hears playback)
- Sound behavior: default is layering (simultaneous), configurable per-tile
- Configuration persists to AppData; sounds referenced by absolute path
- Close to tray; quit via tray menu

**Target Environment:**
- Windows 10/11
- Voicemeeter Banana (optional, for mic routing)

---

## 2. Goals and Non-Goals

### Goals (v1)
- Fast, reliable hotkey-to-sound playback
- Low perceived latency (< 50ms)
- Stable device routing with fallbacks
- Simple UI (tile grid + editor panel)
- Stop all control (panic button)

### Non-Goals (v1)
- No mic ducking/sidechaining
- No per-tab browser routing
- No direct HID/USB device configuration
- No cloud sync or accounts

---

## 3. Key UX Decisions

- **Trigger behavior**: Pressing hotkey always restarts sound from beginning
- **Sound behavior**: Sounds layer by default (up to 4 simultaneous)
  - `StopOthers`: This sound cuts other sounds when played
  - `Protected`: This sound can't be cut by other sounds
- **Volume model**: Per-tile volume + Master Monitor + Master Inject
- **Tile click modes**: Toggle between Play (click to trigger) and Edit (click to select)
- **App lifecycle**: Close hides to tray; Exit via tray menu
- **Tray icon**: Left-click opens window, right-click shows menu (Open/Exit)
- **Status messages**: Toast-style, visible 10 seconds then fade out

---

## 4. Tech Stack

| Component | Choice |
|-----------|--------|
| Framework | .NET 10.0 Windows |
| UI | WPF with MVVM |
| MVVM | CommunityToolkit.Mvvm 8.4.0 |
| Audio | NAudio 2.2.1 (WASAPI shared mode) |
| DI | Microsoft.Extensions.DependencyInjection 8.0.0 |
| Tray | System.Windows.Forms.NotifyIcon (native) |
| Hotkeys | Win32 RegisterHotKey with MOD_NOREPEAT |

---

## 5. Architecture

### Components
1. **UI Layer (WPF)** - Tile grid, editor panel, device dropdowns
2. **ConfigService** - JSON config persistence
3. **HotkeyService** - Global hotkey registration
4. **AudioEngine** - Dual output buses (Monitor + Inject)
5. **SoundLibrary** - Decode and cache PCM buffers

### Audio Engine
- Two always-running WasapiOut streams
- Internal format: float32, stereo, 48kHz
- Voice system with fade envelopes
- Max 4 simultaneous voices

---

## 6. Configuration

**Location:** `%AppData%\Soundboard\config.json`

**Contents:**
- App version
- Device IDs (monitor, inject)
- Master volumes
- Hotkey bindings (16 tiles + stop all)
- Tile configs (name, absolute file path, volume, stopOthers, protected)

Note: Sound files are referenced by absolute path (not copied). User manages their own sound file organization.

---

## 7. Implementation Status

### Completed
- Project structure with DI container
- All models (AppConfig, TileConfig, HotkeyBinding, AudioBuffer, AudioDevice)
- ConfigService with JSON persistence
- DeviceEnumerator with hot-plug notifications
- SoundLibrary with decode/cache
- AudioEngine with dual output buses and protected voice support
- Voice system with fade envelopes
- HotkeyService with Win32 integration
- Hotkey reassignment (suspend/resume during learning)
- MainWindow with 4x4 grid and resizable editor panel
- System tray icon (native WinForms) with left-click open, right-click menu
- Close-to-tray behavior
- Toast-style status messages with fade-out
- Sound behavior toggles (StopOthers, Protected)
- Play/Edit mode toggle for tile clicks
- Progress bar animation on playing tiles
- Stop All button with configurable hotkey
- Per-tile stop button (visible during playback)
- Dark mode UI with dark title bars (Windows 10 1809+)

### Future Enhancements
- Serilog logging integration
- Device fallback with user warnings
- "Restart audio" button
- Large file size warning on import
