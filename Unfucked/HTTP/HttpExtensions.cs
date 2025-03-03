using Unfucked.HTTP;

// ReSharper disable once CheckNamespace - putting it in a child namespace makes it harder for users to find it with autocomplete, because it won't be imported by "using Unfucked;"
namespace Unfucked;

public static class HttpExtensions {

    public static IWebTarget Target(this HttpClient httpClient, Uri uri) => new WebTarget(httpClient, uri);
    public static IWebTarget Target(this HttpClient httpClient, string uri) => new WebTarget(httpClient, uri);
    public static IWebTarget Target(this HttpClient httpClient, UrlBuilder urlBuilder) => new WebTarget(httpClient, urlBuilder);
    public static IWebTarget Target(this HttpClient httpClient, UriBuilder uriBuilder) => new WebTarget(httpClient, uriBuilder);

    public static HttpClient Register(this HttpClient httpClient, ClientRequestFilter filter, int position = HttpConfiguration.LastPosition) {
        _ = UnfuckedHttpHandler.FindHandler(httpClient)?.Register(filter, position) ?? throw WebTarget.ConfigUnavailable;
        return httpClient;
    }

    public static HttpClient Register(this HttpClient httpClient, ClientResponseFilter filter, int position = HttpConfiguration.LastPosition) {
        _ = UnfuckedHttpHandler.FindHandler(httpClient)?.Register(filter, position) ?? throw WebTarget.ConfigUnavailable;
        return httpClient;
    }

    public static HttpClient Property<T>(this HttpClient httpClient, PropertyKey<T> key, T? value) where T: notnull {
        _ = UnfuckedHttpHandler.FindHandler(httpClient)?.Property(key, value) ?? throw WebTarget.ConfigUnavailable;
        return httpClient;
    }

    public static bool Property<T>(this HttpClient httpClient, PropertyKey<T> key, out T? existingValue) where T: notnull =>
        UnfuckedHttpHandler.FindHandler(httpClient) is { } handler ? handler.Property(key, out existingValue) : throw WebTarget.ConfigUnavailable;

}