using Unfucked.HTTP.Config;

namespace Unfucked.HTTP;

/// <inheritdoc cref="IWebTarget" />
public partial class WebTarget: IWebTarget, Configurable<WebTarget> {

    private readonly UrlBuilder            urlBuilder;
    private readonly IHttpClient           client;
    private readonly IUnfuckedHttpHandler? clientHandler;
    private readonly IClientConfig?        clientConfig;

    private WebTarget(IHttpClient client, UrlBuilder urlBuilder, IUnfuckedHttpHandler? clientHandler, IClientConfig? clientConfig) {
        this.client        = client;
        this.urlBuilder    = urlBuilder;
        this.clientHandler = clientHandler;
        this.clientConfig  = clientConfig;
    }

    private WebTarget(IHttpClient client, UrlBuilder urlBuilder, IUnfuckedHttpHandler? clientHandler): this(client, urlBuilder, clientHandler, clientHandler?.ClientConfig) {}

    public WebTarget(IHttpClient client, UrlBuilder urlBuilder): this(client, urlBuilder, client.Handler) {}
    public WebTarget(IHttpClient client, Uri uri): this(client, new UrlBuilder(uri)) {}
    public WebTarget(IHttpClient client, string uri): this(client, new UrlBuilder(uri)) {}
    public WebTarget(IHttpClient client, UriBuilder uriBuilder): this(client, new UrlBuilder(uriBuilder)) {}

}