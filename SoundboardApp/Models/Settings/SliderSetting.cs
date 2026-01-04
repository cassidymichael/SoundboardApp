using CommunityToolkit.Mvvm.ComponentModel;

namespace Soundboard.Models.Settings;

public partial class SliderSetting : SettingItemBase
{
    [ObservableProperty]
    private double _value;

    public double Minimum { get; init; } = 0;
    public double Maximum { get; init; } = 100;
    public double Step { get; init; } = 1;
    public string ValueFormat { get; init; } = "{0:0}%";

    public required Func<AppConfig, double> Getter { get; init; }
    public required Action<AppConfig, double> Setter { get; init; }

    public Action<double>? OnChanged { get; init; }

    public string FormattedValue => string.Format(ValueFormat, Value);

    partial void OnValueChanged(double value)
    {
        OnPropertyChanged(nameof(FormattedValue));
        OnChanged?.Invoke(value);
    }

    public override void ApplyToConfig(AppConfig config)
    {
        Setter(config, Value);
    }

    public override void LoadFromConfig(AppConfig config)
    {
        Value = Getter(config);
    }
}
