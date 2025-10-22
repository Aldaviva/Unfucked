using Unfucked.HTTP.Config;

namespace Unfucked.HTTP;

internal class UnfuckedHttpRequestMessage: HttpRequestMessage {

    public UnfuckedHttpRequestMessage(HttpMethod method, string? requestUri): base(method, requestUri) { }
    public UnfuckedHttpRequestMessage(HttpMethod method, Uri? requestUri): base(method, requestUri) { }

    public IClientConfig? Config { get; init; }

}