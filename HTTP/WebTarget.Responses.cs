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

    private static readonly IReadOnlyList<MessageBodyReader> DEFAULT_MESSAGE_BODY_READERS;

    static WebTarget() {
        StringBodyReader stringBodyReader;
        List<MessageBodyReader> defaultMessageBodyReaders = [
            new StreamBodyReader(),
            new ByteArrayBodyReader.ReadOnlyMemoryBodyReader(),
            new ByteArrayBodyReader.MemoryBodyReader(),
            new ByteArrayBodyReader(),
            stringBodyReader = new StringBodyReader(),
            new JsonBodyReader(),
            new XmlBodyReader.XDocumentReader(),
            new XmlBodyReader.XmlDocumentReader(),
            new XmlBodyReader.XPathReader(),
            new XmlBodyReader()
        ];

        try {
            defaultMessageBodyReaders.Add(new HtmlDocumentReader());
        } catch (FileNotFoundException) {
            // library consumer doesn't depend on AngleSharp, so skip this reader
        }

        defaultMessageBodyReaders.Add(new GenericBodyReader(stringBodyReader));

        DEFAULT_MESSAGE_BODY_READERS = defaultMessageBodyReaders.AsReadOnlyList();
    }

    /// <exception cref="ProcessingException">response parsing failed</exception>
    /// <exception cref="WebApplicationException">the response status code was not successful, and <see cref="PropertyKey.ThrowOnUnsuccessfulStatusCode"/> was left enabled</exception>
    /// <exception cref="OperationCanceledException"><paramref name="cancellationToken"/> was canceled</exception>
    private async Task<T> ParseResponseBody<T>(HttpResponseMessage response, CancellationToken cancellationToken) {
        if (typeof(T) == typeof(HttpResponseMessage)) {
            return (T) (object) response;
        } else if (!Property(PropertyKey.ThrowOnUnsuccessfulStatusCode, out bool value) || value) {
            await ThrowIfUnsuccessful(response, cancellationToken).ConfigureAwait(false);
        }

        MediaTypeHeaderValue? responseContentType = response.Content.Headers.ContentType;
        Encoding?             responseEncoding    = Entity.ParseEncoding(responseContentType?.CharSet);

        IEnumerable<MessageBodyReader> messageBodyReaders = (clientConfig?.MessageBodyReaders ?? []).Concat(DEFAULT_MESSAGE_BODY_READERS).ToList();

        foreach (MessageBodyReader reader in messageBodyReaders) {
            cancellationToken.ThrowIfCancellationRequested();
            if (reader.CanRead<T>(responseContentType, null)) {
                try {
                    Task<Stream> responseBodyStream = response.Content
#if NET5_0_OR_GREATER
                        .ReadAsStreamAsync(cancellationToken);
#else
                        .ReadAsStreamAsync();
#endif

                    return await reader.Read<T>(await responseBodyStream.ConfigureAwait(false), response.StatusCode, response.Headers, response.Content.Headers, response.RequestMessage!, response,
                        responseEncoding, clientConfig, cancellationToken).ConfigureAwait(false);
                } catch (Exception e) when (e is not OutOfMemoryException) {
                    throw new ProcessingException(e, await HttpExceptionParams.FromResponse(response, cancellationToken).ConfigureAwait(false));
                }
            }
        }

        // await response.Content.LoadIntoBufferAsync().ConfigureAwait(false);

#if NET5_0_OR_GREATER
        await using Stream bodyStream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
#else
        using Stream bodyStream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
#endif
        const int maxPrefixLengthChars = 32;
        Encoding  encoding             = responseEncoding ?? Encoding.UTF8;

#if NET5_0_OR_GREATER
        await using
#else
        using
#endif
            PartiallyBufferedStream partialBufferedBodyStream = await PartiallyBufferedStream.CreateAndBuffer(bodyStream, encoding.GetMaxByteCount(maxPrefixLengthChars)).ConfigureAwait(false);
        using StreamReader bodyReader = new(partialBufferedBodyStream, encoding, true);
        // using StreamReader bodyReader = new(bodyStream, responseEncoding ?? Encoding.UTF8, true);

        char[] prefixBuffer = new char[maxPrefixLengthChars];

        int prefixLengthChars;
#if NETCOREAPP2_1_OR_GREATER
        try {
            prefixLengthChars = await bodyReader.ReadAsync(prefixBuffer.AsMemory(), cancellationToken).ConfigureAwait(false);
        } catch (OperationCanceledException e) {
            throw new ProcessingException(e, await HttpExceptionParams.FromResponse(response, CancellationToken.None).ConfigureAwait(false));
        }
#else
        prefixLengthChars = await bodyReader.ReadAsync(prefixBuffer, 0, prefixBuffer.Length).ConfigureAwait(false);
#endif
        string bodyPrefix = new string(prefixBuffer, 0, prefixLengthChars).Trim();
        partialBufferedBodyStream.Position = 0;

        foreach (MessageBodyReader reader in messageBodyReaders) {
            cancellationToken.ThrowIfCancellationRequested();
            if (reader.CanRead<T>(responseContentType, bodyPrefix)) {
                try {
                    return await reader.Read<T>(partialBufferedBodyStream, response.StatusCode, response.Headers, response.Content.Headers, response.RequestMessage!, response, responseEncoding,
                        clientConfig, cancellationToken).ConfigureAwait(false);
                } catch (Exception e) when (e is not OutOfMemoryException) {
                    throw new ProcessingException(e, await HttpExceptionParams.FromResponse(response, cancellationToken).ConfigureAwait(false));
                }
            }
        }

        HttpExceptionParams p = await HttpExceptionParams.FromResponse(response, cancellationToken).ConfigureAwait(false);
        throw new ProcessingException(new SerializationException(
                $"Could not find deserializer for response body. You may need to register a new {nameof(MessageBodyReader)} to your {nameof(IHttpClient)}, {nameof(IClientConfig)}, or {nameof(IWebTarget)}. (URI: {p.RequestUrl}, Content-Type: {responseContentType}, .NET type: {typeof(T)})"),
            p);
    }

    /// <exception cref="WebApplicationException">the response status code was not successful</exception>
    internal static async Task ThrowIfUnsuccessful(HttpResponseMessage response, CancellationToken cancellationToken) {
        if (!response.IsSuccessStatusCode) {
            HttpStatusCode      statusCode      = response.StatusCode;
            string              reason          = response.ReasonPhrase ?? statusCode.ToString();
            HttpExceptionParams exceptionParams = await HttpExceptionParams.FromResponse(response, cancellationToken).ConfigureAwait(false);
            response.Dispose();
            throw statusCode switch {
                HttpStatusCode.BadRequest           => new BadRequestException(reason, exceptionParams),
                HttpStatusCode.Unauthorized         => new NotAuthorizedException(reason, exceptionParams),
                HttpStatusCode.Forbidden            => new ForbiddenException(reason, exceptionParams),
                HttpStatusCode.NotFound             => new NotFoundException(reason, exceptionParams),
                HttpStatusCode.MethodNotAllowed     => new NotAllowedException(reason, exceptionParams),
                HttpStatusCode.NotAcceptable        => new NotAcceptableException(reason, exceptionParams),
                HttpStatusCode.UnsupportedMediaType => new NotSupportedException(reason, exceptionParams),
                HttpStatusCode.InternalServerError  => new InternalServerErrorException(reason, exceptionParams),
                HttpStatusCode.ServiceUnavailable   => new ServiceUnavailableException(reason, exceptionParams),

                >= HttpStatusCode.MultipleChoices and < HttpStatusCode.BadRequest     => new RedirectionException(statusCode, response.Headers.Location, reason, exceptionParams),
                >= HttpStatusCode.BadRequest and < HttpStatusCode.InternalServerError => new ClientErrorException(statusCode, reason, exceptionParams),
                >= HttpStatusCode.InternalServerError and < (HttpStatusCode) 600      => new ServerErrorException(statusCode, reason, exceptionParams),

                _ => new WebApplicationException(statusCode, reason, exceptionParams)
            };
        }
    }

}