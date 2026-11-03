using System.Collections.Concurrent;
using System.Diagnostics.Contracts;
using System.Net;
using System.Reflection;
using Unfucked.HTTP.Config;
using Unfucked.HTTP.Exceptions;
using Unfucked.HTTP.Filters;
using Unfucked.HTTP.Serialization;
#if NET8_0_OR_GREATER
using System.Diagnostics.Metrics;
#endif

namespace Unfucked.HTTP;

public interface IUnfuckedHttpHandler: Configurable<IUnfuckedHttpHandler> {

    /// <inheritdoc cref="DelegatingHandler.InnerHandler" />
    HttpMessageHandler? InnerHandler { get; }

    /// <summary>
    /// HTTP client configuration, including properties, request and response filters, and message body readers
    /// </summary>
    IClientConfig ClientConfig { get; }

    /// <summary>
    /// This is the method to mock/fake/stub/spy if you want to inspect HTTP requests and return fake responses instead of real ones.
    /// </summary>
    Task<HttpResponseMessage> TestableSendAsync(HttpRequestMessage request, CancellationToken cancellationToken);

    /// <summary>
    /// This should just delegate to <c>UnfuckedHttpHandler.SendAsync</c>, it's only here because the method was originally only specified on a superclass, not an interface.
    /// </summary>
    /// <exception cref="ProcessingException"></exception>
    Task<HttpResponseMessage> FilterAndSendAsync(HttpRequestMessage request, CancellationToken cancellationToken);

}

public class UnfuckedHttpHandler: DelegatingHandler, IUnfuckedHttpHandler {

    private static readonly  ConcurrentDictionary<int, WeakReference<IUnfuckedHttpHandler>?> HTTP_CLIENT_HANDLER_CACHE = new();
    private static readonly  TimeSpan                                                        DEFAULT_TIMEOUT           = new(0, 0, 30);
    internal static readonly string                                                          RequestTimedOutProperty   = $"{typeof(UnfuckedHttpHandler).Namespace}.timedOut";

    private static readonly Lazy<FieldInfo> HTTP_CLIENT_HANDLER_FIELD = new(static () => typeof(HttpMessageInvoker).GetFields(BindingFlags.NonPublic | BindingFlags.Instance)
        .First(static field => field.FieldType == typeof(HttpMessageHandler)), LazyThreadSafetyMode.PublicationOnly);

    private readonly FilterContext                                                         baseFilterContext;
    private readonly bool                                                                  disposeInnerHandler = true;
    private readonly Lazy<ConcurrentDictionary<HandlerSettings, DelegatingHandlerWrapper>> alternativeHandlers = new();

#if NET8_0_OR_GREATER
    private IMeterFactory? wireLoggingMeterFactory;
#endif

    private bool disposed;

    /// <inheritdoc />
    public IClientConfig ClientConfig { get; private set; }

    /// <inheritdoc />
    [Pure]
    public IReadOnlyList<ClientRequestFilter> RequestFilters => ClientConfig.RequestFilters;

    /// <inheritdoc />
    [Pure]
    public IReadOnlyList<ClientResponseFilter> ResponseFilters => ClientConfig.ResponseFilters;

    /// <inheritdoc />
    [Pure]
    public IEnumerable<MessageBodyReader> MessageBodyReaders => ClientConfig.MessageBodyReaders;

    /*
     * No-argument constructor overload lets FakeItEasy call this real constructor, which makes ClientConfig not a fake so registering JSON options aren't ignored, which would cause confusing errors
     * at test runtime. Default values for other constructor below wouldn't have been called by FakeItEasy. This avoids having to remember to call
     * options.WithArgumentsForConstructor(() => new UnfuckedHttpHandler(null, null)) when creating the fake.
     */
    public UnfuckedHttpHandler(): this(null) {}

    // HttpClientHandler automatically uses SocketsHttpHandler on .NET Core ≥ 2.1, or HttpClientHandler otherwise
    public UnfuckedHttpHandler(HttpMessageHandler? innerHandler = null, IClientConfig? configuration = null) {
        ClientConfig      = configuration ?? new ClientConfig();
        baseFilterContext = new FilterContext(this, ClientConfig);

        if (innerHandler is not null) {
            InnerHandler        = innerHandler;
            disposeInnerHandler = false;
        }
    }

    public static UnfuckedHttpHandler Create(HttpClient toClone, IClientConfig? configuration = null) =>
        new((HttpMessageHandler) HTTP_CLIENT_HANDLER_FIELD.Value.GetValue(toClone)!, configuration);

