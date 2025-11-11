using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Options;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;

namespace Unfucked.DI.Logging;

/// <summary>
/// <para>A console log formatter that prints messages with a level character (like 'i'), ISO 8601-like date and time with milliseconds, class (by default the simple name), message, and any stack trace, separated by vertical pipes (' | '), for example:</para>
/// <para><c> i | 2024-09-08 13:27:00.000 | Program | Application starting</c></para>
/// <para>Install this formatter in your app host using <see cref="LoggingExtensions.AddUnfuckedConsole"/>, for example:</para>
/// <para><c>new HostApplicationBuilder().Logging.AddUnfuckedConsole(opts => opts.Color = false);</c></para>
/// </summary>
/// <param name="options">Whether the formatter should include full class names including their namespace, what character to use to separate columns, and whether to use colored output</param>
[ExcludeFromCodeCoverage]
public class UnfuckedConsoleFormatter(IOptions<UnfuckedConsoleFormatterOptions> options): ConsoleFormatter(ID) {

    /// <summary>
    /// Name of this logger class, used by <see cref="ConsoleLoggerExtensions.AddConsole(Microsoft.Extensions.Logging.ILoggingBuilder,Action{ConsoleLoggerOptions})"/>.
    /// </summary>
    public const string ID = "UnfuckedConsoleFormatter";

    private const string DEFAULT_DATE_FORMAT = "yyyy'-'MM'-'dd' 'HH':'mm':'ss.fff";
    private const string PADDING             = "                                ";
    private const string ANSI_RESET          = "\e[0m";

    /**
     * Any categories longer than this will be wider only on their own line, while subsequent lines from different categories will only be as wide as this field.
     */
    private static readonly int MAX_PADDED_CATEGORY_LENGTH = PADDING.Length;

    private static readonly char[] LEVEL_LABELS = ['t', 'd', 'i', 'W', 'E', 'C', ' '];

    private readonly UnfuckedConsoleFormatterOptions options  = options.Value;
    private readonly bool                            useColor = options.Value.Color && ConsoleControl.IsColorSupported();

    private readonly string[] levelColors = [
        ConsoleControl.Color(ConsoleColor.DarkGray),                         // trace
        ConsoleControl.Color(ConsoleColor.Gray),                             // debug
        string.Empty,                                                        // info
        ConsoleControl.Color(Color.Black, Color.FromArgb(0xFF, 0xAA, 0x00)), // warn
        ConsoleControl.Color(Color.White, Color.FromArgb(0xB0, 0x3A, 0x3A)), // error
        ConsoleControl.Color(Color.White, Color.FromArgb(0xB0, 0x3A, 0x3A))  // critical
    ];

    private int maxCategoryLength;

    /// <inheritdoc />
    public override void Write<TState>(in LogEntry<TState> logEntry, IExternalScopeProvider? scopeProvider, TextWriter textWriter) {
        DateTimeOffset now       = DateTimeOffset.Now;
        string?        message   = logEntry.State?.ToString(); // logEntry.Formatter?.Invoke(logEntry.State, logEntry.Exception)
        Exception?     exception = logEntry.Exception;
        if (message is not null || exception is not null) {
            if (useColor) {
                textWriter.Write(levelColors[(int) logEntry.LogLevel]);
            }

            textWriter.Write(' ');
            textWriter.Write(LEVEL_LABELS[(int) logEntry.LogLevel]);
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
                textWriter.Write(ANSI_RESET);
            }

            textWriter.WriteLine();
        }
    }

    private void WriteCategory<TState>(LogEntry<TState> logEntry, TextWriter textWriter) {
        int lastSeparatorPosition = options.IncludeNamespaces ? -1 : logEntry.Category.LastIndexOf('.', logEntry.Category.Length - 2);

        ReadOnlySpan<char> category = lastSeparatorPosition != -1 ? logEntry.Category.AsSpan(lastSeparatorPosition + 1) : logEntry.Category.AsSpan();

        int categoryLength = category.Length;
#if NETCOREAPP2_1_OR_GREATER || NETSTANDARD2_1_OR_GREATER
        textWriter.Write(category);
#else
        textWriter.Write(category.ToString());
#endif

        if (categoryLength >= maxCategoryLength) {
            maxCategoryLength = categoryLength;
        } else {
            ReadOnlySpan<char> padding = PADDING.AsSpan(0, Math.Max(0, Math.Min(maxCategoryLength, MAX_PADDED_CATEGORY_LENGTH) - categoryLength));
#if NETCOREAPP2_1_OR_GREATER || NETSTANDARD2_1_OR_GREATER
            textWriter.Write(padding);
#else
            textWriter.Write(padding.ToString());
#endif
        }
    }

    private string FormatTime(DateTimeOffset now) {
        try {
            return now.ToString(options.TimestampFormat ?? DEFAULT_DATE_FORMAT);
        } catch (FormatException) {
#pragma warning disable Ex0100 // Member may throw undocumented exception - known good hardcoded fallback format string
            return now.ToString(DEFAULT_DATE_FORMAT);
#pragma warning restore Ex0100 // Member may throw undocumented exception
        }
    }

}