using Unfucked.HTTP.Config;

namespace Unfucked.HTTP;

public partial class UnfuckedWebTarget: WebTarget, Configurable<UnfuckedWebTarget> {

    private readonly UrlBuilder            urlBuilder;
    private readonly HttpClient            client;
    private readonly IUnfuckedHttpHandler? clientHandler;
    private readonly IClientConfig?        clientConfig;

    private UnfuckedWebTarget(HttpClient client, UrlBuilder urlBuilder, IUnfuckedHttpHandler? clientHandler, IClientConfig? clientConfig) {
        this.client        = client;
        this.urlBuilder    = urlBuilder;
        this.clientHandler = clientHandler;
        this.clientConfig  = clientConfig;
    }

    private UnfuckedWebTarget(HttpClient client, UrlBuilder urlBuilder, IUnfuckedHttpHandler? clientHandler):
        this(client, urlBuilder, clientHandler, clientHandler?.ClientConfig) { }

    public UnfuckedWebTarget(HttpClient client, UrlBuilder urlBuilder): this(client, urlBuilder, UnfuckedHttpHandler.FindHandler(client)) { }
    public UnfuckedWebTarget(HttpClient client, Uri uri): this(client, new UrlBuilder(uri)) { }
    public UnfuckedWebTarget(HttpClient client, string uri): this(client, new UrlBuilder(uri)) { }
    public UnfuckedWebTarget(HttpClient client, UriBuilder uriBuilder): this(client, new UrlBuilder(uriBuilder)) { }

}