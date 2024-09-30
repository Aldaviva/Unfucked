using ManagedWinapi.Windows;
using System.Collections.Concurrent;
using System.Windows.Automation;
using Timer = System.Threading.Timer;

namespace Unfucked.Windows;

/// <summary>
/// Fire events when a window is opened.
/// </summary>
public interface IWindowOpenedEventEmitter: IDisposable {

    /// <summary>
    /// Triggered when a window is opened, passing the new mwinapi <see cref="SystemWindow"/>.
    /// </summary>
    event EventHandler<SystemWindow>? SystemWindowOpened;

    /// <summary>
    /// Triggered when a window is opened, passing the UI Automation <see cref="AutomationElement"/>.
    /// </summary>
    event EventHandler<AutomationElement>? AutomationElementOpened;

    /// <summary>
    /// Triggered when a window is opened, passing the raw Win32 <see cref="IntPtr"/> window handle.
    /// </summary>
    event EventHandler<IntPtr>? WindowHandleOpened;

}

/// <inheritdoc />
public class WindowOpenedEventEmitter: IWindowOpenedEventEmitter {

    private static readonly TimeSpan CleanUpInterval = TimeSpan.FromMinutes(14);

    /// <inheritdoc />
    public event EventHandler<SystemWindow>? SystemWindowOpened;

    /// <inheritdoc />
    public event EventHandler<AutomationElement>? AutomationElementOpened;

    /// <inheritdoc />
    public event EventHandler<IntPtr>? WindowHandleOpened;

    private readonly ShellHook shellHook;
    private readonly Timer     cleanUpTimer;

    private readonly ConcurrentDictionary<int, Enumerables.ValueHolder<long>> alreadyOpenedWindows = Enumerables.CreateConcurrentDictionary<int, long>();

    /// <summary>
    /// Create a new event emitter that listens for windows being opened using both UI Automation and a Win32 window message processor pump.
    /// </summary>
    public WindowOpenedEventEmitter() {
        shellHook            =  new ShellHook();
        shellHook.ShellEvent += OnWindowOpened;

        Automation.AddAutomationEventHandler(WindowPattern.WindowOpenedEvent, AutomationElement.RootElement, TreeScope.Children, OnWindowOpened);

        cleanUpTimer = new Timer(CleanUpWindowOpeningTimes, null, CleanUpInterval, CleanUpInterval);
    }

    private void OnWindowOpened(object? sender, AutomationEventArgs e) {
        if (sender is AutomationElement windowEl && windowEl.ToHwnd() is { } hwnd && AddWindow(hwnd)) {
            WindowHandleOpened?.Invoke(this, hwnd);
            if (windowEl.ToSystemWindow() is { } systemWindow) {
                SystemWindowOpened?.Invoke(this, systemWindow);
            }
            AutomationElementOpened?.Invoke(this, windowEl);
        }
    }

    private void OnWindowOpened(object? sender, ShellEventArgs args) {
        if (args.Event == ShellEventArgs.ShellEvent.WindowCreated && AddWindow(args.WindowHandle)) {
            WindowHandleOpened?.Invoke(this, args.WindowHandle);
            SystemWindowOpened?.Invoke(this, new SystemWindow(args.WindowHandle));
            if (AutomationElement.FromHandle(args.WindowHandle) is { } automationElement) {
                AutomationElementOpened?.Invoke(this, automationElement);
            }
        }
    }

    /// <summary>
    /// For a given window, cache its opening time.
    /// </summary>
    /// <param name="windowHandle">Win32 window handle</param>
    /// <returns><c>true</c> if this is the first time a window has been opened with this handle, or <c>false</c> if this window has already been opened before because a duplicate window opening event was received.</returns>
    private bool AddWindow(IntPtr windowHandle) {
        DateTime now                  = DateTime.UtcNow;
        long?    previousWindowHandle = alreadyOpenedWindows.Exchange(windowHandle.ToInt32(), now.ToBinary());
        if (previousWindowHandle == null) {
            return true;
        } else {
            DateTime previousOpenTime = DateTime.FromBinary(previousWindowHandle.Value);
            TimeSpan timeDifference   = (now - previousOpenTime).Abs();
            return timeDifference > TimeSpan.FromMinutes(1);
        }
    }

    private void CleanUpWindowOpeningTimes(object? state) {
        DateTime now = DateTime.UtcNow;

        IEnumerable<KeyValuePair<int, Enumerables.ValueHolder<long>>> oldWindowsToCleanUp = alreadyOpenedWindows.Where(pair => DateTime.FromBinary(pair.Value.Value) < now - CleanUpInterval);

        foreach (KeyValuePair<int, Enumerables.ValueHolder<long>> windowToCleanUp in oldWindowsToCleanUp) {
            alreadyOpenedWindows.TryRemove(windowToCleanUp);
        }
    }

    /// <inheritdoc />
    public void Dispose() {
        cleanUpTimer.Dispose();
        shellHook.Dispose();
        Automation.RemoveAutomationEventHandler(WindowPattern.WindowOpenedEvent, AutomationElement.RootElement, OnWindowOpened);
        GC.SuppressFinalize(this);
    }

}