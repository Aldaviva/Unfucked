using ManagedWinapi.Windows;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Runtime.InteropServices;
using System.Windows;

namespace Unfucked;

/// <summary>
/// Methods that make it easier to work with Managed Windows API (mwinapi, https://mwinapi.sourceforge.net).
/// </summary>
public static class WindowsExtensions {

    /// <summary>
    /// Get the base filename, without a file extension, of the executable that is running the process for a window.
    /// </summary>
    /// <param name="window">An mwinapi <see cref="SystemWindow"/> that wraps a Win32 window.</param>
    /// <returns>Executable file basename for the process of <paramref name="window"/>, or <c>null</c> if the process has already exited.</returns>
    [Pure]
    public static string? GetProcessExecutableBasename(this SystemWindow window) {
        try {
            if (GetWindowThreadProcessId(window.HWnd, out int pid) != 0) {
                try {
                    using Process process = Process.GetProcessById(pid);
                    return process.ProcessName;
                } catch (InvalidOperationException) {
                    return null;
                } catch (ArgumentException) {
                    return null;
                }
            }
        } catch (ArgumentException) { }

        return null;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern int GetWindowThreadProcessId(IntPtr hWnd, out int lpdwProcessId);

    /// <summary>
    /// Convert a rectangle from mwinapi to Win32 format.
    /// </summary>
    /// <param name="mwinapiRect">A <see cref="RECT"/> from mwinapi.</param>
    /// <returns>A <see cref="Rect"/> with the same coordinates as <paramref name="mwinapiRect"/>.</returns>
    [Pure]
    public static Rect ToWindowsRect(this RECT mwinapiRect) {
        return new Rect(mwinapiRect.Left, mwinapiRect.Top, mwinapiRect.Width, mwinapiRect.Height);
    }

    /// <summary>
    /// Convert a rectangle from Win32 to mwinapi format.
    /// </summary>
    /// <param name="windowsRect">A <see cref="RECT"/> from Win32.</param>
    /// <returns>A <see cref="RECT"/> with the same coordinates as <paramref name="windowsRect"/>.</returns>
    [Pure]
    public static RECT toMwinapiRect(this Rect windowsRect) {
        return new RECT((int) windowsRect.X, (int) windowsRect.Y, (int) windowsRect.Right, (int) windowsRect.Bottom);
    }

}