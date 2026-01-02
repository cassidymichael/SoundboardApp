using System.Windows;
using System.Windows.Controls;

namespace Soundboard.Views.Controls;

public partial class TileControl : UserControl
{
    private const double MaxAspectRatio = 2.0;

    public TileControl()
    {
        InitializeComponent();
        SizeChanged += OnSizeChanged;
    }

    private void OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        // Limit height to 2x width to prevent overly tall tiles
        if (e.NewSize.Width > 0)
        {
            MaxHeight = e.NewSize.Width * MaxAspectRatio;
        }
    }
}
