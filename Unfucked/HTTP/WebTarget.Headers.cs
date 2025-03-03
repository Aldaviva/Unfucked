using System.Collections.Immutable;
using System.Globalization;
using System.Net;
using System.Net.Http.Headers;

namespace Unfucked.HTTP;

public partial class WebTarget {

    private ImmutableList<KeyValuePair<string, string>> Headers { get; init; } = ImmutableList<KeyValuePair<string, string>>.Empty;

    public WebTarget Header(string key, object? value) => new(client, urlBuilder, clientHandler, clientConfig) {
        Headers = value is null
            ? Headers.RemoveAll(pair => pair.Key == key)
            : Headers.Add(new KeyValuePair<string, string>(key, value.ToString() ?? string.Empty))
    };

    public WebTarget Header(string key, params IEnumerable<object> values) => new(client, urlBuilder, clientHandler, clientConfig) {
        Headers = Headers.AddRange(values.Select(value => new KeyValuePair<string, string>(key, value.ToString() ?? string.Empty)))
    };

    public WebTarget Accept(params IEnumerable<string> mediaTypes) => Header(HttpHeaders.Accept, mediaTypes);

    public WebTarget Accept(params IEnumerable<MediaTypeHeaderValue> mediaTypes) => Accept(mediaTypes.Select(mediaType => mediaType.ToString()));

    public WebTarget AcceptEncoding(params IEnumerable<string> encodings) => Header(HttpHeaders.AcceptEncoding, encodings);

    public WebTarget AcceptLanguage(params IEnumerable<string> languages) => Header(HttpHeaders.AcceptLanguage, languages);

    public WebTarget AcceptLanguage(params IEnumerable<CultureInfo> languages) => AcceptLanguage(languages.Select(culture => culture.IetfLanguageTag));

    public WebTarget CacheControl(string cacheControl) => Header(HttpHeaders.CacheControl, cacheControl);

    public WebTarget CacheControl(CacheControlHeaderValue cacheControl) => CacheControl(cacheControl.ToString());

    public WebTarget Cookie(Cookie cookie) => Header(HttpHeaders.Cookie, cookie.ToString());

    public WebTarget Cookie(string key, string value) => Cookie(new Cookie(key, value));

    public WebTarget UserAgent(string userAgentString) => Header(HttpHeaders.UserAgent, userAgentString);

    public WebTarget UserAgent(ProductInfoHeaderValue userAgentString) => Header(HttpHeaders.UserAgent, userAgentString.ToString());

    public WebTarget Authorization(string credentials) => Header(HttpHeaders.Authorization, credentials);

    public WebTarget Authorization(string username, string password) => Authorization(Convert.ToBase64String(Strings.Utf8.GetBytes($"{username}:{password}"), Base64FormattingOptions.None));

    public WebTarget Authorization(NetworkCredential credentials) => Authorization(credentials.UserName, credentials.Password);

    public WebTarget Referrer(string referrer) => Header(HttpHeaders.Referrer, referrer);

    public WebTarget Referrer(Uri referrer) => Referrer(referrer.AbsoluteUri);

    public WebTarget RequestedWith(string requester = "XMLHttpRequest") => Header(HttpHeaders.XRequestedWith, requester);

}