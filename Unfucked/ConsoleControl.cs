using JetBrains.Annotations;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;
#if NET8_0_OR_GREATER
using System.Runtime.InteropServices.Marshalling;
#endif

namespace Unfucked;

/*
 * Reference: https://en.wikipedia.org/wiki/ANSI_escape_code#SGR_(Select_Graphic_Rendition)_parameters
 */
/// <summary>
/// <para>Methods that make it easier to control the text console.</para>
/// <para>Colored, interpolated strings can be made more terse with <c>using static Unfucked.ConsoleControl;</c></para>
/// </summary>
public static partial class ConsoleControl {

    private const string KERNEL32           = "kernel32.dll";
    private const string TASKBAR_LIST_CLSID = "56FDF344-FD6D-11d0-958A-006097C9A090";

    private static readonly IntPtr              INVALID_HANDLE_VALUE = new(-1);
    private static readonly bool                IS_WINDOWS_TERMINAL  = Environment.GetEnvironmentVariable("WT_SESSION", EnvironmentVariableTarget.Process) is not null;
    private static readonly Lazy<ITaskbarList3> TASKBAR_LIST         = new(GetTaskbarList, LazyThreadSafetyMode.ExecutionAndPublication);
    private static readonly Lazy<IntPtr>        MAIN_WINDOW          = new(GetConsoleWindow, LazyThreadSafetyMode.PublicationOnly);

    private static CancellationTokenSource? completeProgressBarLingerCancellation;

    /// <summary>
    /// The color that the console would have used if you never changed its color. For example, this can be used to reset the text color back to its default color (like white) after changing it to blue.
    /// </summary>
    /// <seealso cref="ResetColor"/>
    public const ConsoleColor DefaultColor = (ConsoleColor) (-1);

    private static VirtualTerminalProcessing virtualTerminalProcessingState =
        Environment.OSVersion.Platform == PlatformID.Win32NT ? VirtualTerminalProcessing.Disabled : VirtualTerminalProcessing.Enabled;

    /// <summary>
    /// Write this string to reset the foreground and background text colors to their default values.
    /// </summary>
    public const string ResetColor = "\e[39;49m";

    /// <inheritdoc cref="WriteClear" />
    public const string Clear = "\e[1J\e[1;1H";

    /// <inheritdoc cref="WriteClearLine" />
    public const string ClearLine = "\r\e[2K";

