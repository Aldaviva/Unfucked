using System.Collections.Immutable;
using System.Diagnostics.Contracts;
using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Text;

namespace Unfucked.HTTP;

public partial class WebTarget {

    private ImmutableList<KeyValuePair<string, string>> Headers { get; init; } = ImmutableList<KeyValuePair<string, string>>.Empty;

    /// <inheritdoc />
    [Pure]
    public IWebTarget Header(string key, object? value) => new WebTarget(client, urlBuilder, clientHandler, clientConfig) {
        Headers = value is null
            ? Headers.RemoveAll(pair => pair.Key == key)
            : Headers.Add(new KeyValuePair<string, string>(key, value.ToString() ?? string.Empty))
    };

    /// <inheritdoc />
    [Pure]
    public IWebTarget Header(string key, params IEnumerable<object?> values) => new WebTarget(client, urlBuilder, clientHandler, clientConfig) {
        Headers = Headers.AddRange(values.Compact().Select(value => new KeyValuePair<string, string>(key, value.ToString() ?? string.Empty)))
    };

    /// <inheritdoc />
    [Pure]
    public IWebTarget Header(IEnumerable<KeyValuePair<string, object?>>? headers) => new WebTarget(client, urlBuilder, clientHandler, clientConfig) {
        Headers = headers is null
            ? ImmutableList<KeyValuePair<string, string>>.Empty
            : Headers.AddRange(headers.Where(pair => pair.Value is not null).Select(pair => new KeyValuePair<string, string>(pair.Key, pair.Value!.ToString() ?? string.Empty)))
    };

    /// <inheritdoc />
    [Pure]
    public IWebTarget Header(IEnumerable<KeyValuePair<string, string>> headers) => new WebTarget(client, urlBuilder, clientHandler, clientConfig) {
        Headers = Headers.AddRange(headers.Select(pair => new KeyValuePair<string, string>(pair.Key, pair.Value)))
    };

    /// <inheritdoc />
    [Pure]
    public IWebTarget Accept(params IEnumerable<string> mediaTypes) => Header(HttpHeaders.ACCEPT, mediaTypes);

    /// <inheritdoc />
    [Pure]
    public IWebTarget Accept(params IEnumerable<MediaTypeHeaderValue> mediaTypes) => Accept(mediaTypes.Select(mediaType => mediaType.ToString()));

    /// <inheritdoc />
    [Pure]
    public IWebTarget AcceptEncoding(params IEnumerable<string> encodings) => Header(HttpHeaders.ACCEPT_ENCODING, encodings);

    /// <inheritdoc />
    [Pure]
    public IWebTarget AcceptLanguage(params IEnumerable<string> languages) => Header(HttpHeaders.ACCEPT_LANGUAGE, languages);

    /// <inheritdoc />
    [Pure]
    public IWebTarget AcceptLanguage(params IEnumerable<CultureInfo> languages) => AcceptLanguage(languages.Select(culture => culture.IetfLanguageTag));

    /// <inheritdoc />
    [Pure]
    public IWebTarget CacheControl(string cacheControl) => Header(HttpHeaders.CACHE_CONTROL, cacheControl);

    /// <inheritdoc />
    [Pure]
    public IWebTarget CacheControl(CacheControlHeaderValue cacheControl) => CacheControl(cacheControl.ToString());

    /// <inheritdoc />
    [Pure]
    public IWebTarget Cookie(Cookie cookie) => Header(HttpHeaders.COOKIE, cookie.ToString());

    /// <inheritdoc />
    [Pure]
    public IWebTarget Cookie(string key, string value) => Cookie(new Cookie(key, value));

    /// <inheritdoc />
    [Pure]
    public IWebTarget UserAgent(string userAgentString) => Header(HttpHeaders.USER_AGENT, userAgentString);

    /// <inheritdoc />
    [Pure]
    public IWebTarget UserAgent(ProductInfoHeaderValue userAgentString) => Header(HttpHeaders.USER_AGENT, userAgentString.ToString());

    /// <inheritdoc />
    [Pure]
    public IWebTarget Authorization(string credentials) => Header(HttpHeaders.AUTHORIZATION, credentials);

    /// <inheritdoc />
    [Pure]
    public IWebTarget Authorization(string username, string password) => Authorization(Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{password}"), Base64FormattingOptions.None));

    /// <inheritdoc />
    [Pure]
    public IWebTarget Authorization(NetworkCredential credentials) => Authorization(credentials.UserName, credentials.Password);

    /// <inheritdoc />
    [Pure]
    public IWebTarget Referrer(string referrer) => Header(HttpHeaders.REFERRER, referrer);

    /// <inheritdoc />
    [Pure]
    public IWebTarget Referrer(Uri referrer) => Referrer(referrer.AbsoluteUri);

    /// <inheritdoc />
    [Pure]
    public IWebTarget RequestedWith(string requester = "XMLHttpRequest") => Header(HttpHeaders.X_REQUESTED_WITH, requester);

}