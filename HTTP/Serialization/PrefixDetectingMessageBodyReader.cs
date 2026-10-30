using System.Net;
using System.Net.Http.Headers;
using System.Runtime.Serialization;
using System.Text;
using Unfucked.HTTP.Config;

namespace Unfucked.HTTP.Serialization;

/// <summary>
/// <para>Deserialize HTTP response bodies, making use of the first 32 or so characters of the body to determine if this class is eligible to handle the given body.</para>
/// <para>This is useful if a server returns JSON or XML with a useless <c>Content-Type</c> header value like <c>application/octet-stream</c>, or no such header, because this can scan the beginning of the body and see if it contains <c>&lt;?xml</c> or another relevant substring.</para>
/// </summary>
public abstract class PrefixDetectingMessageBodyReader: MessageBodyReader {

    /// <summary>Determine if this <see cref="MessageBodyReader"/> instance can handle the given type.</summary>
    /// <typeparam name="T">Type to deserialize the body to.</typeparam>
    /// <param name="contentType">The content type of the response. May be null if the response headers don't specify it.</param>
    /// <param name="bodyPrefix">The first roughly 32 characters of the response body, in case this helps determine if this class can parse it (such as an XML prolog). It is decoded into <paramref name="contentType"/> or <see cref="Encoding.UTF8"/> if unspecified. This will initially be called with a <c>null</c> argument value for this parameter, for performance.</param>
    /// <returns><c>true</c> if this class can parse a byte stream into a <typeparamref name="T"/> instance, or <c>false</c> if it cannot.</returns>
    public abstract bool CanRead<T>(MediaTypeHeaderValue? contentType, string? bodyPrefix);

    /// <summary>
    /// <para>Deserialize an HTTP response body as an instance of the given type.</para>
    /// <para>This method will only ever be called by <c>Unfucked.HTTP</c> iff <see cref="CanRead"/> returns <c>true</c>, so you don't have to check its condition again and you can assume it passed.</para>
    /// </summary>
    /// <typeparam name="T">Type to deserialize the body to.</typeparam>
    /// <param name="responseBody">HTTP response body stream, at position 0.</param>
    /// <param name="status">HTTP response status code.</param>
    /// <param name="headers">HTTP response headers (excluding body headers like <c>Content-Type</c>).</param>
    /// <param name="bodyHeaders">HTTP response body headers (like <c>Content-Type</c>).</param>
    /// <param name="request">The HTTP request that resulted in this response.</param>
    /// <param name="response">If you need to dispose the response. Not a <see cref="HttpResponseMessage"/> because that points to a <see cref="Stream"/> that is not at position 0.</param>
    /// <param name="responseEncoding">Character encoding of the body, extracted from the <see cref="HttpHeaders.ContentType"/> response header, or <c>null</c> if the response headers don't specify a character encoding.</param>
    /// <param name="clientConfig">Configuration options specified on the HTTP client.</param>
    /// <param name="cancellationToken">To cancel async operations early.</param>
    /// <returns>The parsed <typeparamref name="T"/> value.</returns>
    /// <exception cref="SerializationException">Deserialization failed.</exception>
    public abstract Task<T> Read<T>(Stream responseBody, HttpStatusCode status, HttpResponseHeaders headers, HttpContentHeaders bodyHeaders, HttpRequestMessage request, IDisposable response,
                                    Encoding? responseEncoding, Configurable? clientConfig, CancellationToken cancellationToken);

    bool MessageBodyReader.CanRead<T>(MediaTypeHeaderValue? contentType) {
        return CanRead<T>(contentType, null);
    }

    async Task<T> MessageBodyReader.Read<T>(HttpResponseMessage response, Encoding? responseEncoding, Configurable? clientConfig, CancellationToken cancellationToken) {
        Task<Stream> bodyStream =
#if NET5_0_OR_GREATER
            response.Content.ReadAsStreamAsync(cancellationToken);
#else
            response.Content.ReadAsStreamAsync();
#endif

        return await Read<T>(await bodyStream.ConfigureAwait(false), response.StatusCode, response.Headers, response.Content.Headers, response.RequestMessage!, response, responseEncoding,
            clientConfig,
            cancellationToken).ConfigureAwait(false);
    }

}