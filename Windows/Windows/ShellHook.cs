using System.Runtime.InteropServices;

namespace Unfucked.Windows;

internal class ShellHook: Form {

    public event EventHandler<ShellEventArgs>? ShellEvent;

    private readonly uint subscriptionId;

    public ShellHook() {
        subscriptionId = RegisterWindowMessage("SHELLHOOK");
        RegisterShellHookWindow(Handle);
    }

    protected override void WndProc(ref Message message) {
        if (message.Msg == subscriptionId) {
            ShellEvent?.Invoke(this, new ShellEventArgs(shellEvent: (ShellEventArgs.ShellEvent) message.WParam.ToInt32(), windowHandle: message.LParam));
        }

        base.WndProc(ref message);
    }

    protected override void Dispose(bool disposing) {
        DeregisterShellHookWindow(Handle);
        base.Dispose(disposing);
    }

    [DllImport("user32.dll", EntryPoint = "RegisterWindowMessageW", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern uint RegisterWindowMessage(string lpString);

    [DllImport("user32.dll")]
    private static extern bool DeregisterShellHookWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool RegisterShellHookWindow(IntPtr hWnd);

}

internal class ShellEventArgs: EventArgs {

    public readonly ShellEvent Event;
    public readonly IntPtr     WindowHandle;

    public ShellEventArgs(ShellEvent shellEvent, IntPtr windowHandle) {
        Event        = shellEvent;
        WindowHandle = windowHandle;
    }

    public enum ShellEvent {

        WINDOW_CREATED = 1,
        WINDOW_DESTROYED,
        ACTIVATE_SHELL_WINDOW,
        WINDOW_ACTIVATED,
        GET_MIN_RECT,
        REDRAW,
        TASKMAN,
        LANGUAGE,
        ACCESSIBILITY_STATE = 11,
        APP_COMMAND

    }

}