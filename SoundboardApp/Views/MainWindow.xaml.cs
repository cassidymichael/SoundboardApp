using Soundboard.ViewModels;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;

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
