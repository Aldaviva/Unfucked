namespace Unfucked.HTTP;

public partial class WebTarget {

    private static readonly HttpMethod PATCH_VERB   = new("PATCH");

    /*
     * Must not be async so the AsyncLocal scope for wire logging is high enough in the async chain.
     */
    /// <inheritdoc />
    public virtual Task<HttpResponseMessage> Send(HttpMethod verb, HttpContent? requestBody = null, CancellationToken cancellationToken = default) =>
        client.SendAsync(new HttpRequest(verb, urlBuilder.ToUrl(), Headers, requestBody, clientConfig), cancellationToken);

    /// <inheritdoc />
    public Task<HttpResponseMessage> Get(CancellationToken cancellationToken = default) => Send(HttpMethod.Get, null, cancellationToken);

    /// <inheritdoc />
    public Task<HttpResponseMessage> Head(CancellationToken cancellationToken = default) => Send(HttpMethod.Head, null, cancellationToken);

    /// <inheritdoc />
    public Task<HttpResponseMessage> Post(HttpContent? requestBody, CancellationToken cancellationToken = default) => Send(HttpMethod.Post, requestBody, cancellationToken);

    /// <inheritdoc />
    public Task<HttpResponseMessage> Put(HttpContent? requestBody, CancellationToken cancellationToken = default) => Send(HttpMethod.Put, requestBody, cancellationToken);

    /// <inheritdoc />
    public Task<HttpResponseMessage> Patch(HttpContent? requestBody, CancellationToken cancellationToken = default) => Send(PATCH_VERB, requestBody, cancellationToken);

    /// <inheritdoc />
    public Task<HttpResponseMessage> Delete(HttpContent? requestBody = null, CancellationToken cancellationToken = default) => Send(HttpMethod.Delete, requestBody, cancellationToken);

    /// <inheritdoc />
    public async Task<T> Send<T>(HttpMethod verb, HttpContent? requestBody, CancellationToken cancellationToken = default) =>
        await ParseAndDisposeResponseBody<T>(await Send(verb, requestBody, cancellationToken).ConfigureAwait(false), cancellationToken).ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<T> Get<T>(CancellationToken cancellationToken = default) =>
        await ParseAndDisposeResponseBody<T>(await Get(cancellationToken).ConfigureAwait(false), cancellationToken).ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<T> Post<T>(HttpContent? requestBody, CancellationToken cancellationToken = default) =>
        await ParseAndDisposeResponseBody<T>(await Post(requestBody, cancellationToken).ConfigureAwait(false), cancellationToken).ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<T> Put<T>(HttpContent? requestBody, CancellationToken cancellationToken = default) =>
        await ParseAndDisposeResponseBody<T>(await Put(requestBody, cancellationToken).ConfigureAwait(false), cancellationToken).ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<T> Patch<T>(HttpContent? requestBody, CancellationToken cancellationToken = default) =>
        await ParseAndDisposeResponseBody<T>(await Patch(requestBody, cancellationToken).ConfigureAwait(false), cancellationToken).ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<T> Delete<T>(HttpContent? requestBody = null, CancellationToken cancellationToken = default) =>
        await ParseAndDisposeResponseBody<T>(await Delete(requestBody, cancellationToken).ConfigureAwait(false), cancellationToken).ConfigureAwait(false);

    private async Task<T> ParseAndDisposeResponseBody<T>(HttpResponseMessage response, CancellationToken cancellationToken) {
        try {
            return await ParseResponseBody<T>(response, cancellationToken).ConfigureAwait(false);
        } finally {
            if (typeof(T) != typeof(Stream) && typeof(T) != typeof(HttpResponseMessage)) {
                response.Dispose();
            }
        }
    }

}