using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Unfucked.Windows.Power;

/// <summary>
/// Allows you to programmatically kill/cancel/interrupt/stop a running screensaver in Windows.
/// </summary>
public interface IScreensaverKiller {

    /// <summary>
    /// Kill/cancel/interrupt/stop the running screensaver in Windows, or do nothing if the screensaver is not currently running.
    /// </summary>
    void KillScreensaver();

}

/// <inheritdoc />
/// <remarks>By BatteryBackupUnit: <see href="https://stackoverflow.com/a/36292070/979493"/></remarks>
public class ScreensaverKiller: IScreensaverKiller {

    /// <inheritdoc />
    /// <remarks>By BatteryBackupUnit: <see href="https://stackoverflow.com/a/36292070/979493"/></remarks>
    public void KillScreensaver() {
        TurnOnScreenAndResetDisplayIdleTimer();
        InterruptScreensaver();
    }

    /// <summary>
    /// Moves the mouse which turns on a turned-off screen and also resets the 
    /// display idle timer, which is key, because otherwise the 
    /// screen would be turned off again immediately.
    /// </summary>
    private static void TurnOnScreenAndResetDisplayIdleTimer() {
        SendInputNativeMethods.Input input = new() { type = SendInputNativeMethods.SendInputEventType.INPUT_MOUSE };
        try {
            SendInputNativeMethods.SendInput(input);
        } catch (Win32Exception exception) {
            Debug.WriteLine("Could not send mouse move input to turn on display: {0}", exception);
        }
    }

    private static void InterruptScreensaver() {
        try {
            if (ScreensaverNativeMethods.GetScreenSaverRunning()) {
                ScreensaverNativeMethods.KillScreenSaver();
            }

            // activate screen saver again so that after idle-"timeout" it shows again
            ScreensaverNativeMethods.ActivateScreensaver();
        } catch (Win32Exception exception) {
            Debug.WriteLine("Screensaver could not be deactivated: {0}", exception);
        }
    }

    private static class SendInputNativeMethods {

