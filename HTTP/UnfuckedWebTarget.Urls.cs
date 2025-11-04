using System.Diagnostics.Contracts;

namespace Unfucked.HTTP;

public partial class WebTarget {

    [Pure]
    private WebTarget With(UrlBuilder newUrlBuilder) => new(client, newUrlBuilder, clientHandler, clientConfig) { Headers = Headers };

    [Pure]
    public Uri Url => urlBuilder.ToUrl();

    [Pure]
    public IWebTarget UserInfo(string? userInfo) => With(urlBuilder.UserInfo(userInfo));

    [Pure]
    public IWebTarget Path(string? segments, bool autoSplit = true) => With(urlBuilder.Path(segments, autoSplit));

    [Pure]
    public IWebTarget Path(object segments) => With(urlBuilder.Path(segments));

    [Pure]
    public IWebTarget Path(params IEnumerable<string> segments) => With(urlBuilder.Path(segments));

    [Pure]
    public IWebTarget Port(ushort? port) => With(urlBuilder.Port(port));

    [Pure]
    public IWebTarget Hostname(string hostname) => With(urlBuilder.Hostname(hostname));

    [Pure]
    public IWebTarget Scheme(string scheme) => With(urlBuilder.Scheme(scheme));

    [Pure]
    public IWebTarget QueryParam(string key, object? value) => With(urlBuilder.QueryParam(key, value));

    [Pure]
    public IWebTarget QueryParam(string key, IEnumerable<object?> values) => With(urlBuilder.QueryParam(key, values));

    [Pure]
    public IWebTarget QueryParam(IEnumerable<KeyValuePair<string, string>> parameters) => With(urlBuilder.QueryParam(parameters));

    [Pure]
    public IWebTarget QueryParam(IEnumerable<KeyValuePair<string, object?>>? parameters) => With(urlBuilder.QueryParam(parameters));

    [Pure]
    public IWebTarget Fragment(string? fragment) => With(urlBuilder.Fragment(fragment));

    [Pure]
    public IWebTarget ResolveTemplate(string key, object? value) => With(urlBuilder.ResolveTemplate(key, value));

    [Pure]
    public IWebTarget ResolveTemplate(IEnumerable<KeyValuePair<string, object?>> values) => With(urlBuilder.ResolveTemplate(values));

    [Pure]
    public IWebTarget ResolveTemplate(object anonymousType) => With(urlBuilder.ResolveTemplate(anonymousType));

}