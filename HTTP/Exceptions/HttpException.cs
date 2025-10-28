using System.Net;
using System.Net.Http.Headers;
using System.Text;

namespace Unfucked.HTTP.Exceptions;

#pragma warning disable CS9113 // Parameter is unread. - the superclass constructor overload that reads it only exists in .NET 6, not .NET Standard 2.0
/// <summary>
/// Base exception class for all network, filtering, deserialization, and HTTP status code errors
/// </summary>
public abstract class HttpException(HttpStatusCode? status, string message, Exception? cause, HttpExceptionParams exceptionParams): HttpRequestException(MessageChain(message, cause), cause
#if !NETSTANDARD2_0
    , status
#endif
) {

#pragma warning restore CS9113 // Parameter is unread.

    protected readonly HttpExceptionParams HttpExceptionParams = exceptionParams;

    public Uri? RequestUrl => HttpExceptionParams.RequestUrl;
    public HttpMethod Verb => HttpExceptionParams.Verb;
    public IDictionary<string, object?>? RequestProperties => HttpExceptionParams.RequestProperties;
#if NET5_0_OR_GREATER
    public HttpRequestOptions? RequestOptions => HttpExceptionParams.RequestOptions;
#endif

    private static string MessageChain(string outerMessage, Exception? cause) {
        StringBuilder chain = new(outerMessage);
        while (cause != null) {
            chain.Append(": ").Append(cause.Message);
            cause = cause.InnerException is var inner && cause != inner ? inner : null;
        }
        return chain.ToString();
    }

}

#pragma warning disable CS0618 // Type or member is obsolete - it's not obsolete in .NET Standard 2.0, which this library targets
public record HttpExceptionParams(
    HttpMethod Verb,
    Uri? RequestUrl,
    HttpResponseHeaders? ResponseHeaders = null,
    ReadOnlyMemory<byte>? ResponseBody = null,
    IDictionary<string, object?>? RequestProperties = null
#if NET5_0_OR_GREATER
    ,
    HttpRequestOptions? RequestOptions = null
#endif
) {

    public static HttpExceptionParams FromRequest(HttpRequestMessage request) => new(request.Method, request.RequestUri, null, null, request.Properties
#if NET5_0_OR_GREATER
        , request.Options
#endif
    );

    public static async Task<HttpExceptionParams> FromResponse(HttpResponseMessage response, CancellationToken cancellationToken = default) {
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

        return new HttpExceptionParams(request?.Method ?? HttpMethod.Get, request?.RequestUri, response.Headers, responseBody, request?.Properties
#if NET5_0_OR_GREATER
            , request?.Options
#endif
        );
    }

}
#pragma warning restore CS0618 // Type or member is obsolete

/// <summary>
/// Unsuccessful HTTP status code
/// </summary>
public class WebApplicationException(HttpStatusCode status, string reasonPhrase, HttpExceptionParams exceptionParams)
    : HttpException(status, $"{(int) status} {reasonPhrase} from {exceptionParams.RequestUrl}", null, exceptionParams) {

    public string ReasonPhrase => reasonPhrase;
    public HttpResponseHeaders ResponseHeaders => HttpExceptionParams.ResponseHeaders!;
    public ReadOnlyMemory<byte>? ResponseBody => HttpExceptionParams.ResponseBody;

    public
#if NET5_0_OR_GREATER
        new
#endif
        HttpStatusCode StatusCode => status;

}

/// <summary>400–499</summary>
public class ClientErrorException(HttpStatusCode status, string reasonPhrase, HttpExceptionParams exceptionParams): WebApplicationException(status, reasonPhrase, exceptionParams);

/// <summary>400</summary>
public class BadRequestException(string? reasonPhrase, HttpExceptionParams exceptionParams): ClientErrorException(HttpStatusCode.BadRequest, reasonPhrase ?? "Bad Request", exceptionParams);

/// <summary>401</summary>
public class NotAuthorizedException(string? reasonPhrase, HttpExceptionParams exceptionParams): ClientErrorException(HttpStatusCode.Unauthorized, reasonPhrase ?? "Unauthorized", exceptionParams);

/// <summary>403</summary>
public class ForbiddenException(string? reasonPhrase, HttpExceptionParams exceptionParams): ClientErrorException(HttpStatusCode.Forbidden, reasonPhrase ?? "Forbidden", exceptionParams);

/// <summary>404</summary>
public class NotFoundException(string? reasonPhrase, HttpExceptionParams exceptionParams): ClientErrorException(HttpStatusCode.NotFound, reasonPhrase ?? "Not Found", exceptionParams);

/// <summary>405</summary>
public class NotAllowedException(string? reasonPhrase, HttpExceptionParams exceptionParams)
    : ClientErrorException(HttpStatusCode.MethodNotAllowed, reasonPhrase ?? "Method Not Allowed", exceptionParams);

/// <summary>406</summary>
public class NotAcceptableException(string? reasonPhrase, HttpExceptionParams exceptionParams): ClientErrorException(HttpStatusCode.NotAcceptable, reasonPhrase ?? "Not Acceptable", exceptionParams);

/// <summary>415</summary>
public class NotSupportedException(string? reasonPhrase, HttpExceptionParams exceptionParams)
    : ClientErrorException(HttpStatusCode.UnsupportedMediaType, reasonPhrase ?? "Unsupported Media Type", exceptionParams);

/// <summary>500–599</summary>
public class ServerErrorException(HttpStatusCode statusCode, string reasonPhrase, HttpExceptionParams exceptionParams): WebApplicationException(statusCode, reasonPhrase, exceptionParams);

/// <summary>500</summary>
public class InternalServerErrorException(string? reasonPhrase, HttpExceptionParams exceptionParams)
    : ServerErrorException(HttpStatusCode.InternalServerError, reasonPhrase ?? "Internal Server Error", exceptionParams);

/// <summary>503</summary>
public class ServiceUnavailableException(string? reasonPhrase, HttpExceptionParams exceptionParams)
    : ServerErrorException(HttpStatusCode.ServiceUnavailable, reasonPhrase ?? "Service Unavailable", exceptionParams);

/// <summary>300–399</summary>
public class RedirectionException(HttpStatusCode statusCode, Uri? destination, string reasonPhrase, HttpExceptionParams exceptionParams)
    : WebApplicationException(statusCode, reasonPhrase, exceptionParams) {

    public Uri? Destination { get; } = destination;

}

/// <summary>Network, filter, or deserialization error</summary>
public class ProcessingException(Exception cause, HttpExceptionParams exceptionParams): HttpException(null,
    $"Network, filter, or deserialization error during HTTP {exceptionParams.Verb} request to {exceptionParams.RequestUrl?.AbsoluteUri}", cause, exceptionParams) {

#if NET5_0_OR_GREATER
    // ReSharper disable once MemberCanBeMadeStatic.Global - unhides parent property, defeating the purpose of hiding it
    [Obsolete(
        $"{nameof(ProcessingException)}s never have status codes, so this property always returns null. They represent failures in network I/O or response deserialization (such as timeouts, refused connections, or malformed JSON), rather than non-200-class status codes, which are instead represented by {nameof(WebApplicationException)}.")]
    public new HttpStatusCode? StatusCode => null;
#endif

}