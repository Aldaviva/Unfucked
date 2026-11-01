using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;
using Unfucked.HTTP.Config;

namespace Unfucked.HTTP.Serialization;

/// <inheritdoc />
public abstract class AbstractMessageBodyReader: MessageBodyReader {

    /// <summary>
    /// <para>Open the HTTP response body as a stream of bytes.</para>
    /// <para>You don't need to <see cref="IDisposable.Dispose"/> the returned <see cref="Stream"/>, because <see cref="WebTarget.ParseAndDisposeResponseBody"/> will do so automatically.</para>
    /// </summary>
    /// <param name="response">HTTP response.</param>
    /// <param name="cancellationToken">Cancel.</param>
    /// <returns>HTTP response body byte stream.</returns>
    protected static ConfiguredTaskAwaitable<Stream> GetBodyStream(HttpResponseMessage response, CancellationToken cancellationToken) {
        Task<Stream> bodyStreamTask =
#if NET5_0_OR_GREATER
            response.Content.ReadAsStreamAsync(cancellationToken);
#else
            response.Content.ReadAsStreamAsync();
#endif
        return bodyStreamTask.ConfigureAwait(false);
    }

    /// <inheritdoc />
    public abstract bool CanRead<T>(MediaTypeHeaderValue? contentType);

    /// <inheritdoc />
    public abstract Task<T> Read<T>(HttpResponseMessage response, Encoding? responseEncoding, Configurable? clientConfig, CancellationToken cancellationToken);

}