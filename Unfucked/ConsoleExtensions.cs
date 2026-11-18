using static Unfucked.ConsoleControl;

namespace Unfucked;

/// <summary>
/// <para>Methods that make it easier to control the text console.</para>
/// <para>The same members as <see cref="ConsoleControl"/>, but as extensions on <see cref="Console"/>.</para>
/// </summary>
public static partial class ConsoleExtensions {

    extension(Console) {

        /// <inheritdoc cref="Clear" />
        public static string ClearAll => Clear;

        /// <inheritdoc cref="ClearLine" />
        public static string ClearLine => ClearLine;

        /// <inheritdoc cref="ConsoleControl.WriteClear" />
        public static void WriteClear() => ConsoleControl.WriteClear();

        /// <inheritdoc cref="ConsoleControl.WriteClearLine" />
        public static void WriteClearLine() => ConsoleControl.WriteClearLine();

        /// <inheritdoc cref="ConsoleControl.Color(TerminalColor?,TerminalColor?)" />
        [Pure]
        public static string Color(TerminalColor? foregroundColor, TerminalColor? backgroundColor = null) => ConsoleControl.Color(foregroundColor, backgroundColor);

        /// <inheritdoc cref="ConsoleControl.Color(string,TerminalColor?,TerminalColor?)" />
        [Pure]
        public static string Color(string text, TerminalColor? foregroundColor, TerminalColor? backgroundColor = null) => ConsoleControl.Color(text, foregroundColor, backgroundColor);

        /// <inheritdoc cref="ConsoleControl.Write(string,TerminalColor?,TerminalColor?)" />
        public static void Write(string text, TerminalColor? foregroundColor, TerminalColor? backgroundColor = null) => ConsoleControl.Write(text, foregroundColor, backgroundColor);

        /// <inheritdoc cref="ConsoleControl.WriteLine(string,TerminalColor?,TerminalColor?)" />
        public static void WriteLine(string text, TerminalColor? foregroundColor, TerminalColor? backgroundColor = null) => ConsoleControl.WriteLine(text, foregroundColor, backgroundColor);

        /// <inheritdoc cref="ResetColor" />
        public static string ResetColor => ResetColor;

        /// <inheritdoc cref="ConsoleControl.EnableColorSupport" />
        public static bool EnableColorSupport() => ConsoleControl.EnableColorSupport();

        /// <inheritdoc cref="ConsoleControl.RenderProgressBar" />
        public static string RenderProgressBar(double progress, ProgressBarOptions options) => ConsoleControl.RenderProgressBar(progress, options);

    }

}