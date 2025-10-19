using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Unfucked.Logging;

/// <summary>
/// <para>Increase the level of log messages from certain categories/classes with certain event IDs, because the original developers foolishly logged errors at the debug level in the same class that logs lots of noisy, low-severity debug messages too, so you can't just set the log level for the provider to debug for this class without getting flooded with extra garbage.</para>
/// <para>For example, if you have mismatched serialization settings between your sender and receiver, SignalR by default won't log anything, until you turn on debug logs for the correct class, at which point you get that error message as well as way too many unrelated logs that cry wolf.</para>
/// <para>To fix this problem, this logger provider increases the log level of certain log messages in that class from debug to a higher level like warning, based on their event ID, so that messages which truly represent errors and other unexpected behavior are shown at the correct level.</para>
/// <para>Usage:</para>
/// <para>1. Register this logger provider by calling <see cref="LoggingExtensions.AmplifyMessageLevels"/>. In the options callback, call <see cref="AmplifiedLogOptions.Amplify"/> one or more times to give it the class name or category of the log message source, the desired new log level, and one or more event IDs that should be changed to the new log level.</para>
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
[ProviderAlias("amplify")] // case-sensitive, strangely
public class AmplifyingLoggerProvider: ILoggerProvider {

    private readonly IServiceProvider                                context;
    private readonly IDictionary<string, IDictionary<int, LogLevel>> categoryAndEventIdToAmplifiedLevels;
    private readonly ThreadLocal<SemaphoreSlim>                      loggerCreationLock = new(() => new SemaphoreSlim(1), true);

    internal AmplifyingLoggerProvider(IServiceProvider context, IDictionary<string, IDictionary<int, LogLevel>> categoryAndEventIdToAmplifiedLevels) {
        this.context                             = context;
        this.categoryAndEventIdToAmplifiedLevels = categoryAndEventIdToAmplifiedLevels;
    }

    /// <inheritdoc />
    // ExceptionAdjustment: P:System.Threading.ThreadLocal`1.Value get -T:System.MissingMemberException
    // ExceptionAdjustment: M:System.Threading.SemaphoreSlim.Release -T:System.Threading.SemaphoreFullException
    public ILogger CreateLogger(string categoryName) {
        if (categoryAndEventIdToAmplifiedLevels.TryGetValue(categoryName, out IDictionary<int, LogLevel>? eventIdsToAmplify) && loggerCreationLock.Value!.Wait(0)) {
            try {
                return new EventIdAmplifyingLogger(eventIdsToAmplify, context.GetRequiredService<ILoggerFactory>().CreateLogger(categoryName));
            } finally {
                // prevents infinite loops when we need to get the upstream compound logger for the given class, but that involves the factory requesting another instance of our own logger, since we're a member of the compound logger itself
                loggerCreationLock.Value.Release();
            }
        }
        return NullLogger.Instance;
    }

    private class EventIdAmplifyingLogger(IDictionary<int, LogLevel> eventIdsToAmplify, ILogger actualLogger): ILogger {

        public bool IsEnabled(LogLevel logLevel) => false;

        public void Log<State>(LogLevel originalLogLevel, EventId eventId, State state, Exception? exception, Func<State, Exception?, string> formatter) {
            LogLevel amplifiedLogLevel = eventIdsToAmplify.TryGetValue(eventId.Id, out LogLevel amplifiedValue) ? amplifiedValue : originalLogLevel;
            if (actualLogger.IsEnabled(amplifiedLogLevel) && !actualLogger.IsEnabled(originalLogLevel)) {
                actualLogger.Log(amplifiedLogLevel, eventId, state, exception, formatter);
            }
        }

        public IDisposable? BeginScope<State>(State state) where State: notnull => actualLogger.BeginScope(state);

    }

    /// <inheritdoc />
    public void Dispose() {
        IList<SemaphoreSlim> locks = loggerCreationLock.Values;
        loggerCreationLock.Dispose();
        foreach (IDisposable @lock in locks) {
            @lock.Dispose();
        }
        GC.SuppressFinalize(this);
    }

}

public class AmplifiedLogOptions {

    private readonly IDictionary<string, IDictionary<int, LogLevel>> categoryAndEventIdToAmplifiedLevels;

    internal AmplifiedLogOptions(IDictionary<string, IDictionary<int, LogLevel>> categoryAndEventIdToAmplifiedLevels) {
        this.categoryAndEventIdToAmplifiedLevels = categoryAndEventIdToAmplifiedLevels;
    }

    /// <summary>
    /// Specify the category and event IDs of log messages to increase to a different level.
    /// </summary>
    /// <param name="category">Typically the fully-qualified name of the class that emits the log message, such as <c>Microsoft.AspNetCore.SignalR.Internal.DefaultHubDispatcher</c>.</param>
    /// <param name="amplifiedLevel">The new log level to re-emit the log messages at, such as <see cref="LogLevel.Warning"/>, should be higher than the original level.</param>
    /// <param name="eventIdsToAmplify">One or more event IDs of log messages to re-emit at a higher level, such as <c>[19, 22]</c>.</param>
    public AmplifiedLogOptions Amplify(string category, LogLevel amplifiedLevel, params IEnumerable<int> eventIdsToAmplify) {
        IDictionary<int, LogLevel> eventIdToLevels = categoryAndEventIdToAmplifiedLevels.GetOrAdd(category, () => new Dictionary<int, LogLevel>(), out bool _);
        foreach (int eventId in eventIdsToAmplify) {
            eventIdToLevels[eventId] = amplifiedLevel;
        }
        return this;
    }

    /// <summary>
    /// Specify the category and event IDs of log messages to increase to a different level.
    /// </summary>
    /// <typeparam name="Category">The class that emits the log messages.</typeparam>
    /// <param name="amplifiedLevel">The new log level to re-emit the log messages at, such as <see cref="LogLevel.Warning"/>, should be higher than the original level.</param>
    /// <param name="eventIdsToAmplify">One or more event IDs of log messages to re-emit at a higher level, such as <c>[19, 22]</c>.</param>
    public AmplifiedLogOptions Amplify<Category>(LogLevel amplifiedLevel, params IEnumerable<int> eventIdsToAmplify) => Amplify(nameof(Category), amplifiedLevel, eventIdsToAmplify);

}