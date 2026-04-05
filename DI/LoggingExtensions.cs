using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Globalization;
using Unfucked.DI.Logging;
using Unfucked.DI.Logging.Internationalized;

namespace Unfucked;

public static class LoggingExtensions {

    /// <summary>
    /// <para>Format console logging output using <see cref="UnfuckedConsoleFormatter"/>, which prints messages with a level character (like 'i'), ISO 8601-like date and time with milliseconds, class (by default the simple name), message, and any stack trace, separated by vertical pipes (' | '), for example:</para>
    /// <para><c> i | 2024-09-08 13:27:00.000 | Program | Application starting</c></para>
    /// </summary>
    /// <param name="logging">Application builder's <see cref="HostApplicationBuilder.Logging"/>.</param>
    /// <param name="options">Options to pass to the formatter to disable colored output, show fully-qualified class names, change the column separator, or change the datetime format.</param>
    public static ILoggingBuilder AddUnfuckedConsole(this ILoggingBuilder logging, Action<UnfuckedConsoleFormatterOptions>? options = null) {
        logging.AddConsole(opts => opts.FormatterName = UnfuckedConsoleFormatter.Id);
        if (options != null) {
            logging.AddConsoleFormatter<UnfuckedConsoleFormatter, UnfuckedConsoleFormatterOptions>(options);
        } else {
            logging.AddConsoleFormatter<UnfuckedConsoleFormatter, UnfuckedConsoleFormatterOptions>();
        }
        return logging;
    }

    /// <summary>
    /// <para>Increase the level of log messages from certain categories/classes with certain event IDs, because the original developers foolishly logged errors at the debug level in the same class that logs lots of noisy, low-severity debug messages too, so you can't just set the log level for the provider to debug for this class without getting flooded with extra garbage.</para>
    /// <para>For example, if you have mismatched serialization settings between your sender and receiver, SignalR by default won't log anything, until you turn on debug logs for the correct class, at which point you get that error message as well as way too many unrelated logs that cry wolf.</para>
    /// <para>To fix this problem, this logger provider increases the log level of certain log messages in that class from debug to a higher level like warning, based on their event ID, so that messages which truly represent errors and other unexpected behavior are shown at the correct level.</para>
    /// <para>Usage:</para>
    /// <para>1. Register this logger provider by calling <see cref="AmplifyMessageLevels"/>. In the options callback, call <see cref="AmplifiedLogOptions.Amplify"/> one or more times to give it the class name or category of the log message source, the desired new log level, and one or more event IDs that should be changed to the new log level.</para>
    /// <para><c>WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
    /// builder.Logging.AmplifyMessageLevels(options =&gt; options.Amplify("Microsoft.AspNetCore.SignalR.Internal.DefaultHubDispatcher", LogLevel.Warning, 2, 3, 5, 11, 13, 14, 15, 19, 21, 22, 23, 24));</c></para>
    /// <para>2. Set your application log level, in <c>appsettings.json</c> or wherever you set them. Make sure the level for the categories you specified (or their ancestors) is both greater than the original level (like debug) so that the noisy unwanted messages are hidden, and less than or equal to the amplified level (like warning), so you can see the important messages you amplified.</para>
    /// <para><c>{
    ///     "logging": {
    ///         "logLevel": {
    ///             "Microsoft.AspNetCore": "warning"
    ///         }
    ///     }
    /// }</c></para>
    /// <para>This will change the log messages from <c>Microsoft.AspNetCore.SignalR.Internal.DefaultHubDispatcher</c> which have any of the specified event IDs from their default debug level to warning. Other messages from that category with different event IDs will be logged at their original levels.</para>
    /// </summary>
    public static ILoggingBuilder AmplifyMessageLevels(this ILoggingBuilder loggingBuilder, Action<AmplifiedLogOptions> options) {
        IDictionary<string, IDictionary<int, LogLevel>> categoryAndEventIdToAmplifiedLevels = new Dictionary<string, IDictionary<int, LogLevel>>();
        options(new AmplifiedLogOptions(categoryAndEventIdToAmplifiedLevels));

        loggingBuilder.Services.AddSingleton<ILoggerProvider>(provider => new AmplifyingLoggerProvider(provider, categoryAndEventIdToAmplifiedLevels));

        foreach (string categoryToAmplify in categoryAndEventIdToAmplifiedLevels.Keys) {
            loggingBuilder.AddFilter<AmplifyingLoggerProvider>(categoryToAmplify, LogLevel.Trace);
        }

        return loggingBuilder;
    }

#pragma warning disable CA2254

