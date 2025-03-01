using Unfucked.HTTP;

namespace Unfucked;

public static class WebTargetExtensions {

    public static WebTarget Target(this HttpClient httpClient, Uri uri) => new(httpClient, uri);
    public static WebTarget Target(this HttpClient httpClient, string uri) => new(httpClient, uri);
    public static WebTarget Target(this HttpClient httpClient, URIBuilder uriBuilder) => new(httpClient, uriBuilder);
    public static WebTarget Target(this HttpClient httpClient, UriBuilder uriBuilder) => new(httpClient, uriBuilder);

}