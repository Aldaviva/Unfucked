namespace Unfucked.HTTP;

public partial class WebTarget {

    private WebTarget With(UrlBuilder newUrlBuilder) => new(client, newUrlBuilder, clientHandler, clientConfig) { Headers = Headers };

    public Uri Url => urlBuilder.ToUrl();

    public WebTarget UserInfo(string? userInfo) => With(urlBuilder.UserInfo(userInfo));

    public WebTarget Path(string? segments, bool autoSplit = true) => With(urlBuilder.Path(segments, autoSplit));

    public WebTarget Path(object segments) => With(urlBuilder.Path(segments));

    public WebTarget Path(params IEnumerable<string> segments) => With(urlBuilder.Path(segments));

    public WebTarget Port(ushort? port) => With(urlBuilder.Port(port));

    public WebTarget Hostname(string hostname) => With(urlBuilder.Hostname(hostname));

    public WebTarget Scheme(string scheme) => With(urlBuilder.Scheme(scheme));

    public WebTarget QueryParam(string key, object? value) => With(urlBuilder.QueryParam(key, value));

    public WebTarget QueryParam(string key, IEnumerable<object> values) => With(urlBuilder.QueryParam(key, values));

    public WebTarget QueryParam(IEnumerable<KeyValuePair<string, object>>? parameters) => With(urlBuilder.QueryParam(parameters));

    public WebTarget Fragment(string? fragment) => With(urlBuilder.Fragment(fragment));

    public WebTarget ResolveTemplate(string key, object? value) => With(urlBuilder.ResolveTemplate(key, value));

    public WebTarget ResolveTemplate(IEnumerable<KeyValuePair<string, object?>> values) => With(urlBuilder.ResolveTemplate(values));

    public WebTarget ResolveTemplate(object anonymousType) => With(urlBuilder.ResolveTemplate(anonymousType));

}