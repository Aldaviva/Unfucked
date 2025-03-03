using Unfucked.HTTP.Config;
using Unfucked.HTTP.Filters;
using Unfucked.HTTP.Serialization;

namespace Unfucked.HTTP;

public static class Extensions {

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

    public static HttpClient Register(this HttpClient httpClient, MessageBodyReader reader) {
        _ = UnfuckedHttpHandler.FindHandler(httpClient)?.Register(reader) ?? throw WebTarget.ConfigUnavailable;
        return httpClient;
    }

    public static HttpClient Property<T>(this HttpClient httpClient, PropertyKey<T> key, T? value) where T: notnull {
        _ = UnfuckedHttpHandler.FindHandler(httpClient)?.Property(key, value) ?? throw WebTarget.ConfigUnavailable;
        return httpClient;
    }

    public static bool Property<T>(this HttpClient httpClient, PropertyKey<T> key, out T? existingValue) where T: notnull =>
        UnfuckedHttpHandler.FindHandler(httpClient) is { } handler ? handler.Property(key, out existingValue) : throw WebTarget.ConfigUnavailable;

}