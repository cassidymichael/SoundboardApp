# Adding New Settings

Guide for adding new settings controls to the Settings UI.

## Quick Start

1. Add property to `AppConfig.cs`
2. Add setting definition in `SettingsViewModel.BuildCategories()`
3. If using Reset to Defaults, update `ResetToDefaultsAsync()`

That's it. The UI renders automatically via DataTemplates.

---

## Setting Types

| Type | Use Case | Key Properties |
|------|----------|----------------|
| `ToggleSetting` | Boolean on/off | `Getter`, `Setter` |
| `SliderSetting` | Numeric range | `Getter`, `Setter`, `Minimum`, `Maximum`, `Step` |
| `ChoiceSetting<T>` | Dropdown selection | `Getter`, `Setter`, `Options` |
| `HotkeySetting` | Hotkey capture | `Getter`, `Setter` |
| `ActionSetting` | Button action | `Action`, `ButtonText` |

---

## Example: Adding a Toggle Setting

### Step 1: Add to AppConfig

```csharp
// Models/AppConfig.cs
public bool MyNewSetting { get; set; } = false;  // default value
```

### Step 2: Add to SettingsViewModel

```csharp
// ViewModels/SettingsViewModel.cs - in BuildCategories()
general.Settings.Add(new ToggleSetting
{
    Key = "MyNewSetting",
    Title = "My new setting",
    Description = "What this setting does",  // optional
    Getter = c => c.MyNewSetting,
    Setter = (c, v) => c.MyNewSetting = v
});
```

### Step 3: Update Reset to Defaults (if applicable)

```csharp
// ViewModels/SettingsViewModel.cs - in ResetToDefaultsAsync()
_configService.Config.MyNewSetting = defaultConfig.MyNewSetting;
```

---

## Example: Adding a Slider Setting

```csharp
category.Settings.Add(new SliderSetting
{
    Key = "FadeOutDuration",
    Title = "Fade out duration",
    Description = "How long sounds take to fade out when stopped",
    Minimum = 0,
    Maximum = 100,
    Step = 5,
    Suffix = "ms",  // displayed after value, e.g. "50ms"
    Getter = c => c.FadeOutDuration,
    Setter = (c, v) => c.FadeOutDuration = v
});
```

---

## Example: Adding a Choice Setting

```csharp
category.Settings.Add(new ChoiceSetting<string>
{
    Key = "Theme",
    Title = "Theme",
    Options = new[] { "Dark", "Light", "System" },
    Getter = c => c.Theme,
    Setter = (c, v) => c.Theme = v
});
```

---

## Setting Dependencies

To disable one setting based on another:

```csharp
var parentSetting = new ToggleSetting { ... };
var childSetting = new ToggleSetting { ... };

// Update child when parent changes
parentSetting.PropertyChanged += (s, e) =>
{
    if (e.PropertyName == nameof(ToggleSetting.Value))
    {
        childSetting.IsEnabled = parentSetting.Value;
        if (!parentSetting.Value)
        {
            childSetting.Value = false;  // also turn off child
        }
    }
};

category.Settings.Add(parentSetting);
category.Settings.Add(childSetting);
```

Then in `InitializeSettingDependencies()`, set the initial state:

```csharp
if (parentSetting != null && childSetting != null)
{
    childSetting.IsEnabled = parentSetting.Value;
}
```

---

## Adding a New Category

```csharp
var newCategory = new SettingsCategory { Name = "Audio" };
newCategory.Settings.Add(new ToggleSetting { ... });
newCategory.Settings.Add(new SliderSetting { ... });
Categories.Add(newCategory);
```

Categories appear in the left navigation in the order they're added.

---

## Special Cases

### Settings Requiring Side Effects

Some settings need immediate action beyond just saving to config:

| Setting | Side Effect | Where Handled |
|---------|-------------|---------------|
| `StartWithWindows` | Update Windows Registry | `UpdateWindowsStartupRegistry()` in `SaveAsync()` |

Add similar handlers in `SaveAsync()` for settings that need external updates.

### Action Settings (Buttons)

Action settings don't persist values - they trigger one-time actions:

```csharp
category.Settings.Add(new ActionSetting
{
    Key = "ClearCache",
    Title = "Clear audio cache",
    Description = "Free memory by clearing cached sounds",
    ButtonText = "Clear",
    Action = async () =>
    {
        await _soundLibrary.ClearCacheAsync();
        // Show confirmation dialog if needed
    }
});
```

---

## Files Reference

| File | Purpose |
|------|---------|
| `Models/AppConfig.cs` | Add new config properties here |
| `Models/Settings/*.cs` | Setting type definitions |
| `ViewModels/SettingsViewModel.cs` | Setting definitions and save logic |
| `Resources/Styles/SettingTemplates.xaml` | UI templates (rarely need changes) |
| `Views/SettingsWindow.xaml` | Window layout (rarely need changes) |
