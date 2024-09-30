using System.Runtime.InteropServices;

namespace Unfucked;

/*
 * Reference: https://en.wikipedia.org/wiki/ANSI_escape_code#SGR_(Select_Graphic_Rendition)_parameters
 */
/// <summary>
/// Methods that make it easier to control the text console.
/// </summary>
public static class ConsoleControl {

    private const string Kernel32 = "kernel32.dll";

    private static readonly IntPtr InvalidHandleValue = new(-1);

    /// <summary>
    /// The color that the console would have used if you never changed its color. For example, this can be used to reset the text color back to its default color (like white) after changing it to blue.
    /// </summary>
    /// <seealso cref="ResetColor"/>
    public const ConsoleColor DefaultColor = (ConsoleColor) (-1);

    private static VirtualTerminalProcessing _virtualTerminalProcessingState =
        Environment.OSVersion.Platform == PlatformID.Win32NT ? VirtualTerminalProcessing.Disabled : VirtualTerminalProcessing.Enabled;

    /// <summary>
    /// Clear screen and move to the top-left position
    /// </summary>
    [ExcludeFromCodeCoverage]
    public static void Clear() {
        if (IsColorSupported()) {
            Console.Write("\x1b[1J\x1b[1;1H");
        } else {
            Console.Clear();
        }
    }

    /// <summary>
    /// Get ANSI control characters to change the foreground and background colors.
    /// </summary>
    /// <param name="foregroundColor">Foreground text color, or <c>null</c> to leave unchanged</param>
    /// <param name="backgroundColor">Background color, or <c>null</c> to leave unchanged</param>
    /// <returns>An ANSI escape sequence that changes the text and background color, if specified.</returns>
    [Pure]
    public static string Color(ConsoleColor? foregroundColor, ConsoleColor? backgroundColor = null) {
        if (IsColorSupported()) {
            bool hasForegroundAndBackground = foregroundColor != null && backgroundColor != null;
            return $"\x1b[{foregroundColor.ToAnsiEscapeCode():D}{(hasForegroundAndBackground ? ";" : "")}{backgroundColor.ToAnsiEscapeCode() + 10:D}m";
        } else {
            return string.Empty;
        }
    }

    /// <summary>
    /// Wrap text with ANSI control characters to change the foreground and background colors and then reset it to the default colors at the end of the string.
    /// </summary>
    /// <param name="text">Text to wrap with color</param>
    /// <param name="foregroundColor">Foreground text color, or <c>null</c> to leave unchanged</param>
    /// <param name="backgroundColor">Background color, or <c>null</c> to leave unchanged</param>
    /// <returns>An ANSI escape sequence that changes the text and background color, followed by <paramref name="text"/>, followed by the ANSI escape sequence to reset colors back to the console defaults.</returns>
    [Pure]
    public static string Color(string text, ConsoleColor? foregroundColor, ConsoleColor? backgroundColor = null) {
        return Color(foregroundColor, backgroundColor) + text + ResetColor;
    }

    /// <summary>
    /// Print colored text to the console. After calling this method, the console will be reset to its default colors for future text to be printed.
    /// </summary>
    /// <param name="text">Text to print in color</param>
    /// <param name="foregroundColor">Foreground text color, or <c>null</c> to leave unchanged</param>
    /// <param name="backgroundColor">Background color, or <c>null</c> to leave unchanged</param>
    [ExcludeFromCodeCoverage]
    public static void Write(string text, ConsoleColor? foregroundColor, ConsoleColor? backgroundColor = null) {
        Console.Write(Color(text, foregroundColor, backgroundColor));
    }

    /// <summary>
    /// Print colored text to the console, followed by a line break. After calling this method, the console will be reset to its default colors for future text to be printed.
    /// </summary>
    /// <param name="text">Text to print in color</param>
    /// <param name="foregroundColor">Foreground text color, or <c>null</c> to leave unchanged</param>
    /// <param name="backgroundColor">Background color, or <c>null</c> to leave unchanged</param>
    [ExcludeFromCodeCoverage]
    public static void WriteLine(string text, ConsoleColor? foregroundColor, ConsoleColor? backgroundColor = null) {
        Console.WriteLine(Color(text, foregroundColor, backgroundColor));
    }

    /// <summary>
    /// Write this string to reset the foreground and background text colors to their default values.
    /// </summary>
    public static string ResetColor { get; } = Color(DefaultColor, DefaultColor);

    /// <summary>
    /// <para>On Windows, you have to call a method to explicitly turn on ANSI escape sequence processing, otherwise you will see the raw escape codes printed as text.</para>
    /// <para>Requires Windows 10 1511 or later.</para>
    /// </summary>
    /// <remarks><see href="https://learn.microsoft.com/en-us/windows/console/console-virtual-terminal-sequences#example-of-enabling-virtual-terminal-processing"/></remarks>
    /// <returns><c>true</c> if virtual terminal processing is enabled, or <c>false</c> if it is unavailable</returns>
    [ExcludeFromCodeCoverage]
    public static bool IsColorSupported() {
        if (_virtualTerminalProcessingState == VirtualTerminalProcessing.Disabled) {
            IntPtr stdout = GetStdHandle(StandardHandle.StandardOutputHandle);
            _virtualTerminalProcessingState = stdout == InvalidHandleValue
                || !GetConsoleMode(stdout, out ConsoleMode originalStdoutMode)
                || !SetConsoleMode(stdout, originalStdoutMode | ConsoleMode.EnableVirtualTerminalProcessing)
                    ? VirtualTerminalProcessing.Unavailable
                    : VirtualTerminalProcessing.Enabled;
        }

        return _virtualTerminalProcessingState == VirtualTerminalProcessing.Enabled;
    }

