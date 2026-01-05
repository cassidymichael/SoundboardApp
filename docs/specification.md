# Soundboard App v1 - Specification

## 1. Summary

Windows soundboard application for real-time use in games, Discord, Microsoft Teams, and streaming setups.

**Key Features:**
- Customizable tile grid (1-8 rows/columns, default 4x4)
- Global hotkeys (F13-F24 pre-mapped externally)
- Dual-output playback:
  - **Inject output**: Voicemeeter AUX Input (routes into microphone/calls)
  - **Monitor output**: System Default (user hears playback)
- Sound behavior: default is layering (simultaneous), configurable per-tile
- Configuration persists to AppData; sounds referenced by absolute path
- Optional close-to-tray behavior (disabled by default)

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
- **Volume model**: Per-tile volume + Master Monitor + Master Inject (linkable)
- **Device selection**: Monitor and Inject dropdowns; select "None" to disable either output
- **Tile click modes**: Toggle between Play (click to trigger) and Edit (click to select)
- **App lifecycle**: Close quits by default; optionally minimizes to tray if enabled in settings
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

**Export/Import**: Settings > Advanced allows exporting the full configuration to a JSON file for backup or transfer. Import replaces current config and requires app restart.

