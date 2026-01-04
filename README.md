# Soundboard App

A simple soundboard app for playing sounds into voice chat and streaming software.

![Soundboard App](docs/images/screenshot01.png)

## Download

Download the latest release from the [Releases](https://github.com/cassidymichael/SoundboardApp/releases/) page. This app only works on Windows.

## Features

- Assign sounds to tiles, click tiles to play sound.
- Assign hotkeys to tiles to trigger them while inside other applications.

## Audio Routing Setup

This app outputs to two audio devices simultaneously:
- **Monitor** - Your speakers/headphones (you hear the sounds)
- **Inject** - A virtual audio device (others hear the sounds in calls/streams)

### Why do you need a virtual audio device?

To route soundboard audio into apps like Discord, Teams, or streaming software, you need third-party software that creates a virtual audio device. Use such software to merge your mic audio with the sounds played by this soundboard. I recommend [Voicemeeter Banana](https://vb-audio.com/Voicemeeter/banana.html).


## Usage

### Tile Controls
- **Click tile** - Select for editing
- **Browse** - Choose a sound file
- **Set** - Press a key to assign hotkey
- **Volume slider** - Per-tile volume
- **Stop other sounds** - When enabled, stops any other currently playing tiles when this tile is triggered.
- **Can't be stopped** - When enabled, prevents other tiles' "stop other sounds" control from applying to this tile.

### Global Controls
- **Stop All** - Panic button (stops all sounds)
- **Monitor/Inject dropdowns** - Select output devices
- **Master volume sliders** - Overall output levels
- **Link icon** - Lock both master sliders together

### Configuration

Settings are stored in `%AppData%\Soundboard\config.json`. Sound files are referenced by path (not copied into the app).

---

## Development

### Project Background

I built this as a quick personal tool because existing soundboard apps didn't fit my needs - I wanted something simple to assign hotkeys for a cheap USB macro keypad.

Since this was a personal project, I gave AI significant responsibility in the development process. This was both to speed things up and to explore AI-enhanced software development practices. All code has been reviewed and tested by me, but the AI influence is substantial.

### Building from Source

1. Open `SoundboardApp.slnx` in Visual Studio 2022+
2. Build and run (F5)

### Project Structure

```
SoundboardApp/
├── Models/          # Data models (TileConfig, AppConfig, etc.)
├── ViewModels/      # MVVM ViewModels
├── Views/           # WPF Windows and Controls
├── Services/        # Business logic (Audio, Config, Hotkeys)
├── Audio/           # Audio engine components (Voice, OutputBus)
├── Interop/         # Win32 P/Invoke for global hotkeys
└── Resources/       # Styles and resources
```

### Tech Stack

| Component | Technology |
|-----------|------------|
| Framework | .NET 10.0 Windows |
| UI | WPF |
| MVVM | CommunityToolkit.Mvvm |
| Audio | NAudio (WASAPI) |
| DI | Microsoft.Extensions.DependencyInjection |

### Further Reading

- [docs/specification.md](docs/specification.md) - Features and requirements
- [docs/architecture.md](docs/architecture.md) - Technical design details

## License

MIT License - see [LICENSE](LICENSE) for details.
