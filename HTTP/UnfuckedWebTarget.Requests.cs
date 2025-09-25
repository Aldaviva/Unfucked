namespace Unfucked.HTTP;

public partial class UnfuckedWebTarget {

    private static readonly HttpMethod PatchVerb = new("PATCH");

    /*
     * Must not be async so the AsyncLocal scope for wire logging is high enough in the async chain.
     */
    public virtual Task<HttpResponseMessage> Send(HttpMethod verb, HttpContent? requestBody = null, CancellationToken cancellationToken = default) {
        Uri url = urlBuilder.ToUrl();
        if (client is IUnfuckedHttpClient u) {
            return u.SendAsync(new HttpRequest(verb, url, Headers, requestBody), cancellationToken);
        }

        using HttpRequestMessage request = new(verb, url) { Content = requestBody };

        foreach (IGrouping<string, string> header in Headers.GroupBy(pair => pair.Key, pair => pair.Value, StringComparer.OrdinalIgnoreCase)) {
            request.Headers.Add(header.Key, header);
        }

        return client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
    }

    public Task<HttpResponseMessage> Get(CancellationToken cancellationToken = default) => Send(HttpMethod.Get, null, cancellationToken);

    public Task<HttpResponseMessage> Head(CancellationToken cancellationToken = default) => Send(HttpMethod.Head, null, cancellationToken);

    public Task<HttpResponseMessage> Post(HttpContent? requestBody, CancellationToken cancellationToken = default) => Send(HttpMethod.Post, requestBody, cancellationToken);

    public Task<HttpResponseMessage> Put(HttpContent? requestBody, CancellationToken cancellationToken = default) => Send(HttpMethod.Put, requestBody, cancellationToken);

    public Task<HttpResponseMessage> Patch(HttpContent? requestBody, CancellationToken cancellationToken = default) => Send(PatchVerb, requestBody, cancellationToken);

    public Task<HttpResponseMessage> Delete(HttpContent? requestBody = null, CancellationToken cancellationToken = default) => Send(HttpMethod.Delete, requestBody, cancellationToken);

    public async Task<T> Send<T>(HttpMethod verb, HttpContent? requestBody, CancellationToken cancellationToken = default) {
        HttpResponseMessage response = await Send(verb, requestBody, cancellationToken).ConfigureAwait(false);
        try {
            return await ParseResponseBody<T>(response, cancellationToken).ConfigureAwait(false);
        } finally {
            DisposeIfNotStream<T>(response);
        }
    }

    public async Task<T> Get<T>(CancellationToken cancellationToken = default) {
        HttpResponseMessage response = await Get(cancellationToken).ConfigureAwait(false);
        try {
            return await ParseResponseBody<T>(response, cancellationToken).ConfigureAwait(false);
        } finally {
            DisposeIfNotStream<T>(response);
        }
    }

    public async Task<T> Post<T>(HttpContent? requestBody, CancellationToken cancellationToken = default) {
        HttpResponseMessage response = await Post(requestBody, cancellationToken).ConfigureAwait(false);
        try {
            return await ParseResponseBody<T>(response, cancellationToken).ConfigureAwait(false);
        } finally {
            DisposeIfNotStream<T>(response);
        }
    }

    public async Task<T> Put<T>(HttpContent? requestBody, CancellationToken cancellationToken = default) {
        HttpResponseMessage response = await Put(requestBody, cancellationToken).ConfigureAwait(false);
        try {
            return await ParseResponseBody<T>(response, cancellationToken).ConfigureAwait(false);
        } finally {
            DisposeIfNotStream<T>(response);
        }
    }

    public async Task<T> Patch<T>(HttpContent? requestBody, CancellationToken cancellationToken = default) {
        HttpResponseMessage response = await Patch(requestBody, cancellationToken).ConfigureAwait(false);
        try {
            return await ParseResponseBody<T>(response, cancellationToken).ConfigureAwait(false);
        } finally {
            DisposeIfNotStream<T>(response);
        }
    }

    public async Task<T> Delete<T>(HttpContent? requestBody = null, CancellationToken cancellationToken = default) {
        HttpResponseMessage response = await Delete(requestBody, cancellationToken).ConfigureAwait(false);
        try {
            return await ParseResponseBody<T>(response, cancellationToken).ConfigureAwait(false);
        } finally {
            DisposeIfNotStream<T>(response);
        }
    }

    private static void DisposeIfNotStream<T>(HttpResponseMessage response) {
        if (typeof(T) != typeof(Stream)) {
            response.Dispose();
        }
    }

}