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

    private static readonly ConcurrentDictionary<int, WeakReference<IUnfuckedHttpHandler>?> HTTP_CLIENT_HANDLER_CACHE = new();

    private static readonly Lazy<FieldInfo> HTTP_CLIENT_HANDLER_FIELD = new(() => typeof(HttpMessageInvoker).GetFields(BindingFlags.NonPublic | BindingFlags.Instance)
        .First(field => field.FieldType == typeof(HttpMessageHandler)), LazyThreadSafetyMode.PublicationOnly);

    private readonly FilterContext baseFilterContext;
    private readonly bool          disposeInnerHandler;

#if NET8_0_OR_GREATER
    private readonly IMeterFactory? wireLoggingMeterFactory;
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
    public UnfuckedHttpHandler(HttpMessageHandler? innerHandler = null, IClientConfig? configuration = null): base(innerHandler ??
#if NETCOREAPP2_1_OR_GREATER
        new SocketsHttpHandler {
            PooledConnectionLifetime = TimeSpan.FromHours(1),
            ConnectTimeout           = TimeSpan.FromSeconds(10),
            AutomaticDecompression   = DecompressionMethods.All,
            // MaxConnectionsPerServer defaults to MAX_INT, so we don't need to increase it here
#if NET8_0_OR_GREATER
            MeterFactory = new WireLogFilter.WireLoggingMeterFactory()
#endif
        }
#else
        new HttpClientHandler {AutomaticDecompression = DecompressionMethods.GZip|DecompressionMethods.Deflate}
#endif
    ) {
        ClientConfig        = configuration ?? new ClientConfig();
        baseFilterContext   = new FilterContext(this, ClientConfig);
        disposeInnerHandler = innerHandler == null;

#if NET8_0_OR_GREATER
        if (innerHandler == null) {
            wireLoggingMeterFactory = ((SocketsHttpHandler) InnerHandler!).MeterFactory;
        }
#endif
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

        try {
            foreach (ClientRequestFilter requestFilter in config?.RequestFilters ?? RequestFilters) {
                HttpRequestMessage newRequest = await requestFilter.Filter(request, filterContext, cancellationToken).ConfigureAwait(false);
                if (request != newRequest) {
                    request.Dispose();
                    request = newRequest;
                }
            }
        } catch (ProcessingException) {
            request.Dispose();
            throw;
        }

        HttpResponseMessage response = await TestableSendAsync(request, cancellationToken).ConfigureAwait(false);

        try {
            foreach (ClientResponseFilter responseFilter in config?.ResponseFilters ?? ResponseFilters) {
                HttpResponseMessage newResponse = await responseFilter.Filter(response, filterContext, cancellationToken).ConfigureAwait(false);
                if (response != newResponse) {
                    response.Dispose();
                    response = newResponse;
                }
            }
        } catch (ProcessingException) {
            request.Dispose();
            response.Dispose();
            throw;
        }

        return response;
    }

    /// <inheritdoc />
    public virtual Task<HttpResponseMessage> TestableSendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => base.SendAsync(request, cancellationToken);

    private UnfuckedHttpHandler With(IClientConfig newConfig) {
        ClientConfig = newConfig;
        return this;
    }

    public IUnfuckedHttpHandler Register(Registrable registrable) => With(ClientConfig.Register(registrable));

    public IUnfuckedHttpHandler Register<Option>(Registrable<Option> registrable, Option registrationOption) => With(ClientConfig.Register(registrable, registrationOption));

    public IUnfuckedHttpHandler Property<T>(PropertyKey<T> key, T? value) where T: notnull => With(ClientConfig.Property(key, value));

    [Pure]
    public bool Property<T>(PropertyKey<T> key,
#if !NETSTANDARD2_0
                            [NotNullWhen(true)]
#endif
                            out T? existingValue) where T: notnull => ClientConfig.Property(key, out existingValue);

    [Pure]
    IUnfuckedHttpHandler Configurable<IUnfuckedHttpHandler>.Property<T>(PropertyKey<T> key, T? newValue) where T: default => Property(key, newValue);

    protected override void Dispose(bool disposing) {
        if (!disposed) {
            disposed = true;
            if (disposing) {
                if (disposeInnerHandler) {
                    InnerHandler?.Dispose();
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
        }
        base.Dispose(disposing);
    }

}

/// <summary>
/// This is used when a consumer passes an IUnfuckedHttpHandler to an UnfuckedHttpClient constructor. Just because it implements IUnfuckedHttpHandler doesn't mean it's a subclass of HttpMessageHandler, and Microsoft stupidly decided to never use interfaces for anything. This class is an adapter that actually has the superclass needed to be used as an HttpMessageHandler.
/// </summary>
internal class IUnfuckedHttpHandlerWrapper(IUnfuckedHttpHandler realHandler): HttpMessageHandler {

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => realHandler.FilterAndSendAsync(request, cancellationToken);

}