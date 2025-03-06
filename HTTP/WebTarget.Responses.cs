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

    private static readonly IReadOnlyList<MessageBodyReader> DefaultMessageBodyReaders = [
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

    private async Task<T> ParseResponseBody<T>(HttpResponseMessage response, CancellationToken cancellationToken) {
        if (!response.IsSuccessStatusCode && (!Property(PropertyKey.ThrowOnUnsuccessfulStatusCode, out bool value) || value)) {
            string         reason     = response.ReasonPhrase!;
            HttpStatusCode statusCode = response.StatusCode;
            WebApplicationException exception = statusCode switch {
                HttpStatusCode.BadRequest           => new BadRequestException(reason),
                HttpStatusCode.Unauthorized         => new NotAuthorizedException(reason),
                HttpStatusCode.Forbidden            => new ForbiddenException(reason),
                HttpStatusCode.NotFound             => new NotFoundException(reason),
                HttpStatusCode.MethodNotAllowed     => new NotAllowedException(reason),
                HttpStatusCode.NotAcceptable        => new NotAcceptableException(reason),
                HttpStatusCode.UnsupportedMediaType => new NotSupportedException(reason),
                HttpStatusCode.InternalServerError  => new InternalServerErrorException(reason),
                HttpStatusCode.ServiceUnavailable   => new ServiceUnavailableException(reason),

                >= HttpStatusCode.MultipleChoices and < HttpStatusCode.BadRequest     => new RedirectionException(statusCode, response.Headers.Location, reason),
                >= HttpStatusCode.BadRequest and < HttpStatusCode.InternalServerError => new ClientErrorException(statusCode, reason),
                >= HttpStatusCode.InternalServerError and < (HttpStatusCode) 600      => new ServerErrorException(statusCode, reason),

                _ => new WebApplicationException(statusCode, reason)
            };
            exception.Data[WebApplicationException.RequestUrlDataKey] = response.RequestMessage!.RequestUri;
            throw exception;
        }

        MediaTypeHeaderValue? responseContentType = response.Content.Headers.ContentType;
        Encoding?             responseEncoding    = null;
        try {
            responseEncoding = responseContentType?.CharSet is { } responseEncodingName ? Encoding.GetEncoding(responseEncodingName) : null;
        } catch (ArgumentException) { }

        IEnumerable<MessageBodyReader> messageBodyReaders = (clientConfig?.MessageBodyReaders ?? []).Concat(DefaultMessageBodyReaders).ToList();

        foreach (MessageBodyReader reader in messageBodyReaders) {
            // not sure if the read stream needs to be rewound between attempts
            if (reader.CanRead<T>(responseContentType?.MediaType, null) && !cancellationToken.IsCancellationRequested) {
                try {
                    return await reader.Read<T>(response.Content, responseEncoding, clientConfig, cancellationToken).ConfigureAwait(false);
                } catch (Exception e) when (e is not OutOfMemoryException) {
                    throw new ProcessingException(e) { Data = { { WebApplicationException.RequestUrlDataKey, response.RequestMessage!.RequestUri } } };
                }
            }
        }

        string bodyPrefix;
        Stream bodyStream;
        // this disposes the response stream, which may prevent further reads
#if NET6_0_OR_GREATER
        await using (bodyStream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false)) {
#else
        using (bodyStream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false)) {
#endif
            using StreamReader bodyReader   = new(bodyStream, responseEncoding ?? Encoding.UTF8, true);
            char[]             prefixBuffer = new char[32];
            int prefixSize =
#if NET6_0_OR_GREATER
                await bodyReader.ReadAsync(prefixBuffer.AsMemory(), cancellationToken).ConfigureAwait(false);
#else
                await bodyReader.ReadAsync(prefixBuffer, 0, prefixBuffer.Length).ConfigureAwait(false);
#endif
            bodyPrefix          = new string(prefixBuffer, 0, prefixSize).Trim();
            bodyStream.Position = 0;
        }

        foreach (MessageBodyReader reader in messageBodyReaders) {
            if (reader.CanRead<T>(responseContentType?.MediaType, bodyPrefix) && !cancellationToken.IsCancellationRequested) {
                try {
                    return await reader.Read<T>(response.Content, responseEncoding, clientConfig, cancellationToken).ConfigureAwait(false);
                } catch (Exception e) when (e is not OutOfMemoryException) {
                    throw new ProcessingException(e) { Data = { { WebApplicationException.RequestUrlDataKey, response.RequestMessage!.RequestUri } } };
                }
            }
        }

        throw new ProcessingException(new SerializationException(
            $"Could not determine content type of response body to deserialize (URI: {response.RequestMessage?.RequestUri}, Content-Type: {responseContentType}, .NET type: {typeof(T)})"));
    }

}