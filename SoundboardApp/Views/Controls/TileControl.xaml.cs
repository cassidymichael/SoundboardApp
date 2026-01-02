using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Soundboard.ViewModels;

namespace Soundboard.Views.Controls;

public partial class TileControl : UserControl
{
    private const double MaxAspectRatio = 2.0;

    public TileControl()
    {
        InitializeComponent();
        SizeChanged += OnSizeChanged;
        MouseRightButtonUp += OnRightClick;
        TileButton.Click += OnTileButtonClick;
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
            var source = e.Source as Button;
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
