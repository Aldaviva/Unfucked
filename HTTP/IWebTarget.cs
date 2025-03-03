using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using Unfucked.HTTP.Config;

namespace Unfucked.HTTP;

public interface IWebTarget: IHttpConfiguration<IWebTarget> {

    Uri Url { get; }

    WebTarget UserInfo(string? userInfo);

    WebTarget Path(string? segments, bool autoSplit = true);

    WebTarget Path(object segments);

    WebTarget Path(params IEnumerable<string> segments);

    WebTarget Port(ushort? port);

    WebTarget Hostname(string hostname);

    WebTarget Scheme(string scheme);

    WebTarget QueryParam(string key, object? value);

    WebTarget QueryParam(string key, IEnumerable<object> values);

    WebTarget QueryParam(IEnumerable<KeyValuePair<string, object>>? parameters);

    WebTarget Fragment(string? fragment);

    WebTarget ResolveTemplate(string key, object? value);

    WebTarget ResolveTemplate(IEnumerable<KeyValuePair<string, object?>> values);

    WebTarget ResolveTemplate(object anonymousType);

    WebTarget Header(string key, object? value);

    WebTarget Header(string key, params IEnumerable<object> values);

    WebTarget Accept(params IEnumerable<string> mediaTypes);

    WebTarget Accept(params IEnumerable<MediaTypeHeaderValue> mediaTypes);

    WebTarget AcceptEncoding(params IEnumerable<string> encodings);

    WebTarget AcceptLanguage(params IEnumerable<string> languages);

    WebTarget AcceptLanguage(params IEnumerable<CultureInfo> languages);

    WebTarget CacheControl(string cacheControl);

    WebTarget CacheControl(CacheControlHeaderValue cacheControl);

    WebTarget Cookie(Cookie cookie);

    WebTarget Cookie(string key, string value);

    WebTarget UserAgent(string userAgentString);

    WebTarget UserAgent(ProductInfoHeaderValue userAgentString);

    WebTarget Authorization(string credentials);

    WebTarget Authorization(string username, string password);

    WebTarget Authorization(NetworkCredential credentials);

    WebTarget Referrer(string referrer);

    WebTarget Referrer(Uri referrer);

    WebTarget RequestedWith(string requester = "XMLHttpRequest");

    Task<HttpResponseMessage> Send(HttpMethod verb, HttpContent? requestBody = null, CancellationToken cancellationToken = default);

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