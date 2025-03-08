using System.Collections.Immutable;
using System.Diagnostics.Contracts;
using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Text;

namespace Unfucked.HTTP;

public partial class UnfuckedWebTarget {

    private ImmutableList<KeyValuePair<string, string>> Headers { get; init; } = ImmutableList<KeyValuePair<string, string>>.Empty;

    [Pure]
    public UnfuckedWebTarget Header(string key, object? value) => new(client, urlBuilder, clientHandler, clientConfig) {
        Headers = value is null
            ? Headers.RemoveAll(pair => pair.Key == key)
            : Headers.Add(new KeyValuePair<string, string>(key, value.ToString() ?? string.Empty))
    };

    [Pure]
    public UnfuckedWebTarget Header(string key, params IEnumerable<object> values) => new(client, urlBuilder, clientHandler, clientConfig) {
        Headers = Headers.AddRange(values.Select(value => new KeyValuePair<string, string>(key, value.ToString() ?? string.Empty)))
    };

    [Pure]
    public UnfuckedWebTarget Accept(params IEnumerable<string> mediaTypes) => Header(HttpHeaders.Accept, mediaTypes);

    [Pure]
    public UnfuckedWebTarget Accept(params IEnumerable<MediaTypeHeaderValue> mediaTypes) => Accept(mediaTypes.Select(mediaType => mediaType.ToString()));

    [Pure]
    public UnfuckedWebTarget AcceptEncoding(params IEnumerable<string> encodings) => Header(HttpHeaders.AcceptEncoding, encodings);

    [Pure]
    public UnfuckedWebTarget AcceptLanguage(params IEnumerable<string> languages) => Header(HttpHeaders.AcceptLanguage, languages);

    [Pure]
    public UnfuckedWebTarget AcceptLanguage(params IEnumerable<CultureInfo> languages) => AcceptLanguage(languages.Select(culture => culture.IetfLanguageTag));

    [Pure]
    public UnfuckedWebTarget CacheControl(string cacheControl) => Header(HttpHeaders.CacheControl, cacheControl);

    [Pure]
    public UnfuckedWebTarget CacheControl(CacheControlHeaderValue cacheControl) => CacheControl(cacheControl.ToString());

    [Pure]
    public UnfuckedWebTarget Cookie(Cookie cookie) => Header(HttpHeaders.Cookie, cookie.ToString());

    [Pure]
    public UnfuckedWebTarget Cookie(string key, string value) => Cookie(new Cookie(key, value));

    [Pure]
    public UnfuckedWebTarget UserAgent(string userAgentString) => Header(HttpHeaders.UserAgent, userAgentString);

    [Pure]
    public UnfuckedWebTarget UserAgent(ProductInfoHeaderValue userAgentString) => Header(HttpHeaders.UserAgent, userAgentString.ToString());

    [Pure]
    public UnfuckedWebTarget Authorization(string credentials) => Header(HttpHeaders.Authorization, credentials);

    [Pure]
    public UnfuckedWebTarget Authorization(string username, string password) => Authorization(Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{password}"), Base64FormattingOptions.None));

    [Pure]
    public UnfuckedWebTarget Authorization(NetworkCredential credentials) => Authorization(credentials.UserName, credentials.Password);

    [Pure]
    public UnfuckedWebTarget Referrer(string referrer) => Header(HttpHeaders.Referrer, referrer);

    [Pure]
    public UnfuckedWebTarget Referrer(Uri referrer) => Referrer(referrer.AbsoluteUri);

    [Pure]
    public UnfuckedWebTarget RequestedWith(string requester = "XMLHttpRequest") => Header(HttpHeaders.XRequestedWith, requester);

}