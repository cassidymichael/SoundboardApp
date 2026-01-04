using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Soundboard.Models;
using Soundboard.Services.Interfaces;
using System.Collections.ObjectModel;

namespace Soundboard.ViewModels;

/// <summary>
/// ViewModel for the Sound Picker dialog used when assigning sounds to tiles.
/// </summary>
public partial class SoundPickerViewModel : ObservableObject
{
    private readonly ISoundsLibraryService _libraryService;
    private readonly IAudioCache _audioCache;

    public ObservableCollection<SoundEntryViewModel> Sounds { get; } = new();

    [ObservableProperty]
    private string _searchQuery = "";

    [ObservableProperty]
    private SoundEntryViewModel? _selectedSound;

    /// <summary>
    /// The result of the dialog - the selected sound entry, or null if cancelled/browse file.
    /// </summary>
    public SoundEntry? Result { get; private set; }

    /// <summary>
    /// True if user chose to browse for a file instead of selecting from library.
    /// </summary>
    public bool BrowseFileRequested { get; private set; }

    /// <summary>
    /// Callback to close the dialog with a result.
    /// </summary>
    public Action<bool>? RequestClose { get; set; }

    public SoundPickerViewModel(ISoundsLibraryService libraryService, IAudioCache audioCache)
    {
        _libraryService = libraryService;
        _audioCache = audioCache;
        RefreshSounds();
        _ = PopulateMissingDurationsAsync();
    }

    private void RefreshSounds()
    {
        Sounds.Clear();

        var filtered = _libraryService.Search(
            string.IsNullOrWhiteSpace(SearchQuery) ? null : SearchQuery);

        foreach (var entry in filtered.OrderBy(s => s.DisplayName))
        {
            if (!entry.IsMissing)
            {
                Sounds.Add(new SoundEntryViewModel(entry));
            }
        }
    }

    private async Task PopulateMissingDurationsAsync()
    {
        var soundsNeedingDuration = Sounds
            .Where(s => !s.Entry.DurationSeconds.HasValue)
            .ToList();

        if (soundsNeedingDuration.Count == 0) return;

        await Task.Run(() =>
        {
            foreach (var sound in soundsNeedingDuration)
            {
                try
                {
                    var buffer = _audioCache.GetOrLoad(sound.FilePath);
                    if (buffer != null)
                    {
                        sound.Entry.DurationSeconds = buffer.Duration.TotalSeconds;
                    }
                }
                catch
                {
                    // Ignore errors
                }
            }
        });

        foreach (var sound in soundsNeedingDuration)
        {
            if (sound.Entry.DurationSeconds.HasValue)
            {
                sound.Refresh();
                _libraryService.UpdateSound(sound.Entry);
            }
        }
    }

    partial void OnSearchQueryChanged(string value)
    {
        RefreshSounds();
    }

    [RelayCommand]
    private void Select()
    {
        if (SelectedSound != null)
        {
            Result = SelectedSound.Entry;
            RequestClose?.Invoke(true);
        }
    }

    [RelayCommand]
    private void BrowseFile()
    {
        BrowseFileRequested = true;
        RequestClose?.Invoke(true);
    }

    [RelayCommand]
    private void Cancel()
    {
        Result = null;
        BrowseFileRequested = false;
        RequestClose?.Invoke(false);
    }
}