    /// <summary>
    /// Clear screen and move to the top-left position.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public static void WriteClear() {
        if (EnableColorSupport()) {
            Console.Write(Clear);
        } else {
            Console.Clear();
        }
    }

    /// <summary>
    /// Clear the line and move to the leftmost position.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public static void WriteClearLine() {
        if (EnableColorSupport()) {
            Console.Write(ClearLine);
        } else {
            Console.WriteLine();
        }
    }

    /// <summary>
    /// Get ANSI control characters to change the foreground and background colors.
    /// </summary>
    /// <param name="foregroundColor">Foreground text color, or <c>null</c> to leave unchanged.</param>
    /// <param name="backgroundColor">Background color, or <c>null</c> to leave unchanged.</param>
    /// <returns>An ANSI escape sequence that changes the text and background color, if specified.</returns>
    [System.Diagnostics.Contracts.Pure]
    public static string Color(TerminalColor? foregroundColor, TerminalColor? backgroundColor = null) =>
        EnableColorSupport() && (foregroundColor is not null || backgroundColor is not null)
            ? $"\e[{foregroundColor.ToAnsiEscapeCode(true)}{(foregroundColor != null && backgroundColor != null ? ";" : "")}{backgroundColor.ToAnsiEscapeCode(false)}m"
            : string.Empty;

    /// <summary>
    /// Wrap text with ANSI control characters to change the foreground and background colors, and then reset it to the default colors at the end of the string.
    /// </summary>
    /// <param name="text">Text to wrap with color</param>
    /// <param name="foregroundColor">Foreground text color, or <c>null</c> to leave unchanged.</param>
    /// <param name="backgroundColor">Background color, or <c>null</c> to leave unchanged.</param>
    /// <returns>An ANSI escape sequence that changes the text and background color, followed by <paramref name="text"/>, followed by the ANSI escape sequence to reset colors back to the console defaults.</returns>
    [System.Diagnostics.Contracts.Pure]
    public static string Color(string text, TerminalColor? foregroundColor, TerminalColor? backgroundColor = null) =>
        Color(foregroundColor, backgroundColor) + text + ResetColor;

    /// <summary>
    /// Print colored text to the console. After calling this method, the console will be reset to its default colors for future text to be printed.
    /// </summary>
    /// <param name="text">Text to print in color.</param>
    /// <param name="foregroundColor">Foreground text color, or <c>null</c> to leave unchanged.</param>
    /// <param name="backgroundColor">Background color, or <c>null</c> to leave unchanged.</param>
    [ExcludeFromCodeCoverage]
    public static void Write(string text, TerminalColor? foregroundColor, TerminalColor? backgroundColor = null) =>
        Console.Write(Color(text, foregroundColor, backgroundColor));

    /// <summary>
    /// Print colored text to the console, followed by a line break. After calling this method, the console will be reset to its default colors for future text to be printed.
    /// </summary>
    /// <param name="text">Text to print in color</param>
    /// <param name="foregroundColor">Foreground text color, or <c>null</c> to leave unchanged.</param>
    /// <param name="backgroundColor">Background color, or <c>null</c> to leave unchanged.</param>
    [ExcludeFromCodeCoverage]
    public static void WriteLine(string text, TerminalColor? foregroundColor, TerminalColor? backgroundColor = null) =>
        Console.WriteLine(Color(text, foregroundColor, backgroundColor));

    /// <summary>
    /// <para>On Windows, you have to call a method to explicitly turn on ANSI escape sequence processing, otherwise you will see the raw escape codes printed as text.</para>
    /// <para>Requires Windows 10 1511 or later.</para>
    /// <para>This is automatically called by all of the following methods: <see cref="ConsoleControl"/><c>.Write*</c>, <c>Color</c>, and <c>RenderProgressBar</c>.</para>
    /// <para>On other operating systems, such as Linux, calling this method always returns <c>true</c> and has no side effects.</para>
    /// </summary>
    /// <remarks><see href="https://learn.microsoft.com/windows/console/console-virtual-terminal-sequences#example-of-enabling-virtual-terminal-processing"/></remarks>
    /// <returns><c>true</c> if virtual terminal processing is enabled, or <c>false</c> if it is unavailable.</returns>
    [ExcludeFromCodeCoverage]
    public static bool EnableColorSupport() {
        if (virtualTerminalProcessingState == VirtualTerminalProcessing.Disabled) {
            IntPtr stdout = GetStdHandle(StandardHandle.StandardOutputHandle);
            virtualTerminalProcessingState = stdout == INVALID_HANDLE_VALUE
                || !GetConsoleMode(stdout, out ConsoleMode originalStdoutMode)
                || !SetConsoleMode(stdout, originalStdoutMode | ConsoleMode.EnableVirtualTerminalProcessing)
                    ? VirtualTerminalProcessing.Unavailable
                    : VirtualTerminalProcessing.Enabled;

            try {
                Console.OutputEncoding = Strings.Utf8; // make RenderProgressBar not write ?
            } catch (IOException) {
                // leave encoding unchanged, maybe the program won't try to write a progress bar with a non-ANSI character
            }
        }

        return virtualTerminalProcessingState == VirtualTerminalProcessing.Enabled;
    }

    /// <summary>Print a textual horizontal progress bar to a string, to be written in a console.</summary>
    /// <param name="progress">Percentage of completion, in the range [0.0, 1.0].</param>
    /// <param name="options">Customize the appearance of the progress bar, such as its width and colors.</param>
    /// <returns>A string containing a textual progress bar, potentially including ANSI escape sequences for colors, ready to pass to <see cref="Console.Write(string)"/>.</returns>
    public static string RenderProgressBar(double progress, ProgressBarOptions options) {
        progress = progress.Clip(0, 1);
        StringBuilder rendered      = new();
        int           totalWidth    = options.TotalWidth(progress);
        int           completeWidth = (int) Math.Floor(totalWidth * progress);
        int           column;

        string? colorEscapeSequence = completeWidth != 0 ? Color(options.CompleteForegroundColor, options.CompleteBackgroundColor) : null;
        if (colorEscapeSequence != null) {
            rendered.Append(colorEscapeSequence);
        }
        for (column = 0; column < completeWidth; column++) {
            rendered.Append(options.CompleteChar);
        }
        if (colorEscapeSequence != null) {
            rendered.Append(ResetColor);
        }

        colorEscapeSequence = completeWidth != totalWidth ? Color(options.IncompleteForegroundColor, options.IncompleteBackgroundColor) : null;
        if (colorEscapeSequence != null) {
            rendered.Append(colorEscapeSequence);
        }
        for (; column < totalWidth; column++) {
            rendered.Append(options.IncompleteChar);
        }
        if (colorEscapeSequence != null) {
            rendered.Append(ResetColor);
        }

        // https://learn.microsoft.com/windows/terminal/tutorials/progress-bar-sequences
        if (options.EnableWindowsProgress && Environment.OSVersion.Platform == PlatformID.Win32NT) {
            if (IS_WINDOWS_TERMINAL && EnableColorSupport()) {
                rendered.Append("\e]9;4;")
                    .Append(progress < 1 ? 1 : 0)
                    .Append(';')
                    .Append((int) Math.Round(progress * 100))
                    .Append('\a');
            } else {
                try {
                    completeProgressBarLingerCancellation?.Cancel(false);
                } catch (ObjectDisposedException) {} catch (AggregateException) {}

                TASKBAR_LIST.Value.SetProgressValue(MAIN_WINDOW.Value, (ulong) Math.Floor(progress * uint.MaxValue), uint.MaxValue);

                if (progress >= 1) {
                    CancellationTokenSource cts = new();
                    _ = Task.Run(async () => {
                        try {
                            await Task.Delay(2000, cts.Token).ConfigureAwait(false);
                            TASKBAR_LIST.Value.SetProgressState(MAIN_WINDOW.Value, TaskbarProgressBarStatus.NoProgress);
                        } catch (OperationCanceledException) {}
                    }, cts.Token);
                    Interlocked.Exchange(ref completeProgressBarLingerCancellation, cts)?.Dispose();
                }
            }
        }

        return rendered.ToString();
    }

    /// <summary>Control the appearance of the progress bar in <see cref="RenderProgressBar"/>.</summary>
    [PublicAPI]
    public readonly record struct ProgressBarOptions() {

        /// <summary>Construct options where the progress bar will always be <paramref name="consoleWidthDifference"/> columns narrower than the total console width.</summary>
        /// <param name="consoleWidthDifference">Progress bar width will be equal to (<see cref="Console.WindowWidth"/> - <paramref name="consoleWidthDifference"/>).</param>
        [SetsRequiredMembers]
        public ProgressBarOptions(int consoleWidthDifference): this() {
            TotalWidth = _ => Console.WindowWidth - Math.Abs(consoleWidthDifference);
        }

        /// <summary>How wide, in columns, the progress bar should be. To make it the full width of the console window, return <see cref="Console.WindowWidth"/>. The <see cref="double"/> argument is the current progress value.</summary>
        public required Func<double, int> TotalWidth { get; init; }

        /// <summary>Foreground color used to render the left, completed side of the progress bar. Does not change the console's text color when null. Defaults to <c>0xffaa00</c>.</summary>
        public TerminalColor? CompleteForegroundColor { get; init; } = System.Drawing.Color.FromArgb(0xff, 0xaa, 0x00);

        /// <summary>Background color used to render the left, completed side of the progress bar. Does not change the console's background color when null, which is the default.</summary>
        public TerminalColor? CompleteBackgroundColor { get; init; } = null;

        /// <summary>Foreground color used to render the right, incomplete side of the progress bar. Does not change the console's text color when null, which is the default.</summary>
        public TerminalColor? IncompleteForegroundColor { get; init; } = null;

        /// <summary>Background color used to render the right, incomplete side of the progress bar. Does not change the console's background color when null, which is the default.</summary>
        public TerminalColor? IncompleteBackgroundColor { get; init; } = null;

        /// <summary>The character that is printed on the left side of the progress bar to represent the completed portion. By default, this is ━ (U+2501 Box Drawings Heavy Horizontal). If non-ASCII characters like this render as ?, try setting <see cref="Console.OutputEncoding"/> to <see cref="Encoding.UTF8"/>.</summary>
        public char CompleteChar { get; init; } = '\u2501';

        /// <summary>The character that is printed on the right side of the progress bar to represent the incomplete portion. By default, this is ─ (U+2500 Box Drawings Light Horizontal). If non-ASCII characters like this render as ?, try setting <see cref="Console.OutputEncoding"/> to <see cref="Encoding.UTF8"/>.</summary>
        public char IncompleteChar { get; init; } = '\u2500';

        /// <summary><c>true</c> to also render this progress bar in the Windows Terminal tab and Windows taskbar button for this console, or <c>false</c> to not change the taskbar or tab. Has no effect on non-Windows operating systems.</summary>
        public bool EnableWindowsProgress { get; init; } = true;

    }

    private static string? ToAnsiEscapeCode(this TerminalColor? color, bool isForeground) => color switch {
        ConsoleColor.Black       => (30 + (isForeground ? 0 : 10)).ToString(),
        ConsoleColor.Blue        => (94 + (isForeground ? 0 : 10)).ToString(),
        ConsoleColor.Cyan        => (96 + (isForeground ? 0 : 10)).ToString(),
        ConsoleColor.DarkBlue    => (34 + (isForeground ? 0 : 10)).ToString(),
        ConsoleColor.DarkCyan    => (36 + (isForeground ? 0 : 10)).ToString(),
        ConsoleColor.DarkGray    => (90 + (isForeground ? 0 : 10)).ToString(),
        ConsoleColor.DarkGreen   => (32 + (isForeground ? 0 : 10)).ToString(),
        ConsoleColor.DarkMagenta => (35 + (isForeground ? 0 : 10)).ToString(),
        ConsoleColor.DarkRed     => (31 + (isForeground ? 0 : 10)).ToString(),
        ConsoleColor.DarkYellow  => (33 + (isForeground ? 0 : 10)).ToString(),
        ConsoleColor.Gray        => (37 + (isForeground ? 0 : 10)).ToString(),
        ConsoleColor.Green       => (92 + (isForeground ? 0 : 10)).ToString(),
        ConsoleColor.Magenta     => (95 + (isForeground ? 0 : 10)).ToString(),
        ConsoleColor.Red         => (91 + (isForeground ? 0 : 10)).ToString(),
        ConsoleColor.White       => (97 + (isForeground ? 0 : 10)).ToString(),
        ConsoleColor.Yellow      => (93 + (isForeground ? 0 : 10)).ToString(),
        (ConsoleColor) (-1)      => (39 + (isForeground ? 0 : 10)).ToString(),
        Color { A: 0 }           => 38 + (isForeground ? 0 : 10) + ";1",
        Color c                  => $"{38 + (isForeground ? 0 : 10)};2;{c.R:D};{c.G:D};{c.B:D}",
        _                        => null
    };

    private enum VirtualTerminalProcessing {

        Disabled, Enabled, Unavailable

    }

    /// <summary>
    /// Retrieves a handle to the specified standard device (standard input, standard output, or standard error).
    /// </summary>
    /// <remarks><see href="https://learn.microsoft.com/windows/console/getstdhandle"/></remarks>
    /// <param name="handle">The standard device.</param>
    /// <returns>
    /// <para>If the function succeeds, the return value is a handle to the specified device, or a redirected handle set by a previous call to <see href="https://learn.microsoft.com/windows/console/setstdhandle">SetStdHandle</see>. The handle has <c>GENERIC_READ</c> and <c>GENERIC_WRITE</c> access rights, unless the application has used <c>SetStdHandle</c> to set a standard handle with lesser access.</para>
    /// <para>It is not required to dispose of this handle with <see href="https://learn.microsoft.com/windows/win32/api/handleapi/nf-handleapi-closehandle">CloseHandle</see> when done. See Remarks for more information.</para>
    /// <para>If the function fails, the return value is <c>INVALID_HANDLE_VALUE</c>. To get extended error information, call <see cref="Marshal.GetLastWin32Error"/>.</para>
    /// <para>If an application does not have associated standard handles, such as a service running on an interactive desktop, and has not redirected them, the return value is <c>NULL</c>.</para>
    /// </returns>
