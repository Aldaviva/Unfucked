namespace Unfucked.HTTP;

public interface IFilteringHttpClientHandler: IHttpConfiguration<IFilteringHttpClientHandler> {

    /// <inheritdoc cref="DelegatingHandler.InnerHandler" />
    public HttpMessageHandler? InnerHandler { get; }

    Task<HttpResponseMessage> TestableSendAsync(HttpRequestMessage request, CancellationToken cancellationToken);

}

public class FilteringHttpClientHandler: DelegatingHandler, IFilteringHttpClientHandler, IHttpConfiguration<FilteringHttpClientHandler> {

    internal HttpConfiguration Filters { get; init; } = new();

    public IReadOnlyList<ClientRequestFilter> RequestFilters => Filters.RequestFilters;
    public IReadOnlyList<ClientResponseFilter> ResponseFilters => Filters.ResponseFilters;

    public FilteringHttpClientHandler(): this(new HttpClientHandler()) { } // HttpClientHandler automatically uses SocketsHttpHandler on .NET Core ≥ 2.1, or HttpClientHandler otherwise

    public FilteringHttpClientHandler(HttpMessageHandler innerHandler): base(innerHandler) { }

    public static HttpClient CreateClient() => new(new FilteringHttpClientHandler());

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

    public FilteringHttpClientHandler Register(ClientRequestFilter? filter, int position = HttpConfiguration.LastPosition) => new(InnerHandler!) { Filters  = Filters.Register(filter, position) };
    public FilteringHttpClientHandler Register(ClientResponseFilter? filter, int position = HttpConfiguration.LastPosition) => new(InnerHandler!) { Filters = Filters.Register(filter, position) };

    IFilteringHttpClientHandler IHttpConfiguration<IFilteringHttpClientHandler>.Register(ClientResponseFilter? filter, int position) => Register(filter, position);
    IFilteringHttpClientHandler IHttpConfiguration<IFilteringHttpClientHandler>.Register(ClientRequestFilter? filter, int position) => Register(filter, position);

}