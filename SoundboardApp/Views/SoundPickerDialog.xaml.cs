using Soundboard.Interop;
using Soundboard.ViewModels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;

namespace Soundboard.Views;

public partial class SoundPickerDialog : Window
{
    public SoundPickerDialog()
    {
        InitializeComponent();

        SourceInitialized += (s, e) =>
        {
            var hwnd = new WindowInteropHelper(this).Handle;
            NativeMethods.EnableDarkTitleBar(hwnd);
        };

        Loaded += (s, e) =>
        {
            if (DataContext is SoundPickerViewModel vm)
            {
                vm.RequestClose = result =>
                {
                    DialogResult = result;
                    Close();
                };
            }
        };
    }

    private void ListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is SoundPickerViewModel vm && vm.SelectedSound != null)
        {
            vm.SelectCommand.Execute(null);
        }
    }
}
