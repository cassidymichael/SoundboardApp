using CommunityToolkit.Mvvm.ComponentModel;
using Soundboard.Models;

namespace Soundboard.ViewModels;

/// <summary>
/// ViewModel wrapper for a SoundEntry in the Sounds Library.
/// </summary>
public partial class SoundEntryViewModel : ObservableObject
{
    private readonly SoundEntry _entry;

    public string Id => _entry.Id;
    public string FilePath => _entry.FilePath;
    public bool IsCopied => _entry.IsCopied;
    public DateTime DateAdded => _entry.DateAdded;

    [ObservableProperty]
    private string _displayName = "";

    [ObservableProperty]
    private string _durationDisplay = "";

    [ObservableProperty]
    private bool _isMissing;

    [ObservableProperty]
    private bool _isSelected;

    [ObservableProperty]
    private bool _isEditing;

    public SoundEntry Entry => _entry;

    public SoundEntryViewModel(SoundEntry entry)
    {
        _entry = entry;
        DisplayName = entry.DisplayName;
        IsMissing = entry.IsMissing;
        UpdateDurationDisplay();
    }

    /// <summary>
    /// Source indicator for display.
    /// </summary>
    public string SourceDisplay => _entry.IsCopied ? "Stored" : "Reference";

    private void UpdateDurationDisplay()
    {
        if (_entry.DurationSeconds.HasValue)
        {
            var duration = TimeSpan.FromSeconds(_entry.DurationSeconds.Value);
            if (duration.TotalMinutes >= 1)
            {
                DurationDisplay = $"{(int)duration.TotalMinutes}:{duration.Seconds:D2}";
            }
            else
            {
                DurationDisplay = $"{duration.TotalSeconds:F1}s";
            }
        }
        else
        {
            DurationDisplay = "-";
        }
    }

    /// <summary>
    /// Updates the model with changes from the ViewModel.
    /// </summary>
    public void SaveChanges()
    {
        _entry.DisplayName = DisplayName;
    }

    /// <summary>
    /// Refreshes the ViewModel from the model.
    /// </summary>
    public void Refresh()
    {
        DisplayName = _entry.DisplayName;
        IsMissing = _entry.IsMissing;
        UpdateDurationDisplay();
    }

    partial void OnDisplayNameChanged(string value)
    {
        _entry.DisplayName = value;
    }
}
