using Soundboard.Interop;
using Soundboard.ViewModels;
using System.Windows;
using System.Windows.Interop;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using Key = System.Windows.Input.Key;
using MouseButtonEventArgs = System.Windows.Input.MouseButtonEventArgs;
using TextBox = System.Windows.Controls.TextBox;

namespace Soundboard.Views;

public partial class SoundsLibraryWindow : Window
{
    public SoundsLibraryWindow()
    {
        InitializeComponent();

        SourceInitialized += (s, e) =>
        {
            var hwnd = new WindowInteropHelper(this).Handle;
            NativeMethods.EnableDarkTitleBar(hwnd);
        };

        Loaded += (s, e) =>
        {
            if (DataContext is SoundsLibraryViewModel vm)
            {
                vm.GetOwnerWindow = () => this;
            }
        };

        KeyDown += (s, e) =>
        {
            if (e.Key == Key.Delete && DataContext is SoundsLibraryViewModel vm && vm.SelectedSound != null)
            {
                vm.RemoveSoundForItemCommand.Execute(vm.SelectedSound);
                e.Handled = true;
            }
        };
    }

    private void SoundItem_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is SoundsLibraryViewModel vm && vm.SelectedSound != null)
        {
            vm.StartEditingSound(vm.SelectedSound);
            e.Handled = true;
        }
    }

    private void EditTextBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (sender is TextBox textBox && textBox.DataContext is SoundEntryViewModel item)
        {
            if (DataContext is SoundsLibraryViewModel vm)
            {
                if (e.Key == Key.Enter)
                {
                    vm.SaveEditingSound(item);
                    e.Handled = true;
                }
                else if (e.Key == Key.Escape)
                {
                    vm.CancelEditingSound(item);
                    e.Handled = true;
                }
            }
        }
    }

    private void EditTextBox_LostFocus(object sender, RoutedEventArgs e)
    {
        if (sender is TextBox textBox && textBox.DataContext is SoundEntryViewModel item)
        {
            if (item.IsEditing && DataContext is SoundsLibraryViewModel vm)
            {
                vm.SaveEditingSound(item);
            }
        }
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