    internal static IUnfuckedHttpHandler? FindHandler(HttpClient httpClient) {
        if (httpClient is IHttpClient client) {
            return client.Handler;
        }

        IUnfuckedHttpHandler? handler = null;
        if (!HTTP_CLIENT_HANDLER_CACHE.TryGetValue(httpClient.GetHashCode(), out WeakReference<IUnfuckedHttpHandler>? handlerHolder) || !(handlerHolder?.TryGetTarget(out handler) ?? true)) {
            handler = findDescendantUnfuckedHandler((HttpMessageHandler?) HTTP_CLIENT_HANDLER_FIELD.Value.GetValue(httpClient));
            CacheClientHandler(httpClient, handler);
        }

        return handler;

        static UnfuckedHttpHandler? findDescendantUnfuckedHandler(HttpMessageHandler? parent) => parent switch {
            UnfuckedHttpHandler f => f,
            DelegatingHandler d   => findDescendantUnfuckedHandler(d.InnerHandler),
            _                     => null
        };
    }

    internal static void CacheClientHandler(HttpClient client, IUnfuckedHttpHandler? handler) =>
        HTTP_CLIENT_HANDLER_CACHE[client.GetHashCode()] = handler is null ? null : new WeakReference<IUnfuckedHttpHandler>(handler);

    /// <inheritdoc />
    public Task<HttpResponseMessage> FilterAndSendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => SendAsync(request, cancellationToken);

    /// <inheritdoc />
    /// <exception cref="ProcessingException">filter error</exception>
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) {
        IClientConfig? config        = (request as UnfuckedHttpRequestMessage)?.Config;
        FilterContext  filterContext = baseFilterContext with { Configuration = config ?? baseFilterContext.Configuration };

        TimeSpan                       timeout    = config?.Property(PropertyKey.Timeout, out TimeSpan configuredTimeout) ?? false ? configuredTimeout : DEFAULT_TIMEOUT;
        using CancellationTokenSource? timeoutCts = timeout != Timeout.InfiniteTimeSpan ? CancellationTokenSource.CreateLinkedTokenSource(cancellationToken) : null;
        cancellationToken = timeoutCts?.Token ?? cancellationToken;
        timeoutCts?.CancelAfter(timeout);
#pragma warning disable CS0618
#if NET5_0_OR_GREATER
        await using
#else
        using
#endif
            CancellationTokenRegistration? setTimedOut = timeoutCts?.Token.Register(static parameters => {
                (HttpRequestMessage req, TimeSpan timeout) = ((HttpRequestMessage req, TimeSpan timeout)) parameters!;
                req.Properties[RequestTimedOutProperty]    = timeout;
            }, (request, timeout));
#pragma warning restore CS0618

        HttpRequestMessage currentRequest = request;
        try {
            foreach (ClientRequestFilter requestFilter in config?.RequestFilters ?? RequestFilters) {
                HttpRequestMessage newRequest = await requestFilter.Filter(currentRequest, filterContext, cancellationToken).ConfigureAwait(false);
                if (currentRequest != newRequest) {
                    currentRequest.Dispose();
                    currentRequest = newRequest;
                }
            }
        } catch (ProcessingException) {
            currentRequest.Dispose();
            throw;
        }

        if (currentRequest is { Headers.TransferEncodingChunked: false, Content: { Headers.ContentLength: null } requestBody }) {
            /*
             * The request body is an as-yet unbuffered stream with an unknown length (HttpContent.TryComputeLength returned false),
             * but the user explicitly wants to not send chunked requests because they know the server rejects it,
             * so force the client to buffer the request body before sending so it can have a valid Content-Length and no Transfer-Encoding: chunked.
             */
            await requestBody.LoadIntoBufferAsync().ConfigureAwait(false);
        }

        HttpResponseMessage response = await TestableSendAsync(currentRequest, cancellationToken).ConfigureAwait(false);

        try {
            foreach (ClientResponseFilter responseFilter in config?.ResponseFilters ?? ResponseFilters) {
                HttpResponseMessage newResponse = await responseFilter.Filter(response, filterContext, cancellationToken).ConfigureAwait(false);
                if (response != newResponse) {
                    response.Dispose();
                    response = newResponse;
                }
            }
        } catch (ProcessingException) {
            currentRequest.Dispose();
            response.Dispose();
            throw;
        }

        return response;
    }

    /// <inheritdoc />
    public virtual Task<HttpResponseMessage> TestableSendAsync(HttpRequestMessage request, CancellationToken cancellationToken) {
        HandlerSettings handlerSettings = new() {
            AutomaticallyFollowRedirections = !((request as UnfuckedHttpRequestMessage)?.Config?.Property(PropertyKey.AutomaticallyFollowRedirections, out bool follow) ?? false) || follow
        };

        if (!handlerSettings.Equals(InnerHandler)) {
            return alternativeHandlers.Value.GetOrAddWithDisposal(handlerSettings, static (settings, innerHandler) =>
                    new DelegatingHandlerWrapper(BuildInnerHandler(settings, innerHandler)), out _, InnerHandler)
                .InternalSendAsync(request, cancellationToken);
        } else if (InnerHandler is null) {
            var innerHandler = BuildInnerHandler(handlerSettings, null);
            InnerHandler = innerHandler;
#if NET8_0_OR_GREATER
            wireLoggingMeterFactory = innerHandler.MeterFactory;
#endif
        }
        return base.SendAsync(request, cancellationToken);
    }

    private static
