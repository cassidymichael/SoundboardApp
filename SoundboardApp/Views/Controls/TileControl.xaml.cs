using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;
using Soundboard.ViewModels;

namespace Soundboard.Views.Controls;

public partial class TileControl : System.Windows.Controls.UserControl
{
    private const double MaxAspectRatio = 2.0;

    public TileControl()
    {
        InitializeComponent();
        SizeChanged += OnSizeChanged;
        MouseRightButtonUp += OnRightClick;
        TileButton.Click += OnTileButtonClick;
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.OldValue is TileViewModel oldVm)
        {
            oldVm.PropertyChanged -= OnViewModelPropertyChanged;
        }

        if (e.NewValue is TileViewModel newVm)
        {
            newVm.PropertyChanged += OnViewModelPropertyChanged;
        }
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is not TileViewModel vm) return;

        // Handle play state changes
        if (e.PropertyName == nameof(TileViewModel.IsPlaying))
        {
            OnIsPlayingChanged(vm.IsPlaying, vm.ClipDurationSeconds);
        }
        // Handle restarts (ClipDurationSeconds is set each time PlayTile is called)
        else if (e.PropertyName == nameof(TileViewModel.ClipDurationSeconds) && vm.IsPlaying)
        {
            OnIsPlayingChanged(true, vm.ClipDurationSeconds);
        }
    }

    private void OnIsPlayingChanged(bool isPlaying, double clipDurationSeconds)
    {
        if (isPlaying && clipDurationSeconds > 0)
        {
            // Start animation directly on the ScaleTransform
            var animation = new DoubleAnimation(0, 1, TimeSpan.FromSeconds(clipDurationSeconds));
            ProgressScale.BeginAnimation(System.Windows.Media.ScaleTransform.ScaleXProperty, animation);
        }
        else
        {
            // Stop animation and reset
            ProgressScale.BeginAnimation(System.Windows.Media.ScaleTransform.ScaleXProperty, null);
            ProgressScale.ScaleX = 0;
        }
    }

    private void OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        // Limit height to 2x width to prevent overly tall tiles
        if (e.NewSize.Width > 0)
        {
            MaxHeight = e.NewSize.Width * MaxAspectRatio;
        }
    }

    private void OnRightClick(object sender, MouseButtonEventArgs e)
    {
        // Right-click always selects the tile for editing
        if (DataContext is TileViewModel vm)
        {
            vm.SelectCommand.Execute(null);
            e.Handled = true;
        }
    }

    private void OnTileButtonClick(object sender, RoutedEventArgs e)
    {
        // Don't handle if click came from a child button (Play/Stop icons)
        // Those buttons have their own Commands
        if (e.OriginalSource is System.Windows.Controls.Primitives.ButtonBase)
        {
            // Click originated from inside a button's template - check if it's a child button
            var source = e.Source as System.Windows.Controls.Button;
            if (source != null && source != TileButton)
            {
                return; // Let the child button's Command handle it
            }
        }

        if (DataContext is not TileViewModel vm) return;

        // Find the MainViewModel to check click mode
        var mainWindow = Window.GetWindow(this);
        if (mainWindow?.DataContext is MainViewModel mainVm)
        {
            if (mainVm.ClickToPlayEnabled && vm.HasSound)
            {
                // Click to play mode - play the sound
                vm.PlayCommand.Execute(null);
            }
            else
            {
                // Edit mode - select the tile
                vm.SelectCommand.Execute(null);
            }
        }
        else
        {
            // Fallback to select if we can't find MainViewModel
            vm.SelectCommand.Execute(null);
        }
    }
}
