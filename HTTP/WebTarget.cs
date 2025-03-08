using System.Diagnostics.Contracts;
using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using Unfucked.HTTP.Config;

namespace Unfucked.HTTP;

public interface WebTarget: Configurable<WebTarget> {

    [Pure]
    Uri Url { get; }

    [Pure]
    UnfuckedWebTarget UserInfo(string? userInfo);

    [Pure]
    UnfuckedWebTarget Path(string? segments, bool autoSplit = true);

    [Pure]
    UnfuckedWebTarget Path(object segments);

    [Pure]
    UnfuckedWebTarget Path(params IEnumerable<string> segments);

    [Pure]
    UnfuckedWebTarget Port(ushort? port);

    [Pure]
    UnfuckedWebTarget Hostname(string hostname);

    [Pure]
    UnfuckedWebTarget Scheme(string scheme);

    [Pure]
    UnfuckedWebTarget QueryParam(string key, object? value);

    [Pure]
    UnfuckedWebTarget QueryParam(string key, IEnumerable<object> values);

    [Pure]
    UnfuckedWebTarget QueryParam(IEnumerable<KeyValuePair<string, string>>? parameters);

    [Pure]
    UnfuckedWebTarget QueryParam(IEnumerable<KeyValuePair<string, object>>? parameters);

    [Pure]
    UnfuckedWebTarget Fragment(string? fragment);

    [Pure]
    UnfuckedWebTarget ResolveTemplate(string key, object? value);

    [Pure]
    UnfuckedWebTarget ResolveTemplate(IEnumerable<KeyValuePair<string, object?>> values);

    [Pure]
    UnfuckedWebTarget ResolveTemplate(object anonymousType);

    [Pure]
    UnfuckedWebTarget Header(string key, object? value);

    [Pure]
    UnfuckedWebTarget Header(string key, params IEnumerable<object> values);

    [Pure]
    UnfuckedWebTarget Accept(params IEnumerable<string> mediaTypes);

    [Pure]
    UnfuckedWebTarget Accept(params IEnumerable<MediaTypeHeaderValue> mediaTypes);

    [Pure]
    UnfuckedWebTarget AcceptEncoding(params IEnumerable<string> encodings);

    [Pure]
    UnfuckedWebTarget AcceptLanguage(params IEnumerable<string> languages);

    [Pure]
    UnfuckedWebTarget AcceptLanguage(params IEnumerable<CultureInfo> languages);

    [Pure]
    UnfuckedWebTarget CacheControl(string cacheControl);

    [Pure]
    UnfuckedWebTarget CacheControl(CacheControlHeaderValue cacheControl);

    [Pure]
    UnfuckedWebTarget Cookie(Cookie cookie);

    [Pure]
    UnfuckedWebTarget Cookie(string key, string value);

    [Pure]
    UnfuckedWebTarget UserAgent(string userAgentString);

    [Pure]
    UnfuckedWebTarget UserAgent(ProductInfoHeaderValue userAgentString);

    [Pure]
    UnfuckedWebTarget Authorization(string credentials);

    [Pure]
    UnfuckedWebTarget Authorization(string username, string password);

    [Pure]
    UnfuckedWebTarget Authorization(NetworkCredential credentials);

    [Pure]
    UnfuckedWebTarget Referrer(string referrer);

    [Pure]
    UnfuckedWebTarget Referrer(Uri referrer);

    [Pure]
    UnfuckedWebTarget RequestedWith(string requester = "XMLHttpRequest");

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