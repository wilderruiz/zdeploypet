using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace ZDeployPet.App;

internal static class NativeWindowChromeRuntime
{
    private const int DwmwaUseImmersiveDarkMode = 20;
    private const int DwmwaBorderColor = 34;
    private const int DwmwaCaptionColor = 35;
    private const int DwmwaTextColor = 36;

    // COLORREF values are 0x00BBGGRR.
    private const int ShellBorderColor = 0x00352B23;  // #232B35
    private const int ShellCaptionColor = 0x00140F0B; // #0B0F14
    private const int ShellTextColor = 0x00F2EDE9;    // #E9EDF2

    public static void Apply(Window window)
    {
        if (window is null) return;

        try
        {
            IntPtr hwnd = new WindowInteropHelper(window).Handle;
            if (hwnd == IntPtr.Zero) return;

            int enabled = 1;
            _ = DwmSetWindowAttribute(hwnd, DwmwaUseImmersiveDarkMode, ref enabled, sizeof(int));

            int caption = ShellCaptionColor;
            _ = DwmSetWindowAttribute(hwnd, DwmwaCaptionColor, ref caption, sizeof(int));

            int text = ShellTextColor;
            _ = DwmSetWindowAttribute(hwnd, DwmwaTextColor, ref text, sizeof(int));

            int border = ShellBorderColor;
            _ = DwmSetWindowAttribute(hwnd, DwmwaBorderColor, ref border, sizeof(int));
        }
        catch (DllNotFoundException)
        {
            // Older/non-DWM environments retain native system chrome.
        }
        catch (EntryPointNotFoundException)
        {
            // Older Windows builds retain native system chrome.
        }
    }

    [DllImport("dwmapi.dll", PreserveSig = true)]
    private static extern int DwmSetWindowAttribute(
        IntPtr hwnd,
        int dwAttribute,
        ref int pvAttribute,
        int cbAttribute);
}
