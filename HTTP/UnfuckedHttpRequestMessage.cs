using Unfucked.HTTP.Config;

namespace Unfucked.HTTP;

internal class UnfuckedHttpRequestMessage: HttpRequestMessage {

    public UnfuckedHttpRequestMessage(HttpMethod method, string? requestUri): base(method, requestUri) { }
    public UnfuckedHttpRequestMessage(HttpMethod method, Uri? requestUri): base(method, requestUri) { }

    public UnfuckedHttpRequestMessage(HttpRequestMessage request): base(request.Method, request.RequestUri) {
        Content = request.Content;
        Version = request.Version;
    }

    public UnfuckedHttpRequestMessage(HttpRequest request): base(request.Verb, request.Uri) {
        Config  = request.ClientConfig;
        Content = request.Body;
    }

    public IClientConfig? Config { get; init; }

}