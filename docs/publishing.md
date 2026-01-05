# Publishing & Distribution

## Quick Reference

| Method | Command/Action | Output |
|--------|----------------|--------|
| VS Portable | Right-click → Publish → Portable | `publish\Soundboard.exe` |
| Script (all) | `.\publish.ps1` | `dist\` folder |
| Script (portable only) | `.\publish.ps1 -Portable` | `dist\Soundboard-x.x.x-portable.zip` |
| Script (installer only) | `.\publish.ps1 -Installer` | `dist\SoundboardSetup-x.x.x.exe` |

## Distribution Formats

### Portable
Single self-contained `.exe` (~60-80MB). Users can run it anywhere without installing .NET. No installation required - just extract and run.

### Installer
Traditional Windows installer created with Inno Setup. Provides:
- Start menu shortcuts
- Optional desktop shortcut
- Optional "Start with Windows"
- Uninstaller in Add/Remove Programs

## Visual Studio Publish

For quick one-off builds:

1. Right-click `SoundboardApp` project → **Publish**
2. Select **Portable** profile
3. Click **Publish**

Output: `SoundboardApp\publish\Soundboard.exe`

## Publish Script

### Prerequisites
- [Inno Setup 6](https://jrsoftware.org/isdl.php) installed to default location (only needed for installer)

### Usage

```powershell
# Create both portable zip and installer (default)
.\publish.ps1

# Portable zip only (no Inno Setup required)
.\publish.ps1 -Portable

# Installer only
.\publish.ps1 -Installer
```

Version is read automatically from `<Version>` in `SoundboardApp.csproj`.

### Output Files

All outputs go to `dist\` folder:
- `Soundboard-x.x.x-portable.zip` - Portable distribution
- `SoundboardSetup-x.x.x.exe` - Installer

## Build Configuration

The publish is configured for:
- **Self-contained**: Includes .NET runtime (no dependencies for users)
- **Single file**: Everything bundled into one `.exe`
- **win-x64**: 64-bit Windows
- **No PDB**: Debug symbols excluded from output

## Files

| File | Purpose |
|------|---------|
| `publish.ps1` | Automated publish script |
| `installer\soundboard.iss` | Inno Setup configuration |
| `SoundboardApp\Properties\PublishProfiles\Portable.pubxml` | VS publish profile |

## Customizing the Installer

Edit `installer\soundboard.iss`:
- `MyAppPublisher` - Your name/company
- `MyAppURL` - Project URL
- `AppId` - Unique GUID (generate new one for forks)
