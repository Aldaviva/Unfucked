using System.Net;

namespace Unfucked.HTTP.Exceptions;

public interface IHttpRequestException {

    Uri RequestUrl { get; }

}

public class WebApplicationException(HttpStatusCode statusCode, string reasonPhrase): HttpRequestException($"{statusCode} {reasonPhrase}", null
#if !NETSTANDARD2_0
    , statusCode
#endif
), IHttpRequestException {

    internal const string RequestUrlDataKey = "requestUrl";

    public string ReasonPhrase { get; } = reasonPhrase;
    public Uri RequestUrl => (Uri) Data[RequestUrlDataKey]!;

#if NETSTANDARD2_0
    public HttpStatusCode? StatusCode { get; } = statusCode;
#endif

}

public class ClientErrorException(HttpStatusCode statusCode, string reasonPhrase): WebApplicationException(statusCode, reasonPhrase);

public class BadRequestException(string? reasonPhrase): ClientErrorException(HttpStatusCode.BadRequest, reasonPhrase ?? "Bad Request");

public class NotAuthorizedException(string? reasonPhrase): ClientErrorException(HttpStatusCode.Unauthorized, reasonPhrase ?? "Unauthorized");

public class ForbiddenException(string? reasonPhrase): ClientErrorException(HttpStatusCode.Forbidden, reasonPhrase ?? "Forbidden");

public class NotFoundException(string? reasonPhrase): ClientErrorException(HttpStatusCode.NotFound, reasonPhrase ?? "Not Found");

public class NotAllowedException(string? reasonPhrase): ClientErrorException(HttpStatusCode.MethodNotAllowed, reasonPhrase ?? "Method Not Allowed");

public class NotAcceptableException(string? reasonPhrase): ClientErrorException(HttpStatusCode.NotAcceptable, reasonPhrase ?? "Not Acceptable");

public class NotSupportedException(string? reasonPhrase): ClientErrorException(HttpStatusCode.UnsupportedMediaType, reasonPhrase ?? "Unsupported Media Type");

public class ServerErrorException(HttpStatusCode statusCode, string reasonPhrase): WebApplicationException(statusCode, reasonPhrase);

public class InternalServerErrorException(string? reasonPhrase): ServerErrorException(HttpStatusCode.InternalServerError, reasonPhrase ?? "Internal Server Error");

public class ServiceUnavailableException(string? reasonPhrase): ServerErrorException(HttpStatusCode.ServiceUnavailable, reasonPhrase ?? "Service Unavailable");

public class RedirectionException(HttpStatusCode statusCode, Uri? destination, string reasonPhrase): WebApplicationException(statusCode, reasonPhrase) {

    public Uri? Destination { get; } = destination;

}

public class ProcessingException(Exception cause): HttpRequestException("Network, filter, or serialization error", cause), IHttpRequestException {

    public Uri RequestUrl => (Uri) Data[WebApplicationException.RequestUrlDataKey]!;

}