# Publishing & Distribution

## Quick Reference

| Method | Command/Action | Output |
|--------|----------------|--------|
| VS Portable | Right-click → Publish → Portable | `publish\Soundboard.exe` |
| Script (all) | `.\publish.ps1` | `dist\` folder |
| Script (portable only) | `.\publish.ps1 -Portable` | `dist\Soundboard-x.x.x-portable.zip` |
| Script (installer only) | `.\publish.ps1 -Installer` | `dist\SoundboardSetup-x.x.x.exe` |
| Script + GitHub Release | `.\publish.ps1 -Release` | `dist\` + GitHub release |

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

# Build and create GitHub release
.\publish.ps1 -Release

# With custom release notes
.\publish.ps1 -Release -ReleaseNotes "- Fixed device dropdown issue"
```

Version is read automatically from `<Version>` in `SoundboardApp.csproj`.

### GitHub Releases

The `-Release` flag creates a GitHub release after building:
- Requires [GitHub CLI](https://cli.github.com/) (`gh`) installed and authenticated
- Creates tag `vX.X.X` from the version in csproj
- Uploads both portable zip and installer
- Opens the release page in your browser

First-time setup:
```powershell
# Install GitHub CLI (if not already installed)
winget install GitHub.cli

# Authenticate
gh auth login
```

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
