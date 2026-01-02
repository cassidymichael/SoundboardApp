# Soundboard App

Windows soundboard for games, Discord, and streaming. Plays sounds via hotkeys to dual audio outputs (monitor + virtual mic).

## Dev Workflow
- **Do not run `dotnet build`** - ask the user to build in Visual Studio instead
- User will test and report any build errors or runtime issues

## Tech Stack
- .NET 10.0 Windows, WPF, MVVM (CommunityToolkit.Mvvm)
- NAudio for WASAPI audio playback
- Win32 RegisterHotKey for global hotkeys
- DI via Microsoft.Extensions.DependencyInjection

## Project Structure
```
SoundboardApp/
├── Models/          # Data: TileConfig, AppConfig, HotkeyBinding
├── Services/        # Business logic: AudioEngine, ConfigService, HotkeyService
├── ViewModels/      # MVVM: MainViewModel, TileViewModel
├── Views/           # WPF: MainWindow, TileControl
├── Audio/           # Voice, OutputBus, FadeEnvelope
└── Interop/         # P/Invoke: NativeMethods, WindowMessageSink
```

## Key Files
- `Services/AudioEngine.cs` - Dual output buses, voice mixing, fade envelopes
- `Services/HotkeyService.cs` - Global hotkey registration via Win32
- `ViewModels/MainViewModel.cs` - Central coordinator, tile/device management
- `Models/TileConfig.cs` - Per-tile settings (sound, hotkey, volume, behavior)

## Architecture
- Config persists to `%AppData%\Soundboard\config.json`
- Sound files referenced by absolute path (not copied)
- Audio decoded on first load to float32/stereo/48kHz, cached in memory
- Max 4 simultaneous voices, 15ms fade-out to prevent clicks
- Close minimizes to tray; Exit via tray menu

## Sound Behavior
- Default: sounds layer (play simultaneously)
- `StopOthers`: this sound cuts other sounds when played
- `Protected`: this sound can't be cut by other sounds

## Docs
- `docs/specification.md` - Features and requirements
- `docs/architecture.md` - Technical design details
