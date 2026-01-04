# Soundboard App - Architecture

## Project Structure

```
SoundboardApp/
├── Models/           # Data: AppConfig, TileConfig, HotkeyBinding
│   └── Settings/     # Settings UI model classes
├── Services/         # Business logic: AudioEngine, ConfigService, HotkeyService
├── Audio/            # Audio internals: Voice, OutputBus, FadeEnvelope
├── ViewModels/       # MVVM: MainViewModel, TileViewModel, SettingsViewModel
├── Views/            # WPF windows, dialogs, controls
│   ├── Controls/     # Reusable controls (TileControl)
│   └── Converters/   # Value converters
├── Interop/          # Win32 P/Invoke for global hotkeys
└── Resources/Styles/ # XAML styles and templates
```

## Dependency Injection

All services are registered as singletons in `App.xaml.cs`. ViewModels receive dependencies via constructor injection.

## Audio Pipeline

```
Sound File (WAV/MP3)
        ↓
   SoundLibrary.GetOrLoad()
        ↓ (decode to float32/stereo/48kHz)
   AudioBuffer (in-memory PCM)
        ↓
   AudioEngine.Play(stopOthers, isProtected)
        ↓
   ┌─────────────────────────────────────┐
   │  VoicePair (IsProtected flag)       │
   │  ┌─────────────┐  ┌─────────────┐   │
   │  │ MonitorVoice│  │ InjectVoice │   │
   │  └──────┬──────┘  └──────┬──────┘   │
   └─────────┼────────────────┼──────────┘
             ↓                ↓
        MonitorBus       InjectBus
     (MixingSampleProvider)
             ↓                ↓
        WasapiOut         WasapiOut
             ↓                ↓
      Monitor Device    Inject Device
      (you hear it)   (Voicemeeter AUX)
```

## Sound Behavior

Each tile has two independent behavior flags:

| StopOthers | Protected | Behavior |
|------------|-----------|----------|
| false | false | Normal layering (default) - plays alongside others |
| false | true | Background/ambient - layers, immune to being cut |
| true | false | Solo SFX - cuts others, can be cut |
| true | true | Priority - cuts others, immune to being cut |

- `FadeOutAllUnprotected()` skips voices with `IsProtected=true`
- Voice cap (4 max) prefers killing unprotected voices first

## Hotkey System

```
Physical Keypress
        ↓
   Windows OS
        ↓
   WM_HOTKEY message
        ↓
   WindowMessageSink (hidden HWND_MESSAGE window)
        ↓
   HotkeyService.OnHotkeyPressed()
        ↓
   ┌─────────────────────────────────────┐
   │ TileTriggered event                 │
   │ StopAllTriggered event              │
   └─────────────────────────────────────┘
        ↓
   MainViewModel handles event
        ↓
   AudioEngine.Play() / StopAll()
```

### Hotkey Learning Mode
When user clicks "Set" to assign a hotkey:
1. `SuspendAll()` temporarily unregisters all hotkeys
2. Window captures keypress via `PreviewKeyDown`
3. If key already assigned to another tile, that tile is unbound
4. `ResumeAll()` re-registers hotkeys with new assignment
5. This allows reassigning keys already in use by other tiles

## Key Design Decisions

### 1. Decode on First Load
Audio files are decoded to float32/stereo/48kHz on first access (via SoundLibrary). Files are referenced by absolute path and cached in memory. This ensures:
- Consistent format for mixing
- No decode latency during playback (after first load)
- Memory tradeoff acceptable for 16 short clips

### 2. Voice Pairs
Each playback creates two Voice instances (monitor + inject) tracked together. This ensures:
- Synchronized playback to both outputs
- Consistent fade-out behavior
- Proper cleanup when sound ends

### 3. Fade Envelopes
Every voice has a FadeEnvelope that provides:
- 3ms fade-in to avoid click at start
- 15ms fade-out when stopped
- Smooth transitions without audio artifacts

### 4. Global Hotkeys via RegisterHotKey
Using Win32 RegisterHotKey instead of low-level hooks:
- Simpler implementation
- MOD_NOREPEAT flag prevents auto-repeat
- Additional 100ms debounce for safety
- Works in most games (some may block)

### 5. Close to Tray (Optional)
Controlled by `AppConfig.CloseToTray` setting (default: false):
- **Enabled**: Window close → Hide() to tray, Exit via tray menu
- **Disabled**: Window close → Application.Current.Shutdown()

ShutdownMode="OnExplicitShutdown" allows both behaviors.
