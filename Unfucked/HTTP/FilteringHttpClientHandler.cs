namespace Unfucked.HTTP;

public interface IFilteringHttpClientHandler {

    IList<ClientRequestFilter> RequestFilters { get; }
    IList<ClientResponseFilter> ResponseFilters { get; }

    /// <inheritdoc cref="DelegatingHandler.InnerHandler" />
    public HttpMessageHandler InnerHandler { get; }

    Task<HttpResponseMessage> MockableSendAsync(HttpRequestMessage request, CancellationToken cancellationToken);

}

public class FilteringHttpClientHandler: DelegatingHandler, IFilteringHttpClientHandler {

    public IList<ClientRequestFilter> RequestFilters { get; } = [];
    public IList<ClientResponseFilter> ResponseFilters { get; } = [];

    public FilteringHttpClientHandler(): this(new HttpClientHandler()) { } // automatically uses SocketsHttpHandler on .NET Core ≥ 2.1, or HttpClientHandler otherwise

    public FilteringHttpClientHandler(HttpMessageHandler innerHandler): base(innerHandler) { }

    /// <inheritdoc />
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => MockableSendAsync(request, cancellationToken);

    /// <summary>
    /// For testing with mocks
    /// </summary>
    public async Task<HttpResponseMessage> MockableSendAsync(HttpRequestMessage request, CancellationToken cancellationToken) {
        foreach (ClientRequestFilter requestFilter in RequestFilters) {
            await requestFilter.Filter(ref request, cancellationToken).ConfigureAwait(false);
        }

        HttpResponseMessage response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);

        foreach (ClientResponseFilter responseFilter in ResponseFilters) {
            await responseFilter.Filter(ref response, cancellationToken).ConfigureAwait(false);
        }

        return response;
    }

}