        /// <exception cref="Win32Exception"></exception>
        public static void SendInput(params Input[] inputs) {
            if (SendInput((uint) inputs.Length, inputs, Marshal.SizeOf<Input>()) != (uint) inputs.Length) {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern uint SendInput(uint nInputs, [MarshalAs(UnmanagedType.LPArray)] [In] Input[] pInputs, int cbSize);

        [StructLayout(LayoutKind.Sequential)]
        public struct Input {

            public SendInputEventType           type;
            public MouseKeybdHardwareInputUnion mkhi;

        }

        [StructLayout(LayoutKind.Explicit)]
        public struct MouseKeybdHardwareInputUnion {

            [FieldOffset(0)]
            public MouseInputData mi;

            [FieldOffset(0)]
            public KeybdInput ki;

            [FieldOffset(0)]
            public HardwareInput hi;

        }

        [StructLayout(LayoutKind.Sequential)]
        public struct KeybdInput {

            public ushort wVk;
            public ushort wScan;
            public uint   dwFlags;
            public uint   time;
            public IntPtr dwExtraInfo;

        }

        [StructLayout(LayoutKind.Sequential)]
        public struct HardwareInput {

            public int   uMsg;
            public short wParamL;
            public short wParamH;

        }

#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value - you don't need to move the mouse to kill the screensaver, just send a mouse input event
        public struct MouseInputData {

            public int             Dx;
            public int             Dy;
            public uint            MouseData;
            public MouseEventFlags DwFlags;
            public uint            Time;
            public IntPtr          DwExtraInfo;

        }
#pragma warning restore CS0649 // Field is never assigned to, and will always have its default value

        [Flags]
        public enum MouseEventFlags: uint {

            MOUSEEVENTF_MOVE        = 0x0001,
            MOUSEEVENTF_LEFTDOWN    = 0x0002,
            MOUSEEVENTF_LEFTUP      = 0x0004,
            MOUSEEVENTF_RIGHTDOWN   = 0x0008,
            MOUSEEVENTF_RIGHTUP     = 0x0010,
            MOUSEEVENTF_MIDDLEDOWN  = 0x0020,
            MOUSEEVENTF_MIDDLEUP    = 0x0040,
            MOUSEEVENTF_XDOWN       = 0x0080,
            MOUSEEVENTF_XUP         = 0x0100,
            MOUSEEVENTF_WHEEL       = 0x0800,
            MOUSEEVENTF_VIRTUALDESK = 0x4000,
            MOUSEEVENTF_ABSOLUTE    = 0x8000

        }

        public enum SendInputEventType {

            INPUT_MOUSE,
            INPUT_KEYBOARD,
            INPUT_HARDWARE

        }

    }

    private static class ScreensaverNativeMethods {

        private const int  SPI_GETSCREENSAVERRUNNING = 0x0072;
        private const int  SPI_SETSCREENSAVEACTIVE   = 0x0011;
        private const int  SPIF_SENDWININICHANGE     = 0x0002;
        private const uint DESKTOP_WRITEOBJECTS      = 0x0080;
        private const uint DESKTOP_READOBJECTS       = 0x0001;
        private const int  WM_CLOSE                  = 0x0010;
        private const int  TRUE                      = 1;

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SystemParametersInfo(uint uiAction, uint uiParam, ref IntPtr pvParam, uint fWinIni);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool PostMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern IntPtr OpenDesktop(string lpszDesktop, uint dwFlags, [In] [MarshalAs(UnmanagedType.Bool)] bool fInherit, uint dwDesiredAccess);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool CloseDesktop(IntPtr hDesktop);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool EnumDesktopWindows(IntPtr hDesktop, EnumDesktopWindowsProc callback, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetForegroundWindow();

        private delegate bool EnumDesktopWindowsProc(IntPtr hDesktop, IntPtr lParam);

        /// <exception cref="Win32Exception"></exception>
        public static bool GetScreenSaverRunning() {
            IntPtr isRunning = IntPtr.Zero;

            if (!SystemParametersInfo(SPI_GETSCREENSAVERRUNNING, 0, ref isRunning, 0)) {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }

            return isRunning != IntPtr.Zero;
        }

        public static void ActivateScreensaver() {
            SetScreenSaverActive(TRUE);
        }

        private static void SetScreenSaverActive(uint active) {
            IntPtr nullVar = IntPtr.Zero;

            // Ignoring error since ERROR_OPERATION_IN_PROGRESS is expected.
            // Methode is called to reset timer and to prevent possible errors 
            // as mentioned in Microsoft's Knowledge Base article #140723:
            // http://support.microsoft.com/kb/140723
            SystemParametersInfo(SPI_SETSCREENSAVEACTIVE, active, ref nullVar, SPIF_SENDWININICHANGE);
        }

        // From Microsoft's Knowledge Base article #140723: 
        // http://support.microsoft.com/kb/140723
        // "How to force a screen saver to close once started 
        // in Windows NT, Windows 2000, and Windows Server 2003"
        /// <exception cref="Win32Exception"></exception>
        public static void KillScreenSaver() {
            IntPtr hDesktop = OpenDesktop("Screen-saver", 0, false, DESKTOP_READOBJECTS | DESKTOP_WRITEOBJECTS);
            if (hDesktop != IntPtr.Zero) {
                if (!EnumDesktopWindows(hDesktop, KillScreenSaverFunc, IntPtr.Zero) || !CloseDesktop(hDesktop)) {
                    throw new Win32Exception(Marshal.GetLastWin32Error());
                }
            } else {
                TerminateWindow(GetForegroundWindow());
            }
        }

        /// <exception cref="Win32Exception"></exception>
        private static bool KillScreenSaverFunc(IntPtr hWnd, IntPtr lParam) {
            if (IsWindowVisible(hWnd)) {
                TerminateWindow(hWnd);
            }

            return true;
        }

        /// <exception cref="Win32Exception"></exception>
        private static void TerminateWindow(IntPtr hWnd) {
            if (!PostMessage(hWnd, WM_CLOSE, IntPtr.Zero, IntPtr.Zero)) {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }
        }

    }

}