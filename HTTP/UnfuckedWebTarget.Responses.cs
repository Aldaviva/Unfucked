using System.Net;
using System.Net.Http.Headers;
using System.Runtime.Serialization;
using System.Text;
using Unfucked.HTTP.Config;
using Unfucked.HTTP.Exceptions;
using Unfucked.HTTP.Serialization;
using NotSupportedException = Unfucked.HTTP.Exceptions.NotSupportedException;

namespace Unfucked.HTTP;

public partial class UnfuckedWebTarget {

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
        if (!Property(PropertyKey.ThrowOnUnsuccessfulStatusCode, out bool value) || value) {
            await ThrowIfUnsuccessful(response, cancellationToken).ConfigureAwait(false);
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
                    throw new ProcessingException(e, await CreateHttpExceptionParams(response, cancellationToken).ConfigureAwait(false));
                }
            }
        }

        string bodyPrefix;
        Stream bodyStream;
        await response.Content.LoadIntoBufferAsync().ConfigureAwait(false);
        // this using block disposes the response stream, which may prevent further reads
#if NET5_0_OR_GREATER
        await using (bodyStream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false)) {
#else
        using (bodyStream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false)) {
#endif
            using StreamReader bodyReader   = new(bodyStream, responseEncoding ?? Encoding.UTF8, true);
            char[]             prefixBuffer = new char[32];
            int prefixSize =
#if NETCOREAPP2_1_OR_GREATER
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
                    throw new ProcessingException(e, await CreateHttpExceptionParams(response, cancellationToken).ConfigureAwait(false));
                }
            }
        }

        HttpExceptionParams p2 = await CreateHttpExceptionParams(response, cancellationToken).ConfigureAwait(false);
        throw new ProcessingException(
            new SerializationException($"Could not determine content type of response body to deserialize (URI: {p2.RequestUrl}, Content-Type: {responseContentType}, .NET type: {typeof(T)})"), p2);
    }

    internal static async Task ThrowIfUnsuccessful(HttpResponseMessage response, CancellationToken cancellationToken) {
        if (!response.IsSuccessStatusCode) {
            HttpStatusCode      statusCode = response.StatusCode;
            string              reason     = response.ReasonPhrase ?? statusCode.ToString();
            HttpExceptionParams p          = await CreateHttpExceptionParams(response, cancellationToken).ConfigureAwait(false);
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

    private static async Task<HttpExceptionParams> CreateHttpExceptionParams(HttpResponseMessage response, CancellationToken cancellationToken) {
        HttpRequestMessage? request = response.RequestMessage;

        ReadOnlyMemory<byte>? responseBody = null;
        try {
            await response.Content.LoadIntoBufferAsync().ConfigureAwait(false);
            Task<byte[]> readAsByteArrayAsync =
#if NET5_0_OR_GREATER
                response.Content.ReadAsByteArrayAsync(cancellationToken);
#else
            response.Content.ReadAsByteArrayAsync();
#endif
            responseBody = (await readAsByteArrayAsync.ConfigureAwait(false)).AsMemory();
        } catch (InvalidOperationException) {
            // leave responseBody null
        }

        return new HttpExceptionParams(request?.Method ?? HttpMethod.Get, request?.RequestUri, response.Headers, responseBody);
    }

}