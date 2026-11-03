namespace Unfucked.HTTP;

public partial class WebTarget {

    private static readonly HttpMethod PATCH_VERB = new("PATCH");

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

    private async Task<T> Send<T>(HttpMethod verb, HttpContent? requestBody, TaskCompletionSource<HttpResponseMessage> responseTask, CancellationToken ct) {
        try {
            HttpResponseMessage response = await Send(verb, requestBody, ct).ConfigureAwait(false);
            responseTask.TrySetResult(response);
            return await ParseAndDisposeResponseBody<T>(response, ct).ConfigureAwait(false);
        } catch (Exception e) when (e is not OutOfMemoryException) {
            if (ct.IsCancellationRequested) {
                responseTask.TrySetCanceled(ct);
            } else {
                responseTask.TrySetException(e);
            }
            throw;
        }
    }

    /// <inheritdoc />
    public Task<T> Send<T>(HttpMethod verb, HttpContent? requestBody, out Task<HttpResponseMessage> response, CancellationToken cancellationToken = default) {
        TaskCompletionSource<HttpResponseMessage> tcs = new();
        response = tcs.Task;
        return Send<T>(verb, requestBody, tcs, cancellationToken);
    }

    /// <inheritdoc />
    public Task<T> Get<T>(out Task<HttpResponseMessage> response, CancellationToken cancellationToken = default) => Send<T>(HttpMethod.Get, null, out response, cancellationToken);

    /// <inheritdoc />
    public Task<T> Post<T>(HttpContent? requestBody, out Task<HttpResponseMessage> response, CancellationToken cancellationToken = default) =>
        Send<T>(HttpMethod.Post, requestBody, out response, cancellationToken);

    /// <inheritdoc />
    public Task<T> Put<T>(HttpContent? requestBody, out Task<HttpResponseMessage> response, CancellationToken cancellationToken = default) =>
        Send<T>(HttpMethod.Put, requestBody, out response, cancellationToken);

    /// <inheritdoc />
    public Task<T> Patch<T>(HttpContent? requestBody, out Task<HttpResponseMessage> response, CancellationToken cancellationToken = default) =>
        Send<T>(PATCH_VERB, requestBody, out response, cancellationToken);

    /// <inheritdoc />
    public Task<T> Delete<T>(HttpContent? requestBody, out Task<HttpResponseMessage> response, CancellationToken cancellationToken = default) =>
        Send<T>(HttpMethod.Delete, requestBody, out response, cancellationToken);

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