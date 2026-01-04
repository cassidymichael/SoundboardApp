using Soundboard.Interop;
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

        // Enable dark title bar
        SourceInitialized += (s, e) =>
        {
            var hwnd = new WindowInteropHelper(this).Handle;
            NativeMethods.EnableDarkTitleBar(hwnd);
        };

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

        var settingsItem = new Forms.ToolStripMenuItem("Settings");
        settingsItem.Click += (s, e) => Dispatcher.Invoke(ShowSettings);
        menu.Items.Add(settingsItem);

        var aboutItem = new Forms.ToolStripMenuItem("About");
        aboutItem.Click += (s, e) => Dispatcher.Invoke(ShowAbout);
        menu.Items.Add(aboutItem);

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

    public void ShowSettings()
    {
        ShowFromTray();
        var app = (App)System.Windows.Application.Current;
        var settingsVm = app.GetService<ViewModels.SettingsViewModel>();
        var settingsWindow = new SettingsWindow { DataContext = settingsVm, Owner = this };

        // Wire up callbacks
        settingsVm.RequestClose = _ => settingsWindow.Close();
        settingsVm.GetOwnerWindow = () => settingsWindow;

        // Wire up factory reset to restart the app
        settingsVm.OnFactoryReset = () =>
        {
            // Restart the application
            var exePath = Environment.ProcessPath;
            if (!string.IsNullOrEmpty(exePath))
            {
                System.Diagnostics.Process.Start(exePath);
            }
            ForceClose();
        };

        settingsWindow.ShowDialog();
    }

    public void ShowAbout()
    {
        ShowFromTray();
        var aboutWindow = new AboutWindow { Owner = this };
        aboutWindow.ShowDialog();
    }

    private void SettingsButton_Click(object sender, RoutedEventArgs e)
    {
        ShowSettings();
    }

    private void AboutButton_Click(object sender, RoutedEventArgs e)
    {
        ShowAbout();
    }

    private static Icon CreateIcon()
    {
        var assembly = System.Reflection.Assembly.GetExecutingAssembly();
        using var stream = assembly.GetManifestResourceStream("Soundboard.app.ico");
        if (stream != null)
        {
            return new Icon(stream);
        }
        return SystemIcons.Application;
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
            var app = (App)System.Windows.Application.Current;
            var configService = app.GetService<Services.Interfaces.IConfigService>();

            if (configService.Config.CloseToTray)
            {
                // Hide to tray instead of closing
                e.Cancel = true;
                Hide();
            }
            else
            {
                // Full close
                _isQuitting = true;
                _trayIcon.Visible = false;
                _trayIcon.Dispose();
                System.Windows.Application.Current.Shutdown();
            }
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
