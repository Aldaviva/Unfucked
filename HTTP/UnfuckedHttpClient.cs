using System.Net.Http.Headers;
using System.Reflection;

namespace Unfucked.HTTP;

public interface IUnfuckedHttpClient {

    IUnfuckedHttpHandler Handler { get; }

    Task<HttpResponseMessage> SendAsync(HttpRequest request, CancellationToken cancellationToken = default);

}

public class UnfuckedHttpClient: HttpClient, IUnfuckedHttpClient {

    public IUnfuckedHttpHandler Handler { get; }

    public UnfuckedHttpClient(HttpMessageHandler? handler = null, bool disposeHandler = true): this((IUnfuckedHttpHandler) (handler as UnfuckedHttpHandler ?? new UnfuckedHttpHandler(handler)),
        disposeHandler) { }

    public UnfuckedHttpClient(IUnfuckedHttpHandler unfuckedHandler, bool disposeHandler = true): base(unfuckedHandler as HttpMessageHandler ?? new IUnfuckedHttpHandlerWrapper(unfuckedHandler),
        disposeHandler) {
        Handler = unfuckedHandler;
        Timeout = TimeSpan.FromSeconds(30);
        if (Assembly.GetEntryAssembly()?.GetName() is { Name: { } programName, Version: { } programVersion }) {
            DefaultRequestHeaders.UserAgent.AddAll(new ProductInfoHeaderValue(programName, programVersion.ToString(4, true)));
        }
        UnfuckedHttpHandler.CacheClientHandler(this, unfuckedHandler);
    }

    public virtual async Task<HttpResponseMessage> SendAsync(HttpRequest request, CancellationToken cancellationToken = default) {
        using HttpRequestMessage req = new(request.Verb, request.Uri) {
            Content = request.Body
        };

        foreach (KeyValuePair<string, string> header in request.Headers) {
            req.Headers.Add(header.Key, header.Value);
        }

        return await SendAsync(req, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
    }

}