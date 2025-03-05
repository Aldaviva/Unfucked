using System.Diagnostics.Contracts;

namespace Unfucked.HTTP;

public partial class WebTarget {

    [Pure]
    private WebTarget With(UrlBuilder newUrlBuilder) => new(client, newUrlBuilder, clientHandler, clientConfig) { Headers = Headers };

    [Pure]
    public Uri Url => urlBuilder.ToUrl();

    [Pure]
    public WebTarget UserInfo(string? userInfo) => With(urlBuilder.UserInfo(userInfo));

    [Pure]
    public WebTarget Path(string? segments, bool autoSplit = true) => With(urlBuilder.Path(segments, autoSplit));

    [Pure]
    public WebTarget Path(object segments) => With(urlBuilder.Path(segments));

    [Pure]
    public WebTarget Path(params IEnumerable<string> segments) => With(urlBuilder.Path(segments));

    [Pure]
    public WebTarget Port(ushort? port) => With(urlBuilder.Port(port));

    [Pure]
    public WebTarget Hostname(string hostname) => With(urlBuilder.Hostname(hostname));

    [Pure]
    public WebTarget Scheme(string scheme) => With(urlBuilder.Scheme(scheme));

    [Pure]
    public WebTarget QueryParam(string key, object? value) => With(urlBuilder.QueryParam(key, value));

    [Pure]
    public WebTarget QueryParam(string key, IEnumerable<object> values) => With(urlBuilder.QueryParam(key, values));

    [Pure]
    public WebTarget QueryParam(IEnumerable<KeyValuePair<string, string>>? parameters) => With(urlBuilder.QueryParam(parameters));

    [Pure]
    public WebTarget QueryParam(IEnumerable<KeyValuePair<string, object>>? parameters) => With(urlBuilder.QueryParam(parameters));

    [Pure]
    public WebTarget Fragment(string? fragment) => With(urlBuilder.Fragment(fragment));

    [Pure]
    public WebTarget ResolveTemplate(string key, object? value) => With(urlBuilder.ResolveTemplate(key, value));

    [Pure]
    public WebTarget ResolveTemplate(IEnumerable<KeyValuePair<string, object?>> values) => With(urlBuilder.ResolveTemplate(values));

    [Pure]
    public WebTarget ResolveTemplate(object anonymousType) => With(urlBuilder.ResolveTemplate(anonymousType));

}