using Soundboard.Interop;
using System.Diagnostics;
using System.Reflection;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Navigation;

namespace Soundboard.Views;

public partial class AboutWindow : Window
{
    public AboutWindow()
    {
        InitializeComponent();

        SourceInitialized += (s, e) =>
        {
            var hwnd = new WindowInteropHelper(this).Handle;
            NativeMethods.EnableDarkTitleBar(hwnd);
        };

        // Get version from assembly
        var version = Assembly.GetExecutingAssembly().GetName().Version;
        if (version != null)
        {
            VersionText.Text = $"Version {version.Major}.{version.Minor}.{version.Build}";
        }

        // Set copyright year
        CopyrightText.Text = $"© {DateTime.Now.Year}";
    }

    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
    {
        Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
        e.Handled = true;
    }
}
