using Soundboard.ViewModels;
using System.ComponentModel;
using System.Drawing;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media.Imaging;

namespace Soundboard.Views;

public partial class MainWindow : Window
{
    private bool _isQuitting;

    public MainWindow()
    {
        InitializeComponent();

        // Get ViewModel from DI container
        var app = (App)Application.Current;
        DataContext = app.GetService<MainViewModel>();

        // Handle keyboard input for hotkey learning
        PreviewKeyDown += OnPreviewKeyDown;

        // Create tray icon and window icon
        var icon = CreateTrayIcon();
        TrayIcon.Icon = icon;
        Icon = Imaging.CreateBitmapSourceFromHIcon(
            icon.Handle,
            Int32Rect.Empty,
            BitmapSizeOptions.FromEmptyOptions());
    }

    private static Icon CreateTrayIcon()
    {
        const int size = 32;
        using var bitmap = new Bitmap(size, size);
        using var g = Graphics.FromImage(bitmap);

        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        g.Clear(Color.Transparent);

        // Draw a simple speaker shape
        using var brush = new SolidBrush(Color.FromArgb(70, 130, 180)); // Steel blue
        using var pen = new Pen(Color.White, 1.5f);

        // Speaker body (rectangle)
        var speakerBody = new Rectangle(6, 10, 8, 12);
        g.FillRectangle(brush, speakerBody);
        g.DrawRectangle(pen, speakerBody);

        // Speaker cone (triangle pointing right)
        var cone = new System.Drawing.Point[]
        {
            new(14, 10),
            new(22, 4),
            new(22, 28),
            new(14, 22)
        };
        g.FillPolygon(brush, cone);
        g.DrawPolygon(pen, cone);

        // Sound waves (arcs)
        using var wavePen = new Pen(Color.White, 2f);
        g.DrawArc(wavePen, 22, 8, 8, 16, -60, 120);

        return System.Drawing.Icon.FromHandle(bitmap.GetHicon());
    }

    private void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (DataContext is MainViewModel vm)
        {
            vm.OnKeyPressed(e.Key, Keyboard.Modifiers);
        }
    }

    private void Window_Closing(object sender, CancelEventArgs e)
    {
        if (!_isQuitting)
        {
            // Hide to tray instead of closing
            e.Cancel = true;
            Hide();
        }
    }

    public void ForceClose()
    {
        _isQuitting = true;
        TrayIcon?.Dispose();
        Close();
    }

    public void ShowFromTray()
    {
        Show();
        WindowState = WindowState.Normal;
        Activate();
    }

    private void TrayIcon_Open(object sender, RoutedEventArgs e)
    {
        ShowFromTray();
    }

    private void TrayIcon_Exit(object sender, RoutedEventArgs e)
    {
        _isQuitting = true;
        TrayIcon?.Dispose();
        Application.Current.Shutdown();
    }

    private void TrayIcon_DoubleClick(object sender, RoutedEventArgs e)
    {
        ShowFromTray();
    }
}
