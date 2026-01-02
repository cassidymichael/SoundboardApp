using Soundboard.ViewModels;
using System.ComponentModel;
using System.Drawing;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using Forms = System.Windows.Forms;

namespace Soundboard.Views;

public partial class MainWindow : Window
{
    private bool _isQuitting;
    private readonly Forms.NotifyIcon _trayIcon;

    public MainWindow()
    {
        InitializeComponent();

        // Get ViewModel from DI container
        var app = (App)System.Windows.Application.Current;
        DataContext = app.GetService<MainViewModel>();

        // Handle keyboard input for hotkey learning
        PreviewKeyDown += OnPreviewKeyDown;

        // Create icon for both tray and window
        var icon = CreateIcon();
        Icon = Imaging.CreateBitmapSourceFromHIcon(
            icon.Handle,
            Int32Rect.Empty,
            BitmapSizeOptions.FromEmptyOptions());

        // Create system tray icon
        _trayIcon = new Forms.NotifyIcon
        {
            Icon = icon,
            Text = "Soundboard",
            Visible = true,
            ContextMenuStrip = CreateTrayMenu()
        };
        _trayIcon.MouseClick += TrayIcon_MouseClick;
    }

    private Forms.ContextMenuStrip CreateTrayMenu()
    {
        var menu = new Forms.ContextMenuStrip();

        var openItem = new Forms.ToolStripMenuItem("Open");
        openItem.Click += (s, e) => Dispatcher.Invoke(ShowFromTray);
        menu.Items.Add(openItem);

        menu.Items.Add(new Forms.ToolStripSeparator());

        var exitItem = new Forms.ToolStripMenuItem("Exit");
        exitItem.Click += (s, e) => Dispatcher.Invoke(ExitApplication);
        menu.Items.Add(exitItem);

        return menu;
    }

    private void TrayIcon_MouseClick(object? sender, Forms.MouseEventArgs e)
    {
        if (e.Button == Forms.MouseButtons.Left)
        {
            Dispatcher.Invoke(ShowFromTray);
        }
    }

    private static Icon CreateIcon()
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

    private void OnPreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
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

    private void ExitApplication()
    {
        _isQuitting = true;
        _trayIcon.Visible = false;
        _trayIcon.Dispose();
        System.Windows.Application.Current.Shutdown();
    }

    public void ForceClose()
    {
        _isQuitting = true;
        _trayIcon.Visible = false;
        _trayIcon.Dispose();
        Close();
    }

    public void ShowFromTray()
    {
        Show();
        WindowState = WindowState.Normal;
        Activate();
    }
}
