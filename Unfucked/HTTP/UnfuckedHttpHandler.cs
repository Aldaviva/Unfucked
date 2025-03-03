using System.Reflection;

namespace Unfucked.HTTP;

public interface IFilteringHttpClientHandler: IHttpConfiguration<IFilteringHttpClientHandler> {

    /// <inheritdoc cref="DelegatingHandler.InnerHandler" />
    public HttpMessageHandler? InnerHandler { get; }

    Task<HttpResponseMessage> TestableSendAsync(HttpRequestMessage request, CancellationToken cancellationToken);

}

public class UnfuckedHttpHandler: DelegatingHandler, IFilteringHttpClientHandler, IHttpConfiguration<UnfuckedHttpHandler> {

    private static readonly IDictionary<int, WeakReference<UnfuckedHttpHandler>?> HttpClientHandlerCache = new Dictionary<int, WeakReference<UnfuckedHttpHandler>?>();

    internal HttpConfiguration ClientConfig { get; private set; } = new();

    public IReadOnlyList<ClientRequestFilter> RequestFilters => ClientConfig.RequestFilters;
    public IReadOnlyList<ClientResponseFilter> ResponseFilters => ClientConfig.ResponseFilters;

    // HttpClientHandler automatically uses SocketsHttpHandler on .NET Core ≥ 2.1, or HttpClientHandler otherwise
    public UnfuckedHttpHandler(HttpMessageHandler? innerHandler = null): base(innerHandler ?? new HttpClientHandler()) { }

    public static HttpClient CreateClient(HttpMessageHandler? innerHandler) => new(new UnfuckedHttpHandler(innerHandler));

    internal static UnfuckedHttpHandler? FindHandler(HttpClient httpClient) {
        UnfuckedHttpHandler? handler = null;
        if (!HttpClientHandlerCache.TryGetValue(httpClient.GetHashCode(), out WeakReference<UnfuckedHttpHandler>? handlerHolder2) || !(handlerHolder2?.TryGetTarget(out handler) ?? true)) {
            handler = FindDescendantHandler((HttpMessageHandler?) typeof(HttpMessageInvoker).GetFields(BindingFlags.NonPublic | BindingFlags.Instance)
                .FirstOrDefault(field => field.FieldType == typeof(HttpMessageHandler))?.GetValue(httpClient));

            HttpClientHandlerCache[httpClient.GetHashCode()] = handler is null ? null : new WeakReference<UnfuckedHttpHandler>(handler);
        }

        return handler;

        static UnfuckedHttpHandler? FindDescendantHandler(HttpMessageHandler? parent) => parent switch {
            null                  => null,
            UnfuckedHttpHandler f => f,
            DelegatingHandler d   => FindDescendantHandler(d.InnerHandler),
            _                     => null
        };
    }

    /// <inheritdoc />
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => TestableSendAsync(request, cancellationToken);

    /// <summary>
    /// For testing with mocks/fakes/stubs/spies/dummies/fixtures
    /// </summary>
    public async Task<HttpResponseMessage> TestableSendAsync(HttpRequestMessage request, CancellationToken cancellationToken) {
        foreach (ClientRequestFilter requestFilter in RequestFilters) {
            await requestFilter.Filter(ref request, cancellationToken).ConfigureAwait(false);
        }

        HttpResponseMessage response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);

        foreach (ClientResponseFilter responseFilter in ResponseFilters) {
            await responseFilter.Filter(ref response, cancellationToken).ConfigureAwait(false);
        }

        return response;
    }

    private UnfuckedHttpHandler With(HttpConfiguration newConfig) {
        ClientConfig = newConfig;
        return this;
    }

    public UnfuckedHttpHandler Register(ClientRequestFilter? filter, int position = HttpConfiguration.LastPosition) =>
        With(ClientConfig.Register(filter, position));

    public UnfuckedHttpHandler Register(ClientResponseFilter? filter, int position = HttpConfiguration.LastPosition) =>
        With(ClientConfig.Register(filter, position));

    IFilteringHttpClientHandler IHttpConfiguration<IFilteringHttpClientHandler>.Register(ClientResponseFilter? filter, int position) => Register(filter, position);
    IFilteringHttpClientHandler IHttpConfiguration<IFilteringHttpClientHandler>.Register(ClientRequestFilter? filter, int position) => Register(filter, position);

    public UnfuckedHttpHandler Property<T>(PropertyKey<T> key, T? value) where T: notnull => With(ClientConfig.Property(key, value));

    public bool Property<T>(PropertyKey<T> key,
#if !NETSTANDARD2_0
                            [NotNullWhen(true)]
#endif
                            out T? existingValue) where T: notnull => ClientConfig.Property(key, out existingValue);

    IFilteringHttpClientHandler IHttpConfiguration<IFilteringHttpClientHandler>.Property<T>(PropertyKey<T> key, T? value) where T: default => Property(key, value);

}