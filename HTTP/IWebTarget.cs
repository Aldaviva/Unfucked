using System.Diagnostics.Contracts;
using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using Unfucked.HTTP.Config;

namespace Unfucked.HTTP;

public interface IWebTarget: IHttpConfiguration<IWebTarget> {

    [Pure]
    Uri Url { get; }

    [Pure]
    WebTarget UserInfo(string? userInfo);

    [Pure]
    WebTarget Path(string? segments, bool autoSplit = true);

    [Pure]
    WebTarget Path(object segments);

    [Pure]
    WebTarget Path(params IEnumerable<string> segments);

    [Pure]
    WebTarget Port(ushort? port);

    [Pure]
    WebTarget Hostname(string hostname);

    [Pure]
    WebTarget Scheme(string scheme);

    [Pure]
    WebTarget QueryParam(string key, object? value);

    [Pure]
    WebTarget QueryParam(string key, IEnumerable<object> values);

    [Pure]
    WebTarget QueryParam(IEnumerable<KeyValuePair<string, string>>? parameters);

    [Pure]
    WebTarget QueryParam(IEnumerable<KeyValuePair<string, object>>? parameters);

    [Pure]
    WebTarget Fragment(string? fragment);

    [Pure]
    WebTarget ResolveTemplate(string key, object? value);

    [Pure]
    WebTarget ResolveTemplate(IEnumerable<KeyValuePair<string, object?>> values);

    [Pure]
    WebTarget ResolveTemplate(object anonymousType);

    [Pure]
    WebTarget Header(string key, object? value);

    [Pure]
    WebTarget Header(string key, params IEnumerable<object> values);

    [Pure]
    WebTarget Accept(params IEnumerable<string> mediaTypes);

    [Pure]
    WebTarget Accept(params IEnumerable<MediaTypeHeaderValue> mediaTypes);

    [Pure]
    WebTarget AcceptEncoding(params IEnumerable<string> encodings);

    [Pure]
    WebTarget AcceptLanguage(params IEnumerable<string> languages);

    [Pure]
    WebTarget AcceptLanguage(params IEnumerable<CultureInfo> languages);

    [Pure]
    WebTarget CacheControl(string cacheControl);

    [Pure]
    WebTarget CacheControl(CacheControlHeaderValue cacheControl);

    [Pure]
    WebTarget Cookie(Cookie cookie);

    [Pure]
    WebTarget Cookie(string key, string value);

    [Pure]
    WebTarget UserAgent(string userAgentString);

    [Pure]
    WebTarget UserAgent(ProductInfoHeaderValue userAgentString);

    [Pure]
    WebTarget Authorization(string credentials);

    [Pure]
    WebTarget Authorization(string username, string password);

    [Pure]
    WebTarget Authorization(NetworkCredential credentials);

    [Pure]
    WebTarget Referrer(string referrer);

    [Pure]
    WebTarget Referrer(Uri referrer);

    [Pure]
    WebTarget RequestedWith(string requester = "XMLHttpRequest");

    Task<HttpResponseMessage> Send(HttpMethod verb, HttpContent? requestBody = null, CancellationToken cancellationToken = default);

    Task<T> Send<T>(HttpMethod verb, HttpContent? requestBody = null, CancellationToken cancellationToken = default);

    Task<HttpResponseMessage> Get(CancellationToken cancellationToken = default);

    Task<T> Get<T>(CancellationToken cancellationToken = default);

    Task<HttpResponseMessage> Head(CancellationToken cancellationToken = default);

    Task<HttpResponseMessage> Post(HttpContent? requestBody, CancellationToken cancellationToken = default);

    Task<T> Post<T>(HttpContent? requestBody, CancellationToken cancellationToken = default);

    Task<HttpResponseMessage> Put(HttpContent? requestBody, CancellationToken cancellationToken = default);

    Task<T> Put<T>(HttpContent? requestBody, CancellationToken cancellationToken = default);

    Task<HttpResponseMessage> Patch(HttpContent? requestBody, CancellationToken cancellationToken = default);

    Task<T> Patch<T>(HttpContent? requestBody, CancellationToken cancellationToken = default);

    Task<HttpResponseMessage> Delete(HttpContent? requestBody = null, CancellationToken cancellationToken = default);

    Task<T> Delete<T>(HttpContent? requestBody = null, CancellationToken cancellationToken = default);

}