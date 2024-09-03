using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Unfucked.Screensaver;

public interface IScreensaverManager {

    void KillScreensaver();

}

// https://stackoverflow.com/a/36292070/979493
public class ScreensaverManager: IScreensaverManager {

    public void KillScreensaver() => turnOnScreenAndInterruptScreensaver();

    private void turnOnScreenAndInterruptScreensaver() {
        turnOnScreenAndResetDisplayIdleTimer();
        InterruptScreensaver();
    }

    /// <summary>
    /// Moves the mouse which turns on a turned-off screen and also resets the 
    /// display idle timer, which is key, because otherwise the 
    /// screen would be turned off again immediately.
    /// </summary>
    private void turnOnScreenAndResetDisplayIdleTimer() {
        SendInputNativeMethods.Input input = new() { type = SendInputNativeMethods.SendInputEventType.InputMouse };
        try {
            SendInputNativeMethods.SendInput(input);
        } catch (Win32Exception exception) {
            Debug.WriteLine("Could not send mouse move input to turn on display: {0}", exception);
        }
    }

    private void InterruptScreensaver() {
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

        public struct MouseInputData {

            public int             Dx;
            public int             Dy;
            public uint            MouseData;
            public MouseEventFlags DwFlags;
            public uint            Time;
            public IntPtr          DwExtraInfo;

        }

        [Flags]
        public enum MouseEventFlags: uint {

            MouseeventfMove        = 0x0001,
            MouseeventfLeftdown    = 0x0002,
            MouseeventfLeftup      = 0x0004,
            MouseeventfRightdown   = 0x0008,
            MouseeventfRightup     = 0x0010,
            MouseeventfMiddledown  = 0x0020,
            MouseeventfMiddleup    = 0x0040,
            MouseeventfXdown       = 0x0080,
            MouseeventfXup         = 0x0100,
            MouseeventfWheel       = 0x0800,
            MouseeventfVirtualdesk = 0x4000,
            MouseeventfAbsolute    = 0x8000

        }

        public enum SendInputEventType {

            InputMouse,
            InputKeyboard,
            InputHardware

        }

    }

    private static class ScreensaverNativeMethods {

        private const int  SpiGetscreensaverrunning = 0x0072;
        private const int  SpiSetscreensaveactive   = 0x0011;
        private const int  SpifSendwininichange     = 0x0002;
        private const uint DesktopWriteobjects      = 0x0080;
        private const uint DesktopReadobjects       = 0x0001;
        private const int  WmClose                  = 0x0010;
        private const int  True                     = 1;

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

            if (!SystemParametersInfo(SpiGetscreensaverrunning, 0, ref isRunning, 0)) {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }

            return isRunning != IntPtr.Zero;
        }

        public static void ActivateScreensaver() {
            SetScreenSaverActive(True);
        }

        private static void SetScreenSaverActive(uint active) {
            IntPtr nullVar = IntPtr.Zero;

            // Ignoring error since ERROR_OPERATION_IN_PROGRESS is expected.
            // Methode is called to reset timer and to prevent possible errors 
            // as mentioned in Microsoft's Knowledge Base article #140723:
            // http://support.microsoft.com/kb/140723
            SystemParametersInfo(SpiSetscreensaveactive, active, ref nullVar, SpifSendwininichange);
        }

        // From Microsoft's Knowledge Base article #140723: 
        // http://support.microsoft.com/kb/140723
        // "How to force a screen saver to close once started 
        // in Windows NT, Windows 2000, and Windows Server 2003"
        /// <exception cref="Win32Exception"></exception>
        public static void KillScreenSaver() {
            IntPtr hDesktop = OpenDesktop("Screen-saver", 0, false, DesktopReadobjects | DesktopWriteobjects);
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
            if (!PostMessage(hWnd, WmClose, IntPtr.Zero, IntPtr.Zero)) {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }
        }

    }

}