#if NET7_0_OR_GREATER
    [LibraryImport(KERNEL32, SetLastError = true)]
    private static partial
#else
    [DllImport(KERNEL32, SetLastError = true)]
    private static extern
#endif
        IntPtr GetStdHandle(StandardHandle handle);

    [SuppressMessage("ReSharper", "CommentTypo")]
    private enum StandardHandle: uint {

        /*/// <summary>
        /// The standard input device. Initially, this is the console input buffer, <c>CONIN$</c>.
        /// </summary>
        STANDARD_INPUT_HANDLE = 4294967286,*/

        /// <summary>
        /// The standard output device. Initially, this is the active console screen buffer, <c>CONOUT$</c>.
        /// </summary>
        StandardOutputHandle = 4294967285,

        /// <summary>
        /// The standard error device. Initially, this is the active console screen buffer, <c>CONOUT$</c>.
        /// </summary>
        StandardErrorHandle = 4294967284

    }

    /// <summary>
    /// Retrieves the current input mode of a console's input buffer or the current output mode of a console screen buffer.
    /// </summary>
    /// <remarks><see href="https://learn.microsoft.com/windows/console/getconsolemode"/></remarks>
    /// <param name="consoleHandle">A handle to the console input buffer or the console screen buffer. The handle must have the <c>GENERIC_READ</c> access right. For more information, see <see href="https://learn.microsoft.com/windows/console/console-buffer-security-and-access-rights"/> Console Buffer Security and Access Rights.</param>
    /// <param name="mode">A pointer to a variable that receives the current mode of the specified buffer.</param>
    /// <returns>If the function succeeds, the return value is nonzero. If the function fails, the return value is zero. To get extended error information, call <see cref="Marshal.GetLastWin32Error"/>.</returns>
    [return: MarshalAs(UnmanagedType.Bool)]