#if NETCOREAPP2_1_OR_GREATER
        SocketsHttpHandler
#else
        HttpClientHandler
#endif
        BuildInnerHandler(HandlerSettings settings, HttpMessageHandler? innerHandler) => new() {
        AllowAutoRedirect = settings.AutomaticallyFollowRedirections,
#if NETCOREAPP2_1_OR_GREATER
        PooledConnectionLifetime = TimeSpan.FromHours(1),
        ConnectTimeout = TimeSpan.FromSeconds(10),
        AutomaticDecompression = DecompressionMethods.All,
        // MaxConnectionsPerServer defaults to MAX_INT, so we don't need to increase it here
#if NET8_0_OR_GREATER
        MeterFactory = (innerHandler as SocketsHttpHandler)?.MeterFactory
#endif
#else
        AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
#endif
    };

    private UnfuckedHttpHandler With(IClientConfig newConfig) {
        ClientConfig = newConfig;
        return this;
    }

    public IUnfuckedHttpHandler Register(Registrable registrable) => With(ClientConfig.Register(registrable));

    public IUnfuckedHttpHandler Register<Option>(Registrable<Option> registrable, Option registrationOption) => With(ClientConfig.Register(registrable, registrationOption));

    public IUnfuckedHttpHandler Property<T>(PropertyKey<T> key, T? value) where T: notnull => With(ClientConfig.Property(key, value));

    [Pure]
    public bool Property<T>(PropertyKey<T> key,
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
                            [NotNullWhen(true)]
#endif
                            out T? existingValue) where T: notnull => ClientConfig.Property(key, out existingValue);

    [Pure]
    IUnfuckedHttpHandler Configurable<IUnfuckedHttpHandler>.Property<T>(PropertyKey<T> key, T? newValue) where T: default => Property(key, newValue);

    protected override void Dispose(bool disposing) {
        if (disposing && !disposed) {
            disposed = true;
            if (disposeInnerHandler) {
                InnerHandler?.Dispose();
            }
            foreach (DelegatingHandlerWrapper handler in alternativeHandlers is { IsValueCreated: true, Value: var handlers } ? handlers.Values : []) {
                handler.Dispose();
            }

            List<KeyValuePair<int, WeakReference<IUnfuckedHttpHandler>?>> evictions =
                HTTP_CLIENT_HANDLER_CACHE.Where(pair => pair.Value != null && (!pair.Value.TryGetTarget(out IUnfuckedHttpHandler? handler) || handler == this)).ToList();
            foreach (KeyValuePair<int, WeakReference<IUnfuckedHttpHandler>?> eviction in evictions) {
#if NET5_0_OR_GREATER
                HTTP_CLIENT_HANDLER_CACHE.TryRemove(eviction);
#else
                HTTP_CLIENT_HANDLER_CACHE.TryRemove(eviction.Key, out _);
#endif
            }

#if NET8_0_OR_GREATER
            wireLoggingMeterFactory?.Dispose();
#endif
        }
        base.Dispose(disposing);
    }

    private readonly struct HandlerSettings: IEquatable<HandlerSettings>, IEquatable<HttpMessageHandler> {

        public bool AutomaticallyFollowRedirections { get; init; }

        public bool Equals(HandlerSettings other) => AutomaticallyFollowRedirections == other.AutomaticallyFollowRedirections;
        public override bool Equals(object? obj) => (obj is HandlerSettings other && Equals(other)) || (obj is HttpMessageHandler other2 && Equals(other2));

        public bool Equals(HttpMessageHandler? other) =>
#if NETCOREAPP2_1_OR_GREATER
            (other as SocketsHttpHandler)?.AllowAutoRedirect == AutomaticallyFollowRedirections ||
#endif
            (other as HttpClientHandler)?.AllowAutoRedirect == AutomaticallyFollowRedirections;

        public static bool operator ==(HandlerSettings left, HandlerSettings right) => left.Equals(right);
        public static bool operator !=(HandlerSettings left, HandlerSettings right) => !left.Equals(right);

        public override int GetHashCode() => AutomaticallyFollowRedirections.GetHashCode();

    }

}

/// <summary>
/// This is used when a consumer passes an IUnfuckedHttpHandler to an UnfuckedHttpClient constructor. Just because it implements IUnfuckedHttpHandler doesn't mean it's a subclass of HttpMessageHandler, and Microsoft stupidly decided to never use interfaces for anything. This class is an adapter that actually has the superclass needed to be used as an HttpMessageHandler.
/// </summary>
internal sealed class IUnfuckedHttpHandlerWrapper(IUnfuckedHttpHandler realHandler): HttpMessageHandler {

    /// <exception cref="ProcessingException"></exception>
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => realHandler.FilterAndSendAsync(request, cancellationToken);

}

internal sealed class DelegatingHandlerWrapper(HttpMessageHandler d): DelegatingHandler(d) {

    internal Task<HttpResponseMessage> InternalSendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => SendAsync(request, cancellationToken);

}