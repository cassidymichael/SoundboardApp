# Soundboard App v1 - Specification

## 1. Summary

Windows soundboard application for real-time use in games, Discord, Microsoft Teams, and streaming setups.

**Key Features:**
- 4x4 tile UI (16 sounds) matching a dedicated keypad
- Global hotkeys (F13-F24 pre-mapped externally)
- Dual-output playback:
  - **Inject output**: Voicemeeter AUX Input (routes into microphone/calls)
  - **Monitor output**: System Default (user hears playback)
- Cut policy: default is "cut" (one at a time), max 4 simultaneous voices
- Portable configuration: sounds copied into app's data directory
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
- Stop current / Stop all controls

### Non-Goals (v1)
- No mic ducking/sidechaining
- No per-tab browser routing
- No direct HID/USB device configuration
- No cloud sync or accounts

---

## 3. Key UX Decisions

- **Trigger behavior**: Pressing hotkey always restarts sound from beginning
- **Cut behavior**: New sound stops current with 15ms fade-out
- **Volume model**: Per-tile volume + Master Monitor + Master Inject
- **App lifecycle**: Close hides to tray; Exit via tray menu

---

## 4. Tech Stack

| Component | Choice |
|-----------|--------|
| Framework | .NET 10.0 Windows |
| UI | WPF with MVVM |
| MVVM | CommunityToolkit.Mvvm 8.4.0 |
| Audio | NAudio 2.2.1 (WASAPI shared mode) |
| DI | Microsoft.Extensions.DependencyInjection 8.0.0 |
| Tray | Hardcodet.NotifyIcon.Wpf 1.1.0 |
| Hotkeys | Win32 RegisterHotKey with MOD_NOREPEAT |

---

## 5. Architecture

### Components
1. **UI Layer (WPF)** - Tile grid, editor panel, device dropdowns
2. **ConfigService** - JSON config, sound import/copy
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

**Location:** `%AppData%\Soundboard\`

```
config.json
sounds/
  tile_00/clip.wav
  tile_01/clip.mp3
  ...
```

**config.json contents:**
- App version
- Device IDs (monitor, inject)
- Master volumes
- Hotkey bindings (16 tiles + stop current + stop all)
- Tile configs (name, file path, volume, allow overlap)

---

## 7. Implementation Status

### Completed
- Project structure with DI container
- All models (AppConfig, TileConfig, HotkeyBinding, AudioBuffer, AudioDevice)
- ConfigService with JSON persistence
- DeviceEnumerator with hot-plug notifications
- SoundLibrary with decode/cache
- AudioEngine with dual output buses
- Voice system with fade envelopes
- HotkeyService with Win32 integration
- MainWindow with 4x4 grid and editor panel
- System tray icon with context menu
- Close-to-tray behavior

### Future Enhancements
- Serilog logging integration
- Device fallback with user warnings
- "Restart audio" button
- Large file size warning on import