#if NET7_0_OR_GREATER
    [LibraryImport(KERNEL32, SetLastError = true)]
    private static partial
#else
    [DllImport(KERNEL32, SetLastError = true)]
    private static extern
#endif
        bool GetConsoleMode(IntPtr consoleHandle, out ConsoleMode mode);

    /// <summary>
    /// Sets the input mode of a console's input buffer or the output mode of a console screen buffer.
    /// </summary>
    /// <remarks><see href="https://learn.microsoft.com/windows/console/setconsolemode"/></remarks>
    /// <param name="consoleHandle">A handle to the console input buffer or the console screen buffer. The handle must have the <c>GENERIC_READ</c> access right. For more information, see <see href="https://learn.microsoft.com/windows/console/console-buffer-security-and-access-rights"/> Console Buffer Security and Access Rights.</param>
    /// <param name="mode">The input or output mode to be set.</param>
    /// <returns>If the function succeeds, the return value is nonzero. If the function fails, the return value is zero. To get extended error information, call <see cref="Marshal.GetLastWin32Error"/>.</returns>
    [return: MarshalAs(UnmanagedType.Bool)]
#if NET7_0_OR_GREATER
    [LibraryImport(KERNEL32, SetLastError = true)]
    private static partial
#else
    [DllImport(KERNEL32, SetLastError = true)]
    private static extern
