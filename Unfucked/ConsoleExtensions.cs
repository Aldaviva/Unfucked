using System.Drawing;

namespace Unfucked;

/// <summary>
/// <para>Methods that make it easier to control the text console.</para>
/// <para>The same members as <see cref="ConsoleControl"/>, but as extensions on <see cref="Console"/>.</para>
/// </summary>
public static partial class ConsoleExtensions {

    extension(Console) {

        /// <inheritdoc cref="ConsoleControl.Clear" />
        public static string ClearAll => ConsoleControl.Clear;

        /// <inheritdoc cref="ConsoleControl.ClearLine" />
        public static string ClearLine => ConsoleControl.ClearLine;

        /// <inheritdoc cref="ConsoleControl.WriteClear" />
        public static void WriteClear() => ConsoleControl.WriteClear();

        /// <inheritdoc cref="ConsoleControl.WriteClearLine" />
        public static void WriteClearLine() => ConsoleControl.WriteClearLine();

        /// <inheritdoc cref="ConsoleControl.Color(ConsoleColor?,ConsoleColor?)" />
        [Pure]
        public static string Color(ConsoleColor? foregroundColor, ConsoleColor? backgroundColor = null) => ConsoleControl.Color(foregroundColor, backgroundColor);

        /// <inheritdoc cref="ConsoleControl.Color(System.Drawing.Color?,System.Drawing.Color?)" />
        [Pure]
        public static string Color(Color? foregroundColor, Color? backgroundColor = null) => ConsoleControl.Color(foregroundColor, backgroundColor);

        /// <inheritdoc cref="ConsoleControl.Color(string,ConsoleColor?,ConsoleColor?)" />
        [Pure]
        public static string Color(string text, ConsoleColor? foregroundColor, ConsoleColor? backgroundColor = null) => ConsoleControl.Color(text, foregroundColor, backgroundColor);

        /// <inheritdoc cref="ConsoleControl.Color(string,System.Drawing.Color?,System.Drawing.Color?)" />
        [Pure]
        public static string Color(string text, Color? foregroundColor, Color? backgroundColor = null) => ConsoleControl.Color(text, foregroundColor, backgroundColor);

        /// <inheritdoc cref="ConsoleControl.Write(string,System.ConsoleColor?,System.ConsoleColor?)" />
        public static void Write(string text, ConsoleColor? foregroundColor, ConsoleColor? backgroundColor = null) => ConsoleControl.Write(text, foregroundColor, backgroundColor);

        /// <inheritdoc cref="ConsoleControl.Write(string,System.Drawing.Color?,System.Drawing.Color?)" />
        public static void Write(string text, Color? foregroundColor, Color? backgroundColor = null) => ConsoleControl.Write(text, foregroundColor, backgroundColor);

        /// <inheritdoc cref="ConsoleControl.WriteLine(string,System.ConsoleColor?,System.ConsoleColor?)" />
        public static void WriteLine(string text, ConsoleColor? foregroundColor, ConsoleColor? backgroundColor = null) => ConsoleControl.WriteLine(text, foregroundColor, backgroundColor);

        /// <inheritdoc cref="ConsoleControl.WriteLine(string,System.Drawing.Color?,System.Drawing.Color?)" />
        public static void WriteLine(string text, Color? foregroundColor, Color? backgroundColor = null) => ConsoleControl.WriteLine(text, foregroundColor, backgroundColor);

        /// <inheritdoc cref="ConsoleControl.ResetColor" />
        public static string ResetColor => ConsoleControl.ResetColor;

        /// <inheritdoc cref="ConsoleControl.EnableColorSupport" />
        public static bool EnableColorSupport() => ConsoleControl.EnableColorSupport();

        /// <inheritdoc cref="ConsoleControl.RenderProgressBar" />
        public static string RenderProgressBar(double progress, ConsoleControl.ProgressBarOptions options) => ConsoleControl.RenderProgressBar(progress, options);

    }

}