    private static int? ToAnsiEscapeCode(this ConsoleColor? color) => color switch {
        null                     => null,
        ConsoleColor.Black       => 30,
        ConsoleColor.Blue        => 94,
        ConsoleColor.Cyan        => 96,
        ConsoleColor.DarkBlue    => 34,
        ConsoleColor.DarkCyan    => 36,
        ConsoleColor.DarkGray    => 90,
        ConsoleColor.DarkGreen   => 32,
        ConsoleColor.DarkMagenta => 35,
        ConsoleColor.DarkRed     => 31,
        ConsoleColor.DarkYellow  => 33,
        ConsoleColor.Gray        => 37,
        ConsoleColor.Green       => 92,
        ConsoleColor.Magenta     => 95,
        ConsoleColor.Red         => 91,
        ConsoleColor.White       => 97,
        ConsoleColor.Yellow      => 93,
        (ConsoleColor) (-1)      => 39,
        _                        => null
    };

    private enum VirtualTerminalProcessing {

        Disabled,
        Enabled,
        Unavailable

    }

    /// <summary>
    /// Retrieves a handle to the specified standard device (standard input, standard output, or standard error).
    /// </summary>
    /// <remarks><see href="https://learn.microsoft.com/en-us/windows/console/getstdhandle"/></remarks>
    /// <param name="handle">The standard device.</param>
    /// <returns>
    /// <para>If the function succeeds, the return value is a handle to the specified device, or a redirected handle set by a previous call to <see href="https://learn.microsoft.com/en-us/windows/console/setstdhandle">SetStdHandle</see>. The handle has <c>GENERIC_READ</c> and <c>GENERIC_WRITE</c> access rights, unless the application has used <c>SetStdHandle</c> to set a standard handle with lesser access.</para>
    /// <para>It is not required to dispose of this handle with <see href="https://learn.microsoft.com/en-us/windows/win32/api/handleapi/nf-handleapi-closehandle">CloseHandle</see> when done. See Remarks for more information.</para>
    /// <para>If the function fails, the return value is <c>INVALID_HANDLE_VALUE</c>. To get extended error information, call <see cref="Marshal.GetLastWin32Error"/>.</para>
    /// <para>If an application does not have associated standard handles, such as a service running on an interactive desktop, and has not redirected them, the return value is <c>NULL</c>.</para>
    /// </returns>
    [DllImport(Kernel32, SetLastError = true)]
    private static extern IntPtr GetStdHandle(StandardHandle handle);

    private enum StandardHandle: uint {

        /// <summary>
        /// The standard input device. Initially, this is the console input buffer, <c>CONIN$</c>.
        /// </summary>
        StandardInputHandle = 4294967286,

        /// <summary>
        /// 	The standard output device. Initially, this is the active console screen buffer, <c>CONOUT$</c>.
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
    /// <remarks><see href="https://learn.microsoft.com/en-us/windows/console/getconsolemode"/></remarks>
    /// <param name="consoleHandle">A handle to the console input buffer or the console screen buffer. The handle must have the <c>GENERIC_READ</c> access right. For more information, see <see href="https://learn.microsoft.com/en-us/windows/console/console-buffer-security-and-access-rights"/> Console Buffer Security and Access Rights.</param>
    /// <param name="mode">A pointer to a variable that receives the current mode of the specified buffer.</param>
    /// <returns>If the function succeeds, the return value is nonzero. If the function fails, the return value is zero. To get extended error information, call <see cref="Marshal.GetLastWin32Error"/>.</returns>
    [DllImport(Kernel32, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetConsoleMode(IntPtr consoleHandle, [Out] out ConsoleMode mode);

    /// <summary>
    /// Sets the input mode of a console's input buffer or the output mode of a console screen buffer.
    /// </summary>
    /// <remarks><see href="https://learn.microsoft.com/en-us/windows/console/setconsolemode"/></remarks>
    /// <param name="consoleHandle">A handle to the console input buffer or the console screen buffer. The handle must have the <c>GENERIC_READ</c> access right. For more information, see <see href="https://learn.microsoft.com/en-us/windows/console/console-buffer-security-and-access-rights"/> Console Buffer Security and Access Rights.</param>
    /// <param name="mode">The input or output mode to be set.</param>
    /// <returns>If the function succeeds, the return value is nonzero. If the function fails, the return value is zero. To get extended error information, call <see cref="Marshal.GetLastWin32Error"/>.</returns>
    [DllImport(Kernel32, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetConsoleMode(IntPtr consoleHandle, ConsoleMode mode);

    /// <summary>
    /// <see href="https://learn.microsoft.com/en-us/windows/console/setconsolemode#parameters"/>
    /// </summary>
    [Flags]
    private enum ConsoleMode: uint {

        // stdin
        EnableProcessedInput       = 1 << 0,
        EnableLineInput            = 1 << 1,
        EnableEchoInput            = 1 << 2,
        EnableWindowInput          = 1 << 3,
        EnableMouseInput           = 1 << 4,
        EnableInsertMode           = 1 << 5,
        EnableQuickEditMode        = 1 << 6,
        EnableVirtualTerminalInput = 1 << 9,

        // stdout
        EnableProcessedOutput           = 1 << 0,
        EnableWrapAtEolOutput           = 1 << 1,
        EnableVirtualTerminalProcessing = 1 << 2,
        DisableNewlineAutoReturn        = 1 << 3,
        EnableLvbGridWorldwide          = 1 << 4

    }

}