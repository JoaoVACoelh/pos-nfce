using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Interop;
using System.Windows.Media;

namespace PosNfce.Desktop;

/// <summary>
/// Main window with the header drawn as the title bar (WindowChrome). The code here only covers what the chrome
/// leaves to the app: the window buttons, the maximized margin and the Windows 11 Snap Layouts on maximize.
/// </summary>
public partial class MainWindow : Window
{
    private const int WmNcHitTest = 0x0084;
    private const int WmNcLButtonDown = 0x00A1;
    private const int WmNcLButtonUp = 0x00A2;
    private const int WmNcMouseLeave = 0x02A2;
    private const int HtMaxButton = 9;
    private const int SmCxSizeFrame = 32;
    private const int SmCxPaddedBorder = 92;

    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        StateChanged += (_, _) => UpdateForWindowState();
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        HwndSource.FromHwnd(new WindowInteropHelper(this).Handle).AddHook(WndProc);
        UpdateForWindowState();
    }

    private void UpdateForWindowState()
    {
        var maximized = WindowState == WindowState.Maximized;

        // Maximized, Windows places the frame outside the screen; without this margin the header would be cut.
        var frame = (GetSystemMetrics(SmCxSizeFrame) + GetSystemMetrics(SmCxPaddedBorder)) / VisualTreeHelper.GetDpi(this).DpiScaleX;
        Root.Margin = maximized ? new Thickness(frame) : new Thickness(0);

        MaximizeButton.Content = maximized ? "\uE923" : "\uE922"; // restore / maximize glyphs
        MaximizeButton.ToolTip = maximized ? "Restaurar" : "Maximizar";
        AutomationProperties.SetName(MaximizeButton, (string)MaximizeButton.ToolTip);
    }

    private void OnMinimize(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;

    private void OnMaximizeOrRestore(object sender, RoutedEventArgs e) => ToggleMaximized();

    private void OnClose(object sender, RoutedEventArgs e) => Close();

    private void ToggleMaximized() =>
        WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;

    /// <summary>
    /// Windows 11 only shows Snap Layouts when the hit test answers "maximize button". Then the clicks arrive as
    /// non-client messages instead of WPF mouse events, so they are handled here.
    /// </summary>
    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        switch (msg)
        {
            case WmNcHitTest:
                if (IsOverMaximizeButton(lParam))
                {
                    MaximizeButton.Tag = "Hover";
                    handled = true;
                    return HtMaxButton;
                }
                MaximizeButton.Tag = null;
                break;
            case WmNcMouseLeave:
                MaximizeButton.Tag = null;
                break;
            case WmNcLButtonDown when wParam == HtMaxButton:
                handled = true;
                break;
            case WmNcLButtonUp when wParam == HtMaxButton:
                handled = true;
                ToggleMaximized();
                break;
        }
        return IntPtr.Zero;
    }

    private bool IsOverMaximizeButton(IntPtr lParam)
    {
        if (!MaximizeButton.IsVisible)
            return false;

        var value = lParam.ToInt64();
        var screen = new Point((short)(value & 0xFFFF), (short)((value >> 16) & 0xFFFF));
        var point = MaximizeButton.PointFromScreen(screen);
        return point.X >= 0 && point.Y >= 0 && point.X < MaximizeButton.ActualWidth && point.Y < MaximizeButton.ActualHeight;
    }

    [DllImport("user32.dll")]
    private static extern int GetSystemMetrics(int index);
}
