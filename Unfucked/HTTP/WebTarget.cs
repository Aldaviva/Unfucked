namespace Unfucked.HTTP;

public partial class WebTarget: IWebTarget, IHttpConfiguration<WebTarget> {

    private readonly UrlBuilder           urlBuilder;
    private readonly HttpClient           client;
    private readonly UnfuckedHttpHandler? clientHandler;
    private readonly HttpConfiguration?   clientConfig;

    private WebTarget(HttpClient client, UrlBuilder urlBuilder, UnfuckedHttpHandler? clientHandler, HttpConfiguration? clientConfig) {
        this.client        = client;
        this.urlBuilder    = urlBuilder;
        this.clientHandler = clientHandler;
        this.clientConfig  = clientConfig;
    }

    private WebTarget(HttpClient client, UrlBuilder urlBuilder, UnfuckedHttpHandler? clientHandler):
        this(client, urlBuilder, clientHandler, clientHandler?.ClientConfig) { }

    public WebTarget(HttpClient client, UrlBuilder urlBuilder): this(client, urlBuilder, UnfuckedHttpHandler.FindHandler(client)) { }
    public WebTarget(HttpClient client, Uri uri): this(client, new UrlBuilder(uri)) { }
    public WebTarget(HttpClient client, string uri): this(client, new UrlBuilder(uri)) { }
    public WebTarget(HttpClient client, UriBuilder uriBuilder): this(client, new UrlBuilder(uriBuilder)) { }

}