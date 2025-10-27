using Unfucked.HTTP.Config;

namespace Unfucked.HTTP;

public partial class UnfuckedWebTarget: WebTarget, Configurable<UnfuckedWebTarget> {

    private readonly UrlBuilder            urlBuilder;
    private readonly IUnfuckedHttpClient   client;
    private readonly IUnfuckedHttpHandler? clientHandler;
    private readonly IClientConfig?        clientConfig;

    private UnfuckedWebTarget(IUnfuckedHttpClient client, UrlBuilder urlBuilder, IUnfuckedHttpHandler? clientHandler, IClientConfig? clientConfig) {
        this.client        = client;
        this.urlBuilder    = urlBuilder;
        this.clientHandler = clientHandler;
        this.clientConfig  = clientConfig;
    }

    private UnfuckedWebTarget(IUnfuckedHttpClient client, UrlBuilder urlBuilder, IUnfuckedHttpHandler? clientHandler): this(client, urlBuilder, clientHandler, clientHandler?.ClientConfig) { }

    public UnfuckedWebTarget(IUnfuckedHttpClient client, UrlBuilder urlBuilder): this(client, urlBuilder, client.Handler) { }
    public UnfuckedWebTarget(IUnfuckedHttpClient client, Uri uri): this(client, new UrlBuilder(uri)) { }
    public UnfuckedWebTarget(IUnfuckedHttpClient client, string uri): this(client, new UrlBuilder(uri)) { }
    public UnfuckedWebTarget(IUnfuckedHttpClient client, UriBuilder uriBuilder): this(client, new UrlBuilder(uriBuilder)) { }

}