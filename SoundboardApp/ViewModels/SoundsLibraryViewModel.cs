using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Soundboard.Services.Interfaces;
using Soundboard.Views;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows;

namespace Soundboard.ViewModels;

/// <summary>
/// ViewModel for the Sounds Library window.
/// </summary>
public partial class SoundsLibraryViewModel : ObservableObject
{
    private readonly ISoundsLibraryService _libraryService;
    private readonly IAudioCache _audioCache;
    private readonly IAudioEngine _audioEngine;

    public ObservableCollection<SoundEntryViewModel> Sounds { get; } = new();

    [ObservableProperty]
    private string _searchQuery = "";

    [ObservableProperty]
    private SoundEntryViewModel? _selectedSound;

    [ObservableProperty]
    private bool _copyToLibrary = true;

    // Callback for getting the owner window (set by code-behind)
    public Func<Window>? GetOwnerWindow { get; set; }

    public SoundsLibraryViewModel(
        ISoundsLibraryService libraryService,
        IAudioCache audioCache,
        IAudioEngine audioEngine)
    {
        _libraryService = libraryService;
        _audioCache = audioCache;
        _audioEngine = audioEngine;

        _libraryService.LibraryChanged += (_, _) =>
        {
            RefreshSounds();
            _ = PopulateMissingDurationsAsync();
        };

        // Initial load
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
            Sounds.Add(new SoundEntryViewModel(entry));
        }
    }

    private async Task PopulateMissingDurationsAsync()
    {
        // Get sounds with missing durations
        var soundsNeedingDuration = Sounds
            .Where(s => !s.Entry.DurationSeconds.HasValue && !s.IsMissing)
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
                    // Ignore errors loading individual files
                }
            }
        });

        // Update UI and persist
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
    private async Task AddSound()
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "Add Sound to Library",
            Filter = "Audio Files|*.wav;*.mp3;*.ogg;*.flac|All Files|*.*",
            Multiselect = true
        };

        if (dialog.ShowDialog() == true)
        {
            foreach (var filePath in dialog.FileNames)
            {
                await _libraryService.AddSoundAsync(
                    filePath,
                    displayName: null,
                    copyToAppFolder: CopyToLibrary);
            }
        }
    }

    [RelayCommand]
    private void RemoveSound()
    {
        if (SelectedSound == null) return;

        _libraryService.RemoveSound(SelectedSound.Id);
    }

    [RelayCommand]
    private void PlayPreview()
    {
        if (SelectedSound == null || SelectedSound.IsMissing) return;

        var buffer = _audioCache.GetOrLoad(SelectedSound.FilePath);
        if (buffer != null)
        {
            // Update duration if not set
            if (!SelectedSound.Entry.DurationSeconds.HasValue)
            {
                SelectedSound.Entry.DurationSeconds = buffer.Duration.TotalSeconds;
                SelectedSound.Refresh();
                _libraryService.UpdateSound(SelectedSound.Entry);
            }

            // Play preview on monitor only (tile index -1 for preview)
            _audioEngine.Play(-1, buffer, 1.0f, stopOthers: false, isProtected: false);
        }
    }

    [RelayCommand]
    private void StopPreview()
    {
        _audioEngine.StopAll();
    }

    [RelayCommand]
    private void PlayPreviewForItem(SoundEntryViewModel? item)
    {
        if (item == null || item.IsMissing) return;

        var buffer = _audioCache.GetOrLoad(item.FilePath);
        if (buffer != null)
        {
            // Update duration if not set
            if (!item.Entry.DurationSeconds.HasValue)
            {
                item.Entry.DurationSeconds = buffer.Duration.TotalSeconds;
                item.Refresh();
                _libraryService.UpdateSound(item.Entry);
            }

            // Play preview on monitor only
            _audioEngine.Play(-1, buffer, 1.0f, stopOthers: false, isProtected: false);
        }
    }

    [RelayCommand]
    private void RemoveSoundForItem(SoundEntryViewModel? item)
    {
        if (item == null) return;

        var owner = GetOwnerWindow?.Invoke();
        if (owner == null) return;

        var confirmed = ConfirmDialog.Show(
            owner,
            "Remove Sound",
            $"Remove \"{item.DisplayName}\" from the library?",
            item.IsCopied ? "The copied file will also be deleted." : null,
            confirmText: "Remove",
            cancelText: "Cancel",
            isDangerous: true);

        if (confirmed)
        {
            _libraryService.RemoveSound(item.Id);
        }
    }

    [RelayCommand]
    private void OpenLibraryFolder()
    {
        Process.Start("explorer.exe", _libraryService.SoundsFolderPath);
    }

    [RelayCommand]
    private void RemoveAllSounds()
    {
        if (Sounds.Count == 0) return;

        var owner = GetOwnerWindow?.Invoke();
        if (owner == null) return;

        var hasCopied = Sounds.Any(s => s.IsCopied);
        var confirmed = ConfirmDialog.Show(
            owner,
            "Remove All Sounds",
            $"Remove all {Sounds.Count} sounds from the library?",
            hasCopied ? "Copied files will also be deleted from disk." : null,
            confirmText: "Remove All",
            cancelText: "Cancel",
            isDangerous: true);

        if (confirmed)
        {
            // Remove all sounds (iterate over a copy since collection will be modified)
            var soundIds = Sounds.Select(s => s.Id).ToList();
            foreach (var id in soundIds)
            {
                _libraryService.RemoveSound(id);
            }
        }
    }

    [RelayCommand]
    private async Task ValidateFiles()
    {
        await _libraryService.ValidateFilesAsync();
        RefreshSounds();
    }

    public void StartEditingSound(SoundEntryViewModel? item)
    {
        if (item == null) return;
        item.IsEditing = true;
    }

    public void SaveEditingSound(SoundEntryViewModel? item)
    {
        if (item == null) return;
        item.IsEditing = false;
        item.SaveChanges();
        _libraryService.UpdateSound(item.Entry);
    }

    public void CancelEditingSound(SoundEntryViewModel? item)
    {
        if (item == null) return;
        item.IsEditing = false;
        item.Refresh();
    }
}
