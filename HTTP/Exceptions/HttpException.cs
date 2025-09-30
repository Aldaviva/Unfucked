using System.Net;
using System.Net.Http.Headers;
using System.Text;

namespace Unfucked.HTTP.Exceptions;

#pragma warning disable CS9113 // Parameter is unread. - the superclass constructor overload that reads it only exists in .NET 6, not .NET Standard 2.0
public abstract class HttpException(HttpStatusCode? status, string message, Exception? cause, HttpExceptionParams p): HttpRequestException(MessageChain(message, cause), cause
#if !NETSTANDARD2_0
    , status
#endif
) {

#pragma warning restore CS9113 // Parameter is unread.

    protected readonly HttpExceptionParams HttpExceptionParams = p;

    public Uri? RequestUrl => HttpExceptionParams.RequestUrl;
    public HttpMethod Verb => HttpExceptionParams.Verb;

    private static string MessageChain(string outerMessage, Exception? cause) {
        StringBuilder chain = new(outerMessage);
        while (cause != null) {
            chain.Append(": ").Append(cause.Message);
            cause = cause.InnerException is var inner && cause != inner ? inner : null;
        }
        return chain.ToString();
    }

}

public record HttpExceptionParams(HttpMethod Verb, Uri? RequestUrl, HttpResponseHeaders? ResponseHeaders, ReadOnlyMemory<byte>? ResponseBody) {

    internal HttpExceptionParams(HttpRequestMessage request): this(request.Method, request.RequestUri, null, null) { }

}

public class WebApplicationException(HttpStatusCode status, string reasonPhrase, HttpExceptionParams p): HttpException(status, $"{(int) status} {reasonPhrase} from {p.RequestUrl}", null, p) {

    public string ReasonPhrase => reasonPhrase;
    public HttpResponseHeaders ResponseHeaders => HttpExceptionParams.ResponseHeaders!;
    public ReadOnlyMemory<byte>? ResponseBody => HttpExceptionParams.ResponseBody;

    public
#if NET5_0_OR_GREATER
        new
#endif
        HttpStatusCode StatusCode => status;

}

public class ClientErrorException(HttpStatusCode status, string reasonPhrase, HttpExceptionParams p): WebApplicationException(status, reasonPhrase, p);

public class BadRequestException(string? reasonPhrase, HttpExceptionParams p): ClientErrorException(HttpStatusCode.BadRequest, reasonPhrase ?? "Bad Request", p);

public class NotAuthorizedException(string? reasonPhrase, HttpExceptionParams p): ClientErrorException(HttpStatusCode.Unauthorized, reasonPhrase ?? "Unauthorized", p);

public class ForbiddenException(string? reasonPhrase, HttpExceptionParams p): ClientErrorException(HttpStatusCode.Forbidden, reasonPhrase ?? "Forbidden", p);

public class NotFoundException(string? reasonPhrase, HttpExceptionParams p): ClientErrorException(HttpStatusCode.NotFound, reasonPhrase ?? "Not Found", p);

public class NotAllowedException(string? reasonPhrase, HttpExceptionParams p): ClientErrorException(HttpStatusCode.MethodNotAllowed, reasonPhrase ?? "Method Not Allowed", p);

public class NotAcceptableException(string? reasonPhrase, HttpExceptionParams p): ClientErrorException(HttpStatusCode.NotAcceptable, reasonPhrase ?? "Not Acceptable", p);

public class NotSupportedException(string? reasonPhrase, HttpExceptionParams p): ClientErrorException(HttpStatusCode.UnsupportedMediaType, reasonPhrase ?? "Unsupported Media Type", p);

public class ServerErrorException(HttpStatusCode statusCode, string reasonPhrase, HttpExceptionParams p): WebApplicationException(statusCode, reasonPhrase, p);

public class InternalServerErrorException(string? reasonPhrase, HttpExceptionParams p): ServerErrorException(HttpStatusCode.InternalServerError, reasonPhrase ?? "Internal Server Error", p);

public class ServiceUnavailableException(string? reasonPhrase, HttpExceptionParams p): ServerErrorException(HttpStatusCode.ServiceUnavailable, reasonPhrase ?? "Service Unavailable", p);

public class RedirectionException(HttpStatusCode statusCode, Uri? destination, string reasonPhrase, HttpExceptionParams p): WebApplicationException(statusCode, reasonPhrase, p) {

    public Uri? Destination { get; } = destination;

}

public class ProcessingException(Exception cause, HttpExceptionParams p): HttpException(null, $"Network, filter, or serialization error during HTTP {p.Verb} request to {p.RequestUrl}", cause, p) {

#if NET5_0_OR_GREATER
    // ReSharper disable once MemberCanBeMadeStatic.Global - unhides parent property, defeating the purpose of hiding it
    [Obsolete(
        $"{nameof(ProcessingException)}s never have status codes, so this property always returns null. They represent failures in network I/O or response deserialization (such as timeouts, refused connections, or malformed JSON), rather than non-200-class status codes, which are instead represented by {nameof(WebApplicationException)}.")]
    public new HttpStatusCode? StatusCode => null;
#endif

}