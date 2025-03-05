namespace Unfucked.HTTP;

public class UnfuckedHttpClient: HttpClient {

    public UnfuckedHttpHandler Handler { get; }

    public UnfuckedHttpClient(HttpMessageHandler? handler = null, bool disposeHandler = true): this(new UnfuckedHttpHandler(handler), disposeHandler) { }

    private UnfuckedHttpClient(UnfuckedHttpHandler unfuckedHandler, bool disposeHandler): base(unfuckedHandler, disposeHandler) {
        Handler = unfuckedHandler;
        UnfuckedHttpHandler.CacheClientHandler(this, unfuckedHandler);
    }

}