#endif
        bool SetConsoleMode(IntPtr consoleHandle, ConsoleMode mode);

#if NET7_0_OR_GREATER
    [LibraryImport(KERNEL32)]
    private static partial
#else
    [DllImport(KERNEL32)]
    private static extern
#endif
        nint GetConsoleWindow();

    /// <summary>
    /// <see href="https://learn.microsoft.com/windows/console/setconsolemode#parameters"/>
    /// </summary>
    [Flags]
    private enum ConsoleMode: uint {

        // stdin
        /*ENABLE_PROCESSED_INPUT        = 1 << 0,
        ENABLE_LINE_INPUT             = 1 << 1,
        ENABLE_ECHO_INPUT             = 1 << 2,
        ENABLE_WINDOW_INPUT           = 1 << 3,
        ENABLE_MOUSE_INPUT            = 1 << 4,
        ENABLE_INSERT_MODE            = 1 << 5,
        ENABLE_QUICK_EDIT_MODE        = 1 << 6,
        ENABLE_VIRTUAL_TERMINAL_INPUT = 1 << 9,*/

        // stdout
        /*ENABLE_PROCESSED_OUTPUT            = 1 << 0,
        ENABLE_WRAP_AT_EOL_OUTPUT          = 1 << 1,*/
        EnableVirtualTerminalProcessing = 1 << 2,
        /*DISABLE_NEWLINE_AUTO_RETURN        = 1 << 3,
        ENABLE_LVB_GRID_WORLDWIDE          = 1 << 4*/

    }

