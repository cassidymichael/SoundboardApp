using System.Windows;
using System.Windows.Interop;

namespace Soundboard.Interop;

/// <summary>
/// A hidden window that receives Windows messages for hotkey handling.
/// </summary>
public class WindowMessageSink : IDisposable
{
    private readonly HwndSource _hwndSource;
    private bool _disposed;

    public IntPtr Handle => _hwndSource.Handle;

    public event Action<int>? HotkeyPressed;

    public WindowMessageSink()
    {
        var parameters = new HwndSourceParameters("SoundboardMessageSink")
        {
            Width = 0,
            Height = 0,
            WindowStyle = 0,
            ParentWindow = new IntPtr(-3) // HWND_MESSAGE - message-only window
        };

        _hwndSource = new HwndSource(parameters);
        _hwndSource.AddHook(WndProc);
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == NativeMethods.WM_HOTKEY)
        {
            int hotkeyId = wParam.ToInt32();
            HotkeyPressed?.Invoke(hotkeyId);
            handled = true;
        }

        return IntPtr.Zero;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _hwndSource.RemoveHook(WndProc);
        _hwndSource.Dispose();
    }
}
