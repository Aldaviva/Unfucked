using System.Diagnostics.Contracts;
using System.Reflection;
using Unfucked.HTTP.Config;
using Unfucked.HTTP.Filters;
using Unfucked.HTTP.Serialization;

namespace Unfucked.HTTP;

public interface IUnfuckedHttpHandler: Configurable<IUnfuckedHttpHandler> {

    /// <inheritdoc cref="DelegatingHandler.InnerHandler" />
    HttpMessageHandler? InnerHandler { get; }

    IClientConfig ClientConfig { get; }

    /// <summary>
    /// This is the method to mock/fake/stub/spy if you want to inspect HTTP requests and return fake responses instead of real ones.
    /// </summary>
    Task<HttpResponseMessage> TestableSendAsync(HttpRequestMessage request, CancellationToken cancellationToken);

    /// <summary>
    /// This should just delegate to <see cref="UnfuckedHttpHandler.SendAsync"/>, it's only here because the method was originally only specified on a superclass, not an interface.
    /// </summary>
    Task<HttpResponseMessage> FilterAndSendAsync(HttpRequestMessage request, CancellationToken cancellationToken);

}

public class UnfuckedHttpHandler: DelegatingHandler, IUnfuckedHttpHandler {

    private static readonly IDictionary<int, WeakReference<IUnfuckedHttpHandler>?> HttpClientHandlerCache = new Dictionary<int, WeakReference<IUnfuckedHttpHandler>?>();

    private static FieldInfo? _handlerField;

    public IClientConfig ClientConfig { get; private set; } = new ClientConfig();

    [Pure]
    public IReadOnlyList<ClientRequestFilter> RequestFilters => ClientConfig.RequestFilters;

    [Pure]
    public IReadOnlyList<ClientResponseFilter> ResponseFilters => ClientConfig.ResponseFilters;

    [Pure]
    public IEnumerable<MessageBodyReader> MessageBodyReaders => ClientConfig.MessageBodyReaders;

    // HttpClientHandler automatically uses SocketsHttpHandler on .NET Core ≥ 2.1, or HttpClientHandler otherwise
    public UnfuckedHttpHandler(HttpMessageHandler? innerHandler = null): base(innerHandler ??
#if NETCOREAPP2_1_OR_GREATER
        new SocketsHttpHandler {
            PooledConnectionLifetime = TimeSpan.FromHours(1),
            ConnectTimeout = TimeSpan.FromSeconds(10)
        }
#else
        new HttpClientHandler()
#endif
    ) { }

    [Pure]
    public static HttpClient CreateClient(HttpMessageHandler? innerHandler = null) => new UnfuckedHttpClient(innerHandler);

    internal static IUnfuckedHttpHandler? FindHandler(HttpClient httpClient) {
        if (httpClient is IUnfuckedHttpClient client) {
            return client.Handler;
        }

        IUnfuckedHttpHandler? handler = null;
        if (!HttpClientHandlerCache.TryGetValue(httpClient.GetHashCode(), out WeakReference<IUnfuckedHttpHandler>? handlerHolder) || !(handlerHolder?.TryGetTarget(out handler) ?? true)) {
            _handlerField ??= typeof(HttpMessageInvoker).GetFields(BindingFlags.NonPublic | BindingFlags.Instance).FirstOrDefault(field => field.FieldType == typeof(HttpMessageHandler));
            handler       =   FindDescendantHandler((HttpMessageHandler?) _handlerField?.GetValue(httpClient));
            CacheClientHandler(httpClient, handler);
        }

        return handler;

        static UnfuckedHttpHandler? FindDescendantHandler(HttpMessageHandler? parent) => parent switch {
            null                  => null,
            UnfuckedHttpHandler f => f,
            DelegatingHandler d   => FindDescendantHandler(d.InnerHandler),
            _                     => null
        };
    }

    internal static void CacheClientHandler(HttpClient client, IUnfuckedHttpHandler? handler) =>
        HttpClientHandlerCache[client.GetHashCode()] = handler is null ? null : new WeakReference<IUnfuckedHttpHandler>(handler);

    /// <inheritdoc />
    public Task<HttpResponseMessage> FilterAndSendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => SendAsync(request, cancellationToken);

    /// <inheritdoc />
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) {
        foreach (ClientRequestFilter requestFilter in RequestFilters) {
            await requestFilter.Filter(ref request, cancellationToken).ConfigureAwait(false);
        }

        HttpResponseMessage response = await TestableSendAsync(request, cancellationToken).ConfigureAwait(false);

        foreach (ClientResponseFilter responseFilter in ResponseFilters) {
            await responseFilter.Filter(ref response, cancellationToken).ConfigureAwait(false);
        }

        return response;
    }

    /// <inheritdoc />
    public virtual Task<HttpResponseMessage> TestableSendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => base.SendAsync(request, cancellationToken);

    private UnfuckedHttpHandler With(IClientConfig newConfig) {
        ClientConfig = newConfig;
        return this;
    }

    public IUnfuckedHttpHandler Register(ClientRequestFilter? filter, int position = Config.ClientConfig.LastFilterPosition) =>
        With(ClientConfig.Register(filter, position));

    public IUnfuckedHttpHandler Register(ClientResponseFilter? filter, int position = Config.ClientConfig.LastFilterPosition) =>
        With(ClientConfig.Register(filter, position));

    public IUnfuckedHttpHandler Register(MessageBodyReader reader) => With(ClientConfig.Register(reader));

    IUnfuckedHttpHandler Configurable<IUnfuckedHttpHandler>.Register(ClientResponseFilter? filter, int position) => Register(filter, position);

    IUnfuckedHttpHandler Configurable<IUnfuckedHttpHandler>.Register(ClientRequestFilter? filter, int position) => Register(filter, position);

    IUnfuckedHttpHandler Configurable<IUnfuckedHttpHandler>.Register(MessageBodyReader reader) => Register(reader);

    public IUnfuckedHttpHandler Property<T>(PropertyKey<T> key, T? value) where T: notnull => With(ClientConfig.Property(key, value));

    [Pure]
    public bool Property<T>(PropertyKey<T> key,
#if !NETSTANDARD2_0
                            [NotNullWhen(true)]
#endif
                            out T? existingValue) where T: notnull => ClientConfig.Property(key, out existingValue);

    [Pure]
    IUnfuckedHttpHandler Configurable<IUnfuckedHttpHandler>.Property<T>(PropertyKey<T> key, T? value) where T: default => Property(key, value);

}

/// <summary>
/// This is used when a consumer passes an IUnfuckedHttpHandler to an UnfuckedHttpClient constructor. Just because it implements IUnfuckedHttpHandler doesn't mean it's a subclass of HttpMessageHandler, and Microsoft stupidly decided to never use interfaces for anything. This class is an adapter that actually has the superclass needed to be used as an HttpMessageHandler.
/// </summary>
internal class IUnfuckedHttpHandlerWrapper(IUnfuckedHttpHandler realHandler): HttpMessageHandler {

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => realHandler.FilterAndSendAsync(request, cancellationToken);

}