using System.Diagnostics.Contracts;

namespace Unfucked.HTTP;

public partial class UnfuckedWebTarget {

    [Pure]
    private UnfuckedWebTarget With(UrlBuilder newUrlBuilder) => new(client, newUrlBuilder, clientHandler, clientConfig) { Headers = Headers };

    [Pure]
    public Uri Url => urlBuilder.ToUrl();

    [Pure]
    public UnfuckedWebTarget UserInfo(string? userInfo) => With(urlBuilder.UserInfo(userInfo));

    [Pure]
    public UnfuckedWebTarget Path(string? segments, bool autoSplit = true) => With(urlBuilder.Path(segments, autoSplit));

    [Pure]
    public UnfuckedWebTarget Path(object segments) => With(urlBuilder.Path(segments));

    [Pure]
    public UnfuckedWebTarget Path(params IEnumerable<string> segments) => With(urlBuilder.Path(segments));

    [Pure]
    public UnfuckedWebTarget Port(ushort? port) => With(urlBuilder.Port(port));

    [Pure]
    public UnfuckedWebTarget Hostname(string hostname) => With(urlBuilder.Hostname(hostname));

    [Pure]
    public UnfuckedWebTarget Scheme(string scheme) => With(urlBuilder.Scheme(scheme));

    [Pure]
    public UnfuckedWebTarget QueryParam(string key, object? value) => With(urlBuilder.QueryParam(key, value));

    [Pure]
    public UnfuckedWebTarget QueryParam(string key, IEnumerable<object> values) => With(urlBuilder.QueryParam(key, values));

    [Pure]
    public UnfuckedWebTarget QueryParam(IEnumerable<KeyValuePair<string, string>>? parameters) => With(urlBuilder.QueryParam(parameters));

    [Pure]
    public UnfuckedWebTarget QueryParam(IEnumerable<KeyValuePair<string, object>>? parameters) => With(urlBuilder.QueryParam(parameters));

    [Pure]
    public UnfuckedWebTarget Fragment(string? fragment) => With(urlBuilder.Fragment(fragment));

    [Pure]
    public UnfuckedWebTarget ResolveTemplate(string key, object? value) => With(urlBuilder.ResolveTemplate(key, value));

    [Pure]
    public UnfuckedWebTarget ResolveTemplate(IEnumerable<KeyValuePair<string, object?>> values) => With(urlBuilder.ResolveTemplate(values));

    [Pure]
    public UnfuckedWebTarget ResolveTemplate(object anonymousType) => With(urlBuilder.ResolveTemplate(anonymousType));

}