    /// <inheritdoc cref="LoggerExtensions.LogDebug(ILogger, EventId, Exception?, string?, object?[])" />
    public static void Debug(this ILogger logger, EventId eventId, Exception? exception, [StructuredMessageTemplate] string? message, params object?[] args) =>
        Log(logger, LogLevel.Debug, eventId, exception, message, args);

    /// <inheritdoc cref="LoggerExtensions.LogDebug(ILogger, EventId, string?, object?[])" />
    public static void Debug(this ILogger logger, EventId eventId, [StructuredMessageTemplate] string? message, params object?[] args) => Log(logger, LogLevel.Debug, eventId, message, args);

    /// <inheritdoc cref="LoggerExtensions.LogDebug(ILogger, Exception?, string?, object?[])" />
    public static void Debug(this ILogger logger, Exception? exception, [StructuredMessageTemplate] string? message, params object?[] args) => Log(logger, LogLevel.Debug, exception, message, args);

    /// <inheritdoc cref="LoggerExtensions.LogDebug(ILogger, string?, object?[])" />
    public static void Debug(this ILogger logger, [StructuredMessageTemplate] string? message, params object?[] args) => Log(logger, LogLevel.Debug, message, args);

    /// <inheritdoc cref="LoggerExtensions.LogTrace(ILogger, EventId, Exception?, string?, object?[])" />
    public static void Trace(this ILogger logger, EventId eventId, Exception? exception, [StructuredMessageTemplate] string? message, params object?[] args) =>
        Log(logger, LogLevel.Trace, eventId, exception, message, args);

    /// <inheritdoc cref="LoggerExtensions.LogTrace(ILogger, EventId, string?, object?[])" />
    public static void Trace(this ILogger logger, EventId eventId, [StructuredMessageTemplate] string? message, params object?[] args) => Log(logger, LogLevel.Trace, eventId, message, args);

    /// <inheritdoc cref="LoggerExtensions.LogTrace(ILogger, Exception?, string?, object?[])" />
    public static void Trace(this ILogger logger, Exception? exception, [StructuredMessageTemplate] string? message, params object?[] args) => Log(logger, LogLevel.Trace, exception, message, args);

    /// <inheritdoc cref="LoggerExtensions.LogTrace(ILogger, string?, object?[])" />
    public static void Trace(this ILogger logger, [StructuredMessageTemplate] string? message, params object?[] args) => Log(logger, LogLevel.Trace, message, args);

    /// <inheritdoc cref="LoggerExtensions.LogInformation(ILogger, EventId, Exception?, string?, object?[])" />
    public static void Info(this ILogger logger, EventId eventId, Exception? exception, [StructuredMessageTemplate] string? message, params object?[] args) =>
        Log(logger, LogLevel.Information, eventId, exception, message, args);

    /// <inheritdoc cref="LoggerExtensions.LogInformation(ILogger, EventId, string?, object?[])" />
    public static void Info(this ILogger logger, EventId eventId, [StructuredMessageTemplate] string? message, params object?[] args) => Log(logger, LogLevel.Information, eventId, message, args);

    /// <inheritdoc cref="LoggerExtensions.LogInformation(ILogger, Exception?, string?, object?[])" />
    public static void Info(this ILogger logger, Exception? exception, [StructuredMessageTemplate] string? message, params object?[] args) =>
        Log(logger, LogLevel.Information, exception, message, args);

    /// <inheritdoc cref="LoggerExtensions.LogInformation(ILogger, string?, object?[])" />
    public static void Info(this ILogger logger, [StructuredMessageTemplate] string? message, params object?[] args) => Log(logger, LogLevel.Information, message, args);

    /// <inheritdoc cref="LoggerExtensions.LogWarning(ILogger, EventId, Exception?, string?, object?[])" />
    public static void Warn(this ILogger logger, EventId eventId, Exception? exception, [StructuredMessageTemplate] string? message, params object?[] args) =>
        Log(logger, LogLevel.Warning, eventId, exception, message, args);

    /// <inheritdoc cref="LoggerExtensions.LogWarning(ILogger, EventId, string?, object?[])" />
    public static void Warn(this ILogger logger, EventId eventId, [StructuredMessageTemplate] string? message, params object?[] args) => Log(logger, LogLevel.Warning, eventId, message, args);

