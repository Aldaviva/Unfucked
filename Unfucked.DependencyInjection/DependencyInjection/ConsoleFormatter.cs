using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Options;
using System.Diagnostics.CodeAnalysis;

namespace Unfucked.DependencyInjection;

[ExcludeFromCodeCoverage]
public class ConsoleFormatter(IOptions<ConsoleFormatter.UnfuckedConsoleFormatterOptions> options): Microsoft.Extensions.Logging.Console.ConsoleFormatter(Id) {

    public const  string Id                = "myConsoleFormatter";
    private const string DefaultDateFormat = "yyyy'-'MM'-'dd' 'HH':'mm':'ss.fff";
    private const string Padding           = "                                ";
    private const string AnsiReset         = "\u001b[0m";

    private static readonly int MaxPaddedCategoryLength = Padding.Length;

    private readonly UnfuckedConsoleFormatterOptions _options = options.Value;

    private int _maxCategoryLength;

    public override void Write<TState>(in LogEntry<TState> logEntry, IExternalScopeProvider? scopeProvider, TextWriter textWriter) {
        DateTimeOffset now       = DateTimeOffset.Now;
        string?        message   = logEntry.State?.ToString();
        Exception?     exception = logEntry.Exception;
        if (message is not null || exception is not null) {

            textWriter.Write(FormatLevel(logEntry.LogLevel));
            textWriter.Write(_options.ColumnSeparator);
            textWriter.Write(FormatTime(now));
            textWriter.Write(_options.ColumnSeparator);
            WriteCategory(logEntry, textWriter);
            textWriter.Write(_options.ColumnSeparator);

            if (message is not null) {
                textWriter.Write(message);
            }

            if (message is not null && exception is not null) {
                textWriter.Write("\n   ");
            }

            if (exception is not null) {
                textWriter.Write(exception.ToString().Replace("\n", "\n   "));
            }

            textWriter.WriteLine(AnsiReset);
        }
    }

    private void WriteCategory<TState>(LogEntry<TState> logEntry, TextWriter textWriter) {
        int lastSeparatorPosition = _options.IncludeNamespaces ? -1 : logEntry.Category.LastIndexOf('.', logEntry.Category.Length - 2);

        ReadOnlySpan<char> category = lastSeparatorPosition != -1 ? logEntry.Category.AsSpan(lastSeparatorPosition + 1) : logEntry.Category.AsSpan();

        int categoryLength = category.Length;
        _maxCategoryLength = Math.Max(_maxCategoryLength, categoryLength);
#if NET6_0_OR_GREATER
        textWriter.Write(category);
#else
        textWriter.Write(category.ToString());
#endif

        if (categoryLength >= _maxCategoryLength) {
            _maxCategoryLength = categoryLength;
        } else {
            ReadOnlySpan<char> padding = Padding.AsSpan(0, Math.Max(0, Math.Min(_maxCategoryLength, MaxPaddedCategoryLength) - categoryLength));
#if NET6_0_OR_GREATER
            textWriter.Write(padding);
#else
            textWriter.Write(padding.ToString());
#endif
        }
    }

    private string FormatTime(DateTimeOffset now) => now.ToString(_options.TimestampFormat ?? DefaultDateFormat);

    private static string FormatLevel(LogLevel level) => level switch {
        LogLevel.Trace       => "\u001b[0;90m t",
        LogLevel.Debug       => " d",
        LogLevel.Information => "\u001b[0;36m i",
        LogLevel.Warning     => "\u001b[30;43m W",
        LogLevel.Error       => "\u001b[97;41m E",
        LogLevel.Critical    => "\u001b[97;41m C",
        _                    => "  "
    };

    public class UnfuckedConsoleFormatterOptions: ConsoleFormatterOptions {

        public bool IncludeNamespaces { get; set; }
        public string ColumnSeparator { get; set; } = " | ";

    }

}