using System.Net;
using System.Net.Http.Headers;
using System.Runtime.Serialization;
using System.Text;
using Unfucked.HTTP.Config;

namespace Unfucked.HTTP.Serialization;

/// <summary>Deserialize an HTTP response body to a given type.</summary>
public interface MessageBodyReader: Registrable {

    /// <summary>Determine if this <see cref="MessageBodyReader"/> instance can handle the given type.</summary>
    /// <typeparam name="T">Type to deserialize the body to.</typeparam>
    /// <param name="contentType">The content type of the response. May be null if the response headers don't specify it.</param>
    /// <param name="bodyPrefix">The first 32 characters of the response body, in case this helps determine if this class can parse it (such as an XML prolog). It is decoded into <paramref name="contentType"/> or <see cref="Encoding.UTF8"/> if unspecified. This will initially be called with a <c>null</c> argument value for this parameter, for performance.</param>
    /// <returns><c>true</c> if this class can parse a byte stream into a <typeparamref name="T"/> instance, or <c>false</c> if it cannot.</returns>
    public bool CanRead<T>(MediaTypeHeaderValue? contentType, string? bodyPrefix);

    /// <summary>
    /// <para>Deserialize an HTTP response body as an instance of the given type.</para>
    /// <para>This method will only ever be called by <c>Unfucked.HTTP</c> iff <see cref="CanRead"/> returns <c>true</c>, so you don't have to check its condition again and you can assume it passed.</para>
    /// </summary>
    /// <typeparam name="T">Type to deserialize the body to.</typeparam>
    /// <param name="responseBody"></param>
    /// <param name="status"></param>
    /// <param name="headers"></param>
    /// <param name="bodyHeaders"></param>
    /// <param name="request"></param>
    /// <param name="response"></param>
    /// <param name="responseEncoding">Character encoding of the body, extracted from the <see cref="HttpHeaders.ContentType"/> response header, or <c>null</c> if the response headers don't specify a character encoding.</param>
    /// <param name="clientConfig">Configuration options specified on the HTTP client.</param>
    /// <param name="cancellationToken">To cancel async operations early.</param>
    /// <returns>The parsed <typeparamref name="T"/> value.</returns>
    /// <exception cref="SerializationException">Deserialization failed.</exception>
    public Task<T> Read<T>(Stream responseBody, HttpStatusCode status, HttpResponseHeaders headers, HttpContentHeaders bodyHeaders, HttpRequestMessage request, IDisposable response,
                           Encoding? responseEncoding, Configurable? clientConfig, CancellationToken cancellationToken);

}