using System.Collections;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Soundboard.Models.Settings;

/// <summary>
/// Non-generic base class for ChoiceSetting that WPF DataTemplates can target.
/// </summary>
public abstract partial class ChoiceSettingBase : SettingItemBase
{
    public abstract IEnumerable OptionsSource { get; }
    public abstract object? SelectedItem { get; set; }
    public abstract void RefreshOptions();
}

/// <summary>
/// Generic choice setting for type-safe dropdown selections.
/// </summary>
public partial class ChoiceSetting<T> : ChoiceSettingBase
{
    [ObservableProperty]
    private T? _selectedValue;

    public ObservableCollection<ChoiceOption<T>> Options { get; } = new();

    public override IEnumerable OptionsSource => Options;

    public override object? SelectedItem
    {
        get => Options.FirstOrDefault(o =>
            (o.Value == null && SelectedValue == null) ||
            (o.Value != null && o.Value.Equals(SelectedValue)));
        set
        {
            if (value is ChoiceOption<T> option)
            {
                SelectedValue = option.Value;
            }
        }
    }

    public required Func<AppConfig, T?> Getter { get; init; }
    public required Action<AppConfig, T?> Setter { get; init; }

    public Action<T?>? OnChanged { get; init; }
    public Func<IEnumerable<ChoiceOption<T>>>? OptionsProvider { get; init; }

    partial void OnSelectedValueChanged(T? value)
    {
        OnPropertyChanged(nameof(SelectedItem));
        OnChanged?.Invoke(value);
    }

    public override void RefreshOptions()
    {
        if (OptionsProvider == null) return;

        var currentValue = SelectedValue;
        Options.Clear();

        foreach (var option in OptionsProvider())
        {
            Options.Add(option);
        }

        // Try to restore the previous selection
        var matching = Options.FirstOrDefault(o =>
            (o.Value == null && currentValue == null) ||
            (o.Value != null && o.Value.Equals(currentValue)));

        SelectedValue = matching != null ? matching.Value : (Options.Count > 0 ? Options[0].Value : default);
    }

    public override void ApplyToConfig(AppConfig config)
    {
        Setter(config, SelectedValue);
    }

    public override void LoadFromConfig(AppConfig config)
    {
        SelectedValue = Getter(config);
    }
}

public record ChoiceOption<T>(T Value, string DisplayName)
{
    public override string ToString() => DisplayName;
}
