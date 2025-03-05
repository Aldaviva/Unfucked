namespace Unfucked.HTTP;

public partial class WebTarget {

    private static readonly HttpMethod PatchVerb = new("PATCH");

    public async Task<HttpResponseMessage> Send(HttpMethod verb, HttpContent? requestBody = null, CancellationToken cancellationToken = default) {
        using HttpRequestMessage request = new(verb, urlBuilder.ToUrl()) { Content = requestBody };

        foreach (IGrouping<string, string> header in Headers.GroupBy(pair => pair.Key, pair => pair.Value, StringComparer.OrdinalIgnoreCase)) {
            request.Headers.Add(header.Key, header);
        }

        return await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
    }

    public Task<HttpResponseMessage> Get(CancellationToken cancellationToken = default) => Send(HttpMethod.Get, null, cancellationToken);

    public Task<HttpResponseMessage> Head(CancellationToken cancellationToken = default) => Send(HttpMethod.Head, null, cancellationToken);

    public Task<HttpResponseMessage> Post(HttpContent? requestBody, CancellationToken cancellationToken = default) => Send(HttpMethod.Post, requestBody, cancellationToken);

    public Task<HttpResponseMessage> Put(HttpContent? requestBody, CancellationToken cancellationToken = default) => Send(HttpMethod.Put, requestBody, cancellationToken);

    public Task<HttpResponseMessage> Patch(HttpContent? requestBody, CancellationToken cancellationToken = default) => Send(PatchVerb, requestBody, cancellationToken);

    public Task<HttpResponseMessage> Delete(HttpContent? requestBody = null, CancellationToken cancellationToken = default) => Send(HttpMethod.Delete, requestBody, cancellationToken);

    public async Task<T> Send<T>(HttpMethod verb, HttpContent? requestBody, CancellationToken cancellationToken = default) {
        using HttpResponseMessage response = await Send(verb, requestBody, cancellationToken).ConfigureAwait(false);
        return await ParseResponseBody<T>(response, cancellationToken).ConfigureAwait(false);
    }

    public async Task<T> Get<T>(CancellationToken cancellationToken = default) {
        using HttpResponseMessage response = await Get(cancellationToken).ConfigureAwait(false);
        return await ParseResponseBody<T>(response, cancellationToken).ConfigureAwait(false);
    }

    public async Task<T> Post<T>(HttpContent? requestBody, CancellationToken cancellationToken = default) {
        using HttpResponseMessage response = await Post(requestBody, cancellationToken).ConfigureAwait(false);
        return await ParseResponseBody<T>(response, cancellationToken).ConfigureAwait(false);
    }

    public async Task<T> Put<T>(HttpContent? requestBody, CancellationToken cancellationToken = default) {
        using HttpResponseMessage response = await Put(requestBody, cancellationToken).ConfigureAwait(false);
        return await ParseResponseBody<T>(response, cancellationToken).ConfigureAwait(false);
    }

    public async Task<T> Patch<T>(HttpContent? requestBody, CancellationToken cancellationToken = default) {
        using HttpResponseMessage response = await Patch(requestBody, cancellationToken).ConfigureAwait(false);
        return await ParseResponseBody<T>(response, cancellationToken).ConfigureAwait(false);
    }

    public async Task<T> Delete<T>(HttpContent? requestBody = null, CancellationToken cancellationToken = default) {
        using HttpResponseMessage response = await Delete(requestBody, cancellationToken).ConfigureAwait(false);
        return await ParseResponseBody<T>(response, cancellationToken).ConfigureAwait(false);
    }

}