# Soundboard App

> **Note:** This is a quick personal project. AI (Claude) was heavily used in its creation, though all code has been reviewed and tested by me.

A Windows soundboard application for real-time use in games, Discord, Microsoft Teams, and streaming setups.

## Features

- **4x4 Tile Grid** - 16 configurable sound buttons matching a dedicated keypad layout
- **Global Hotkeys** - Trigger sounds even when the app is not focused (F13-F24 support)
- **Dual Audio Output**
  - **Monitor** - You hear the sound (System Default or custom device)
  - **Inject** - Routes to Voicemeeter/virtual mic for calls and streams
- **Low Latency** - Audio decoded and cached in memory for instant playback
- **Click-Free Playback** - 15ms fade-out prevents audio clicks when stopping sounds
- **System Tray** - Minimizes to tray; right-click to exit

## Requirements

- Windows 10/11
- .NET 10.0 Runtime
- Optional: Voicemeeter Banana for mic routing

## Getting Started

1. Open `SoundboardApp.slnx` in Visual Studio 2022+
2. Build and run (F5)
3. Click a tile to select it
4. Click "Browse" to select a sound file (WAV, MP3, OGG, FLAC)
5. Click "Set" and press a key to assign a hotkey
6. Select output devices from the dropdowns

## Project Structure

```
SoundboardApp/
├── Models/              # Data models (TileConfig, AppConfig, etc.)
├── ViewModels/          # MVVM ViewModels
├── Views/               # WPF Windows and Controls
├── Services/            # Business logic (Audio, Config, Hotkeys)
├── Audio/               # Audio engine components (Voice, OutputBus)
├── Interop/             # Win32 P/Invoke for global hotkeys
└── Resources/           # Styles and resources
```

## Tech Stack

| Component | Technology |
|-----------|------------|
| Framework | .NET 10.0 Windows |
| UI | WPF |
| MVVM | CommunityToolkit.Mvvm |
| Audio | NAudio (WASAPI) |
| DI | Microsoft.Extensions.DependencyInjection |
| Tray Icon | Windows Forms NotifyIcon |

## Configuration

Settings are stored in `%AppData%\Soundboard\config.json`. Sound files are referenced by absolute path (not copied).

## Usage

### Tile Controls
- **Click tile** - Select for editing
- **Browse** - Select a sound file
- **Set** - Capture a hotkey
- **Volume slider** - Per-tile volume

### Global Controls
- **Stop All** - Panic button - stops everything
- **Monitor/Inject dropdowns** - Select output devices
- **Master volume sliders** - Control overall output levels

## License

MIT License - see [LICENSE](LICENSE) for details.
