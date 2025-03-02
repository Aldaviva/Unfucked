using Unfucked.HTTP;

// ReSharper disable once CheckNamespace - putting it in a child namespace makes it harder for users to find it with autocomplete, because it won't be imported by "using Unfucked;"
namespace Unfucked;

public static class WebTargetExtensions {

    public static WebTarget Target(this HttpClient httpClient, Uri uri) => new(httpClient, uri);
    public static WebTarget Target(this HttpClient httpClient, string uri) => new(httpClient, uri);
    public static WebTarget Target(this HttpClient httpClient, UrlBuilder urlBuilder) => new(httpClient, urlBuilder);
    public static WebTarget Target(this HttpClient httpClient, UriBuilder uriBuilder) => new(httpClient, uriBuilder);

}