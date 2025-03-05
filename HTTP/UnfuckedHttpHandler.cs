using System.Diagnostics.Contracts;
using System.Reflection;
using Unfucked.HTTP.Config;
using Unfucked.HTTP.Filters;
using Unfucked.HTTP.Serialization;

namespace Unfucked.HTTP;

public interface IFilteringHttpClientHandler: IHttpConfiguration<IFilteringHttpClientHandler> {

    /// <inheritdoc cref="DelegatingHandler.InnerHandler" />
    public HttpMessageHandler? InnerHandler { get; }

    Task<HttpResponseMessage> TestableSendAsync(HttpRequestMessage request, CancellationToken cancellationToken);

}

public class UnfuckedHttpHandler: DelegatingHandler, IFilteringHttpClientHandler, IHttpConfiguration<UnfuckedHttpHandler> {

    private static readonly IDictionary<int, WeakReference<UnfuckedHttpHandler>?> HttpClientHandlerCache = new Dictionary<int, WeakReference<UnfuckedHttpHandler>?>();

    private static FieldInfo? _handlerField;

    internal HttpConfiguration ClientConfig { get; private set; } = new();

    [Pure]
    public IReadOnlyList<ClientRequestFilter> RequestFilters => ClientConfig.RequestFilters;

    [Pure]
    public IReadOnlyList<ClientResponseFilter> ResponseFilters => ClientConfig.ResponseFilters;

    [Pure]
    public IEnumerable<MessageBodyReader> MessageBodyReaders => ClientConfig.MessageBodyReaders;

    // HttpClientHandler automatically uses SocketsHttpHandler on .NET Core ≥ 2.1, or HttpClientHandler otherwise
    public UnfuckedHttpHandler(HttpMessageHandler? innerHandler = null): base(innerHandler ?? new HttpClientHandler()) { }

    [Pure]
    public static HttpClient CreateClient(HttpMessageHandler? innerHandler = null) {
        UnfuckedHttpHandler handler    = new(innerHandler);
        HttpClient          httpClient = new(handler);
        CacheClientHandler(httpClient, handler);
        return httpClient;
    }

    internal static UnfuckedHttpHandler? FindHandler(HttpClient httpClient) {
        if (httpClient is UnfuckedHttpClient client) {
            return client.Handler;
        }

        UnfuckedHttpHandler? handler = null;
        if (!HttpClientHandlerCache.TryGetValue(httpClient.GetHashCode(), out WeakReference<UnfuckedHttpHandler>? handlerHolder) || !(handlerHolder?.TryGetTarget(out handler) ?? true)) {
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

    internal static void CacheClientHandler(HttpClient client, UnfuckedHttpHandler? handler) =>
        HttpClientHandlerCache[client.GetHashCode()] = handler is null ? null : new WeakReference<UnfuckedHttpHandler>(handler);

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

    /// <summary>
    /// For testing with mocks/fakes/stubs/spies/dummies/fixtures
    /// </summary>
    public Task<HttpResponseMessage> TestableSendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => base.SendAsync(request, cancellationToken);

    private UnfuckedHttpHandler With(HttpConfiguration newConfig) {
        ClientConfig = newConfig;
        return this;
    }

    public UnfuckedHttpHandler Register(ClientRequestFilter? filter, int position = HttpConfiguration.LastFilterPosition) =>
        With(ClientConfig.Register(filter, position));

    public UnfuckedHttpHandler Register(ClientResponseFilter? filter, int position = HttpConfiguration.LastFilterPosition) =>
        With(ClientConfig.Register(filter, position));

    public UnfuckedHttpHandler Register(MessageBodyReader reader) => With(ClientConfig.Register(reader));

    IFilteringHttpClientHandler IHttpConfiguration<IFilteringHttpClientHandler>.Register(ClientResponseFilter? filter, int position) => Register(filter, position);

    IFilteringHttpClientHandler IHttpConfiguration<IFilteringHttpClientHandler>.Register(ClientRequestFilter? filter, int position) => Register(filter, position);

    IFilteringHttpClientHandler IHttpConfiguration<IFilteringHttpClientHandler>.Register(MessageBodyReader reader) => Register(reader);

    public UnfuckedHttpHandler Property<T>(PropertyKey<T> key, T? value) where T: notnull => With(ClientConfig.Property(key, value));

    [Pure]
    public bool Property<T>(PropertyKey<T> key,
#if !NETSTANDARD2_0
                            [NotNullWhen(true)]
#endif
                            out T? existingValue) where T: notnull => ClientConfig.Property(key, out existingValue);

    [Pure]
    IFilteringHttpClientHandler IHttpConfiguration<IFilteringHttpClientHandler>.Property<T>(PropertyKey<T> key, T? value) where T: default => Property(key, value);

}