#if NET8_0_OR_GREATER
    private static ITaskbarList3 GetTaskbarList() {
        Guid interfaceId = new(ITaskbarList3.InterfaceId);
        Guid clsid       = new(TASKBAR_LIST_CLSID);
        // Are you fucking kidding me with this shit? How is this "as easy as built-in COM"?
        Marshal.ThrowExceptionForHR(CoCreateInstance(ref clsid, 0 /* No aggregation */, 1 /* INPROC_SERVER */, ref interfaceId, out object comObject));
        return (ITaskbarList3) comObject;
    }

    [LibraryImport("ole32.dll")]
    private static partial int CoCreateInstance(ref Guid clsid, nint aggregateIUnknownInterface, uint classExecutionContext, ref Guid interfaceId,
                                                [MarshalUsing(typeof(UniqueComInterfaceMarshaller<object>))] out object comObject);

    [GeneratedComInterface]
#else
    private static ITaskbarList3 GetTaskbarList() {
        ITaskbarList3 instance = new();
        instance.HrInit();
        return instance;
    }

    [ComImport, Guid(TASKBAR_LIST_CLSID), ClassInterface(ClassInterfaceType.None)]
    private class CTaskbarList;

    [ComImport]
    [CoClass(typeof(CTaskbarList))]
#endif

    [Guid(InterfaceId)]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal partial interface ITaskbarList3 {

        // ITaskbarList
        [PreserveSig]
        void HrInit();

        [PreserveSig]
        void AddTab(nint hwnd);

        [PreserveSig]
        void DeleteTab(nint hwnd);

        [PreserveSig]
        void ActivateTab(nint hwnd);

        [PreserveSig]
        void SetActiveAlt(nint hwnd);

        // ITaskbarList2
        [PreserveSig]
        void MarkFullscreenWindow(nint hwnd, [MarshalAs(UnmanagedType.Bool)] bool fullscreen);

        // ITaskbarList3
        [PreserveSig]
        void SetProgressValue(nint hwnd, ulong completed, ulong total);

        [PreserveSig]
        void SetProgressState(nint hwnd, TaskbarProgressBarStatus state);

        internal const string InterfaceId = "c43dc798-95d1-4bea-9030-bb99e2983a1a";

    }

    internal enum TaskbarProgressBarStatus {

        NoProgress = 0, Indeterminate = 1, Normal = 2, Error = 4, Paused = 8

    }

    /// <summary>
    /// A color that can be used in a terminal window, either a 4-bit preset <see cref="ConsoleColor"/> that follows the user's preferences, or a 24-bit custom <see cref="System.Drawing.Color"/> that always renders the same way (semitransparency is ignored).
    /// </summary>
    public readonly union TerminalColor(Color, ConsoleColor);

}