    /// <inheritdoc cref="LoggerExtensions.LogWarning(ILogger, Exception?, string?, object?[])" />
    public static void Warn(this ILogger logger, Exception? exception, [StructuredMessageTemplate] string? message, params object?[] args) => Log(logger, LogLevel.Warning, exception, message, args);

    /// <inheritdoc cref="LoggerExtensions.LogWarning(ILogger, string?, object?[])" />
    public static void Warn(this ILogger logger, [StructuredMessageTemplate] string? message, params object?[] args) => Log(logger, LogLevel.Warning, message, args);

    /// <inheritdoc cref="LoggerExtensions.LogError(ILogger, EventId, Exception?, string?, object?[])" />
    public static void Error(this ILogger logger, EventId eventId, Exception? exception, [StructuredMessageTemplate] string? message, params object?[] args) =>
        Log(logger, LogLevel.Error, eventId, exception, message, args);

    /// <inheritdoc cref="LoggerExtensions.LogError(ILogger, EventId, string?, object?[])" />
    public static void Error(this ILogger logger, EventId eventId, [StructuredMessageTemplate] string? message, params object?[] args) => Log(logger, LogLevel.Error, eventId, message, args);

    /// <inheritdoc cref="LoggerExtensions.LogError(ILogger, Exception?, string?, object?[])" />
    public static void Error(this ILogger logger, Exception? exception, [StructuredMessageTemplate] string? message, params object?[] args) => Log(logger, LogLevel.Error, exception, message, args);

    /// <inheritdoc cref="LoggerExtensions.LogError(ILogger, string?, object?[])" />
    public static void Error(this ILogger logger, [StructuredMessageTemplate] string? message, params object?[] args) => Log(logger, LogLevel.Error, message, args);

    /// <inheritdoc cref="LoggerExtensions.LogCritical(ILogger, EventId, Exception?, string?, object?[])" />
    public static void Critical(this ILogger logger, EventId eventId, Exception? exception, [StructuredMessageTemplate] string? message, params object?[] args) =>
        Log(logger, LogLevel.Critical, eventId, exception, message, args);

    /// <inheritdoc cref="LoggerExtensions.LogCritical(ILogger, EventId, string?, object?[])" />
    public static void Critical(this ILogger logger, EventId eventId, [StructuredMessageTemplate] string? message, params object?[] args) => Log(logger, LogLevel.Critical, eventId, message, args);

    /// <inheritdoc cref="LoggerExtensions.LogCritical(ILogger, Exception?, string?, object?[])" />
    public static void Critical(this ILogger logger, Exception? exception, [StructuredMessageTemplate] string? message, params object?[] args) =>
        Log(logger, LogLevel.Critical, exception, message, args);

    /// <inheritdoc cref="LoggerExtensions.LogCritical(ILogger, string?, object?[])" />
    public static void Critical(this ILogger logger, [StructuredMessageTemplate] string? message, params object?[] args) => Log(logger, LogLevel.Critical, message, args);

    private static void Log(ILogger logger, LogLevel level, EventId eventId, Exception? exception, string? message, object?[] args) =>
        logger.Log(level, eventId, new UnfuckedFormattedLogValues(message, args), exception, messageFormatter);

    private static void Log(ILogger logger, LogLevel level, EventId eventId, string? message, object?[] args) => Log(logger, level, eventId, null, message, args);

    private static void Log(ILogger logger, LogLevel level, Exception? exception, string? message, object?[] args) => Log(logger, level, 0, exception, message, args);

    private static void Log(ILogger logger, LogLevel level, string? message, object?[] args) => Log(logger, level, 0, null, message, args);

    private static readonly Func<UnfuckedFormattedLogValues, Exception?, string> messageFormatter = MessageFormatter;
    private static string MessageFormatter(UnfuckedFormattedLogValues state, Exception? error) => state.ToString();

    /// <summary>
    /// <para>The culture used to format values in log messages, such as numbers and dates. Useful for making your percentages not look fucked up, like "100 %".</para>
    /// <para>Used when you call the Unfucked logging extension methods like <see cref="Info(ILogger,string?,object?[])"/>, but not the Microsoft logging extension methods like <see cref="LoggerExtensions.LogInformation(ILogger,string?,object?[])"/>. </para>
    /// <para>Defaults to <see cref="CultureInfo.CurrentCulture"/> when the first message in the process is logged.</para>
    /// </summary>
    public static IFormatProvider LogFormattingCulture {
        get => UnfuckedLogValuesFormatter.Culture;
        set => UnfuckedLogValuesFormatter.Culture = value;
    }

#pragma warning restore CA2254

}