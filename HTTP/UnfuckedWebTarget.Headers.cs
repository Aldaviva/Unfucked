using System.Collections.Immutable;
using System.Diagnostics.Contracts;
using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Text;

namespace Unfucked.HTTP;

public partial class UnfuckedWebTarget {

    private ImmutableList<KeyValuePair<string, string>> Headers { get; init; } = ImmutableList<KeyValuePair<string, string>>.Empty;

    /// <inheritdoc />
    [Pure]
    public WebTarget Header(string key, object? value) => new UnfuckedWebTarget(client, urlBuilder, clientHandler, clientConfig) {
        Headers = value is null
            ? Headers.RemoveAll(pair => pair.Key == key)
            : Headers.Add(new KeyValuePair<string, string>(key, value.ToString() ?? string.Empty))
    };

    /// <inheritdoc />
    [Pure]
    public WebTarget Header(string key, params IEnumerable<object> values) => new UnfuckedWebTarget(client, urlBuilder, clientHandler, clientConfig) {
        Headers = Headers.AddRange(values.Select(value => new KeyValuePair<string, string>(key, value.ToString() ?? string.Empty)))
    };

    /// <inheritdoc />
    [Pure]
    public WebTarget Accept(params IEnumerable<string> mediaTypes) => Header(HttpHeaders.Accept, mediaTypes);

    /// <inheritdoc />
    [Pure]
    public WebTarget Accept(params IEnumerable<MediaTypeHeaderValue> mediaTypes) => Accept(mediaTypes.Select(mediaType => mediaType.ToString()));

    /// <inheritdoc />
    [Pure]
    public WebTarget AcceptEncoding(params IEnumerable<string> encodings) => Header(HttpHeaders.AcceptEncoding, encodings);

    /// <inheritdoc />
    [Pure]
    public WebTarget AcceptLanguage(params IEnumerable<string> languages) => Header(HttpHeaders.AcceptLanguage, languages);

    /// <inheritdoc />
    [Pure]
    public WebTarget AcceptLanguage(params IEnumerable<CultureInfo> languages) => AcceptLanguage(languages.Select(culture => culture.IetfLanguageTag));

    /// <inheritdoc />
    [Pure]
    public WebTarget CacheControl(string cacheControl) => Header(HttpHeaders.CacheControl, cacheControl);

    /// <inheritdoc />
    [Pure]
    public WebTarget CacheControl(CacheControlHeaderValue cacheControl) => CacheControl(cacheControl.ToString());

    /// <inheritdoc />
    [Pure]
    public WebTarget Cookie(Cookie cookie) => Header(HttpHeaders.Cookie, cookie.ToString());

    /// <inheritdoc />
    [Pure]
    public WebTarget Cookie(string key, string value) => Cookie(new Cookie(key, value));

    /// <inheritdoc />
    [Pure]
    public WebTarget UserAgent(string userAgentString) => Header(HttpHeaders.UserAgent, userAgentString);

    /// <inheritdoc />
    [Pure]
    public WebTarget UserAgent(ProductInfoHeaderValue userAgentString) => Header(HttpHeaders.UserAgent, userAgentString.ToString());

    /// <inheritdoc />
    [Pure]
    public WebTarget Authorization(string credentials) => Header(HttpHeaders.Authorization, credentials);

    /// <inheritdoc />
    [Pure]
    public WebTarget Authorization(string username, string password) => Authorization(Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{password}"), Base64FormattingOptions.None));

    /// <inheritdoc />
    [Pure]
    public WebTarget Authorization(NetworkCredential credentials) => Authorization(credentials.UserName, credentials.Password);

    /// <inheritdoc />
    [Pure]
    public WebTarget Referrer(string referrer) => Header(HttpHeaders.Referrer, referrer);

    /// <inheritdoc />
    [Pure]
    public WebTarget Referrer(Uri referrer) => Referrer(referrer.AbsoluteUri);

    /// <inheritdoc />
    [Pure]
    public WebTarget RequestedWith(string requester = "XMLHttpRequest") => Header(HttpHeaders.XRequestedWith, requester);

}