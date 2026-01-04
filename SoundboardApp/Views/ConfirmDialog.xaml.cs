using Soundboard.Interop;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;

namespace Soundboard.Views;

public partial class ConfirmDialog : Window
{
    public bool Result { get; private set; }

    public ConfirmDialog()
    {
        InitializeComponent();

        SourceInitialized += (s, e) =>
        {
            var hwnd = new WindowInteropHelper(this).Handle;
            NativeMethods.EnableDarkTitleBar(hwnd);
        };

        KeyDown += (s, e) =>
        {
            if (e.Key == Key.Enter)
            {
                Result = true;
                Close();
                e.Handled = true;
            }
            else if (e.Key == Key.Escape)
            {
                Result = false;
                Close();
                e.Handled = true;
            }
        };
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 1)
        {
            DragMove();
        }
    }

    private void ConfirmButton_Click(object sender, RoutedEventArgs e)
    {
        Result = true;
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        Result = false;
        Close();
    }

    /// <summary>
    /// Shows a confirmation dialog with Yes/No or OK/Cancel buttons.
    /// </summary>
    public static bool Show(
        Window owner,
        string title,
        string message,
        string? secondaryMessage = null,
        string confirmText = "OK",
        string cancelText = "Cancel",
        bool showCancel = true,
        bool showIcon = true,
        bool isDangerous = false)
    {
        var dialog = new ConfirmDialog
        {
            Owner = owner,
            DataContext = new ConfirmDialogViewModel
            {
                Title = title,
                Message = message,
                SecondaryMessage = secondaryMessage,
                ConfirmText = confirmText,
                CancelText = cancelText,
                ShowCancel = showCancel,
                ShowIcon = showIcon,
                ConfirmButtonStyle = isDangerous
                    ? (Style)System.Windows.Application.Current.Resources["DangerButtonStyle"]
                    : (Style)System.Windows.Application.Current.Resources["PrimaryButtonStyle"]
            }
        };

        dialog.ShowDialog();
        return dialog.Result;
    }

    /// <summary>
    /// Shows an information dialog with just an OK button.
    /// </summary>
    public static void ShowInfo(Window owner, string title, string message)
    {
        Show(owner, title, message, showCancel: false, showIcon: false);
    }
}

internal class ConfirmDialogViewModel
{
    public string Title { get; set; } = "Confirm";
    public string Message { get; set; } = "";
    public string? SecondaryMessage { get; set; }
    public string ConfirmText { get; set; } = "OK";
    public string CancelText { get; set; } = "Cancel";
    public bool ShowCancel { get; set; } = true;
    public bool ShowIcon { get; set; } = true;
    public Style? ConfirmButtonStyle { get; set; }
}
