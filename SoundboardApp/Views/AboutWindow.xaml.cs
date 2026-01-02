using System.Reflection;
using System.Windows;

namespace Soundboard.Views;

public partial class AboutWindow : Window
{
    public AboutWindow()
    {
        InitializeComponent();

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
}
