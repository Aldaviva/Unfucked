using System.Collections.Immutable;
using System.Diagnostics.Contracts;
using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Text;

namespace Unfucked.HTTP;

public partial class WebTarget {

    private ImmutableList<KeyValuePair<string, string>> Headers { get; init; } = ImmutableList<KeyValuePair<string, string>>.Empty;

    [Pure]
    public WebTarget Header(string key, object? value) => new(client, urlBuilder, clientHandler, clientConfig) {
        Headers = value is null
            ? Headers.RemoveAll(pair => pair.Key == key)
            : Headers.Add(new KeyValuePair<string, string>(key, value.ToString() ?? string.Empty))
    };

    [Pure]
    public WebTarget Header(string key, params IEnumerable<object> values) => new(client, urlBuilder, clientHandler, clientConfig) {
        Headers = Headers.AddRange(values.Select(value => new KeyValuePair<string, string>(key, value.ToString() ?? string.Empty)))
    };

    [Pure]
    public WebTarget Accept(params IEnumerable<string> mediaTypes) => Header(HttpHeaders.Accept, mediaTypes);

    [Pure]
    public WebTarget Accept(params IEnumerable<MediaTypeHeaderValue> mediaTypes) => Accept(mediaTypes.Select(mediaType => mediaType.ToString()));

    [Pure]
    public WebTarget AcceptEncoding(params IEnumerable<string> encodings) => Header(HttpHeaders.AcceptEncoding, encodings);

    [Pure]
    public WebTarget AcceptLanguage(params IEnumerable<string> languages) => Header(HttpHeaders.AcceptLanguage, languages);

    [Pure]
    public WebTarget AcceptLanguage(params IEnumerable<CultureInfo> languages) => AcceptLanguage(languages.Select(culture => culture.IetfLanguageTag));

    [Pure]
    public WebTarget CacheControl(string cacheControl) => Header(HttpHeaders.CacheControl, cacheControl);

    [Pure]
    public WebTarget CacheControl(CacheControlHeaderValue cacheControl) => CacheControl(cacheControl.ToString());

    [Pure]
    public WebTarget Cookie(Cookie cookie) => Header(HttpHeaders.Cookie, cookie.ToString());

    [Pure]
    public WebTarget Cookie(string key, string value) => Cookie(new Cookie(key, value));

    [Pure]
    public WebTarget UserAgent(string userAgentString) => Header(HttpHeaders.UserAgent, userAgentString);

    [Pure]
    public WebTarget UserAgent(ProductInfoHeaderValue userAgentString) => Header(HttpHeaders.UserAgent, userAgentString.ToString());

    [Pure]
    public WebTarget Authorization(string credentials) => Header(HttpHeaders.Authorization, credentials);

    [Pure]
    public WebTarget Authorization(string username, string password) => Authorization(Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{password}"), Base64FormattingOptions.None));

    [Pure]
    public WebTarget Authorization(NetworkCredential credentials) => Authorization(credentials.UserName, credentials.Password);

    [Pure]
    public WebTarget Referrer(string referrer) => Header(HttpHeaders.Referrer, referrer);

    [Pure]
    public WebTarget Referrer(Uri referrer) => Referrer(referrer.AbsoluteUri);

    [Pure]
    public WebTarget RequestedWith(string requester = "XMLHttpRequest") => Header(HttpHeaders.XRequestedWith, requester);

}