using System.Net;
using System.Net.Http.Headers;
using System.Runtime.Serialization;
using System.Text;
using Unfucked.HTTP.Config;
using Unfucked.HTTP.Exceptions;
using Unfucked.HTTP.Serialization;
using NotSupportedException = Unfucked.HTTP.Exceptions.NotSupportedException;

namespace Unfucked.HTTP;

public partial class WebTarget {

    private static readonly IReadOnlyList<MessageBodyReader> DEFAULT_MESSAGE_BODY_READERS = [
        new StreamBodyReader(),
        new ByteArrayBodyReader(),
        new ByteArrayBodyReader.MemoryBodyReader(),
        new ByteArrayBodyReader.ReadOnlyMemoryBodyReader(),
        new StringBodyReader(),
        new JsonBodyReader(),
        new XmlBodyReader.XDocumentReader(),
        new XmlBodyReader.XmlDocumentReader(),
        new XmlBodyReader.XPathReader(),
        new XmlBodyReader()
    ];

    /// <exception cref="ProcessingException">response parsing failed</exception>
    /// <exception cref="WebApplicationException">the response status code was not succesful, and <see cref="PropertyKey.ThrowOnUnsuccessfulStatusCode"/> was left enabled</exception>
    private async Task<T> ParseResponseBody<T>(HttpResponseMessage response, CancellationToken cancellationToken) {
        if (!Property(PropertyKey.ThrowOnUnsuccessfulStatusCode, out bool value) || value) {
            await ThrowIfUnsuccessful(response, cancellationToken).ConfigureAwait(false);
        }

        MediaTypeHeaderValue? responseContentType = response.Content.Headers.ContentType;
        Encoding?             responseEncoding    = null;
        try {
            responseEncoding = responseContentType?.CharSet is { } responseEncodingName ? Encoding.GetEncoding(responseEncodingName) : null;
        } catch (ArgumentException) { }

        IEnumerable<MessageBodyReader> messageBodyReaders = (clientConfig?.MessageBodyReaders ?? []).Concat(DEFAULT_MESSAGE_BODY_READERS).ToList();

        foreach (MessageBodyReader reader in messageBodyReaders) {
            // not sure if the read stream needs to be rewound between attempts
            if (reader.CanRead<T>(responseContentType?.MediaType, null) && !cancellationToken.IsCancellationRequested) {
                try {
                    return await reader.Read<T>(response.Content, responseEncoding, clientConfig, cancellationToken).ConfigureAwait(false);
                } catch (Exception e) when (e is not OutOfMemoryException) {
                    throw new ProcessingException(e, await HttpExceptionParams.FromResponse(response, cancellationToken).ConfigureAwait(false));
                }
            }
        }

        await response.Content.LoadIntoBufferAsync().ConfigureAwait(false);

        /*
         * Don't dispose the bodyStream yet, because it's a buffered shared instance in the HttpContent at this point and must be read multiple times by message body readers below who each call
         * ReadAsStreamAsync(). It will get disposed in Get<T>() and similar by DisposeIfNotStream().
         */
#if NET5_0_OR_GREATER
        Stream bodyStream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
#else
        Stream bodyStream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
#endif
        using StreamReader bodyReader   = new(bodyStream, responseEncoding ?? Encoding.UTF8, true);
        char[]             prefixBuffer = new char[32];

        int prefixSize;
#if NETCOREAPP2_1_OR_GREATER
        try {
            prefixSize = await bodyReader.ReadAsync(prefixBuffer.AsMemory(), cancellationToken).ConfigureAwait(false);
        } catch (OperationCanceledException e) {
            throw new ProcessingException(e, await HttpExceptionParams.FromResponse(response, CancellationToken.None).ConfigureAwait(false));
        }
#else
        prefixSize = await bodyReader.ReadAsync(prefixBuffer, 0, prefixBuffer.Length).ConfigureAwait(false);
#endif
        string bodyPrefix = new string(prefixBuffer, 0, prefixSize).Trim();
        bodyStream.Position = 0;

        foreach (MessageBodyReader reader in messageBodyReaders) {
            if (reader.CanRead<T>(responseContentType?.MediaType, bodyPrefix) && !cancellationToken.IsCancellationRequested) {
                try {
                    return await reader.Read<T>(response.Content, responseEncoding, clientConfig, cancellationToken).ConfigureAwait(false);
                } catch (Exception e) when (e is not OutOfMemoryException) {
                    throw new ProcessingException(e, await HttpExceptionParams.FromResponse(response, cancellationToken).ConfigureAwait(false));
                }
            }
        }

        HttpExceptionParams p = await HttpExceptionParams.FromResponse(response, cancellationToken).ConfigureAwait(false);
        throw new ProcessingException(
            new SerializationException($"Could not determine content type of response body to deserialize (URI: {p.RequestUrl}, Content-Type: {responseContentType}, .NET type: {typeof(T)})"), p);
    }

    /// <exception cref="WebApplicationException">the response status code was not successful</exception>
    internal static async Task ThrowIfUnsuccessful(HttpResponseMessage response, CancellationToken cancellationToken) {
        if (!response.IsSuccessStatusCode) {
            HttpStatusCode      statusCode = response.StatusCode;
            string              reason     = response.ReasonPhrase ?? statusCode.ToString();
            HttpExceptionParams p          = await HttpExceptionParams.FromResponse(response, cancellationToken).ConfigureAwait(false);
            response.Dispose();
            throw statusCode switch {
                HttpStatusCode.BadRequest           => new BadRequestException(reason, p),
                HttpStatusCode.Unauthorized         => new NotAuthorizedException(reason, p),
                HttpStatusCode.Forbidden            => new ForbiddenException(reason, p),
                HttpStatusCode.NotFound             => new NotFoundException(reason, p),
                HttpStatusCode.MethodNotAllowed     => new NotAllowedException(reason, p),
                HttpStatusCode.NotAcceptable        => new NotAcceptableException(reason, p),
                HttpStatusCode.UnsupportedMediaType => new NotSupportedException(reason, p),
                HttpStatusCode.InternalServerError  => new InternalServerErrorException(reason, p),
                HttpStatusCode.ServiceUnavailable   => new ServiceUnavailableException(reason, p),

                >= HttpStatusCode.MultipleChoices and < HttpStatusCode.BadRequest     => new RedirectionException(statusCode, response.Headers.Location, reason, p),
                >= HttpStatusCode.BadRequest and < HttpStatusCode.InternalServerError => new ClientErrorException(statusCode, reason, p),
                >= HttpStatusCode.InternalServerError and < (HttpStatusCode) 600      => new ServerErrorException(statusCode, reason, p),

                _ => new WebApplicationException(statusCode, reason, p)
            };
        }
    }

}