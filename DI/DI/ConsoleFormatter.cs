using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Options;
using System.Diagnostics.CodeAnalysis;

namespace Unfucked.DI;

/// <summary>
/// <para>A console log formatter that prints messages with a level character (like 'i'), ISO 8601-like date and time with milliseconds, class (by default the simple name), message, and any stack trace, separated by vertical pipes (' | '), for example:</para>
/// <para><c> i | 2024-09-08 13:27:00.000 | Program | Application starting</c></para>
/// <para>Install this formatter in your app host using <see cref="DependencyInjectionExtensions.AddUnfuckedConsole"/>, for example:</para>
/// <para><c>new HostApplicationBuilder().Logging.AddUnfuckedConsole(opts => opts.Color = false);</c></para>
/// </summary>
/// <param name="options">Whether the formatter should include full class names including their namespace, what character to use to separate columns, and whether to use colored output</param>
[ExcludeFromCodeCoverage]
public class ConsoleFormatter(IOptions<ConsoleFormatter.ConsoleFormatterOptions> options): Microsoft.Extensions.Logging.Console.ConsoleFormatter(Id) {

    /// <summary>
    /// Name of this logger class, used by <see cref="ConsoleLoggerExtensions.AddConsole(Microsoft.Extensions.Logging.ILoggingBuilder,Action{ConsoleLoggerOptions})"/>.
    /// </summary>
    public const string Id = "UnfuckedConsoleFormatter";

    private const string DefaultDateFormat = "yyyy'-'MM'-'dd' 'HH':'mm':'ss.fff";
    private const string Padding           = "                                ";
    private const string AnsiReset         = "\u001b[0m";

    private static readonly int    MaxPaddedCategoryLength = Padding.Length;
    private static readonly char[] LevelLabels             = ['t', 'd', 'i', 'W', 'E', 'C', ' '];

    private readonly ConsoleFormatterOptions options  = options.Value;
    private readonly bool                    useColor = options.Value.Color && ConsoleControl.IsColorSupported();

    private readonly string[] levelColors = [
        ConsoleControl.Color(ConsoleColor.DarkGray),
        string.Empty,
        ConsoleControl.Color(ConsoleColor.DarkCyan),
        ConsoleControl.Color(ConsoleColor.Black, ConsoleColor.DarkYellow),
        ConsoleControl.Color(ConsoleColor.White, ConsoleColor.DarkRed),
        ConsoleControl.Color(ConsoleColor.White, ConsoleColor.DarkRed),
    ];

    private int maxCategoryLength;

    /// <inheritdoc />
    public override void Write<TState>(in LogEntry<TState> logEntry, IExternalScopeProvider? scopeProvider, TextWriter textWriter) {
        DateTimeOffset now       = DateTimeOffset.Now;
        string?        message   = logEntry.State?.ToString();
        Exception?     exception = logEntry.Exception;
        if (message is not null || exception is not null) {
            if (useColor) {
                textWriter.Write(levelColors[(int) logEntry.LogLevel]);
            }

            textWriter.Write(' ');
            textWriter.Write(LevelLabels[(int) logEntry.LogLevel]);
            textWriter.Write(options.ColumnSeparator);
            textWriter.Write(FormatTime(now));
            textWriter.Write(options.ColumnSeparator);
            WriteCategory(logEntry, textWriter);
            textWriter.Write(options.ColumnSeparator);

            if (message is not null) {
                textWriter.Write(message);
            }

            if (message is not null && exception is not null) {
                textWriter.Write("\n   ");
            }

            if (exception is not null) {
                textWriter.Write(exception.ToString().Replace("\n", "\n   "));
            }

            if (useColor) {
                textWriter.Write(AnsiReset);
            }

            textWriter.WriteLine();
        }
    }

    private void WriteCategory<TState>(LogEntry<TState> logEntry, TextWriter textWriter) {
        int lastSeparatorPosition = options.IncludeNamespaces ? -1 : logEntry.Category.LastIndexOf('.', logEntry.Category.Length - 2);

        ReadOnlySpan<char> category = lastSeparatorPosition != -1 ? logEntry.Category.AsSpan(lastSeparatorPosition + 1) : logEntry.Category.AsSpan();

        int categoryLength = category.Length;
        maxCategoryLength = Math.Max(maxCategoryLength, categoryLength);
#if NET6_0_OR_GREATER
        textWriter.Write(category);
#else
        textWriter.Write(category.ToString());
#endif

        if (categoryLength >= maxCategoryLength) {
            maxCategoryLength = categoryLength;
        } else {
            ReadOnlySpan<char> padding = Padding.AsSpan(0, Math.Max(0, Math.Min(maxCategoryLength, MaxPaddedCategoryLength) - categoryLength));
#if NET6_0_OR_GREATER
            textWriter.Write(padding);
#else
            textWriter.Write(padding.ToString());
#endif
        }
    }

    private string FormatTime(DateTimeOffset now) {
        try {
            return now.ToString(options.TimestampFormat ?? DefaultDateFormat);
        } catch (FormatException) {
#pragma warning disable Ex0100 // Member may throw undocumented exception - known good hardcoded fallback format string
            return now.ToString(DefaultDateFormat);
#pragma warning restore Ex0100 // Member may throw undocumented exception
        }
    }

    /// <summary>
    /// Options to control the <see cref="ConsoleFormatter"/>.
    /// </summary>
    [SuppressMessage("ReSharper", "PropertyCanBeMadeInitOnly.Global")] // users must be able to set these in a builder pattern call where the class instance has already been constructed for them
    public class ConsoleFormatterOptions: Microsoft.Extensions.Logging.Console.ConsoleFormatterOptions {

        /// <summary>
        /// <para>By default, the type name of the class that emitted the log message is printed as the class' simple name, without its namespace (such as <c>MyClass</c>).</para>
        /// <para>By setting this to <c>true</c>, you can print the full type name including its namespace, such as <c>MyNamespace.MyClass</c>.</para>
        /// </summary>
        public bool IncludeNamespaces { get; set; }

        /// <summary>
        /// String that is printed between each column in log lines, <c>" | "</c> by default.
        /// </summary>
        public string ColumnSeparator { get; set; } = " | ";

        /// <summary>
        /// <para>By default, the console logger prints messages in color depending on the log level (errors are white text on a red background, for example).</para>
        /// <para>You may disable this by setting this property to <c>false</c>, for example, in response to the user passing a <c>--no-color</c> option on the command line.</para>
        /// <para>Color output will also be automatically disabled if the stdout console does not support color, such as on Windows versions before Windows 10 version 1511.</para>
        /// </summary>
        public bool Color { get; set; } = true;

    }

}