using System.Collections.ObjectModel;

namespace Soundboard.Models.Settings;

public class SettingsCategory
{
    public required string Name { get; init; }
    public string? Icon { get; init; }
    public ObservableCollection<SettingItemBase> Settings { get; init; } = new();

    public SettingsCategory()
    {
    }

    public SettingsCategory(string name, params SettingItemBase[] settings)
    {
        Name = name;
        foreach (var setting in settings)
        {
            Settings.Add(setting);
        }
    }
}
