using System.Diagnostics.Contracts;
using Unfucked.HTTP.Config;
using Unfucked.HTTP.Filters;
using Unfucked.HTTP.Serialization;

namespace Unfucked.HTTP;

public static class Extensions {

    [Pure]
    public static WebTarget Target(this HttpClient httpClient, Uri uri) => new UnfuckedWebTarget(httpClient, uri);

    [Pure]
    public static WebTarget Target(this HttpClient httpClient, string uri) => new UnfuckedWebTarget(httpClient, uri);

    [Pure]
    public static WebTarget Target(this HttpClient httpClient, UrlBuilder urlBuilder) => new UnfuckedWebTarget(httpClient, urlBuilder);

    [Pure]
    public static WebTarget Target(this HttpClient httpClient, UriBuilder uriBuilder) => new UnfuckedWebTarget(httpClient, uriBuilder);

    public static HttpClient Register(this HttpClient httpClient, ClientRequestFilter filter, int position = ClientConfig.LastFilterPosition) {
        _ = UnfuckedHttpHandler.FindHandler(httpClient)?.Register(filter, position) ?? throw UnfuckedWebTarget.ConfigUnavailable;
        return httpClient;
    }

    public static HttpClient Register(this HttpClient httpClient, ClientResponseFilter filter, int position = ClientConfig.LastFilterPosition) {
        _ = UnfuckedHttpHandler.FindHandler(httpClient)?.Register(filter, position) ?? throw UnfuckedWebTarget.ConfigUnavailable;
        return httpClient;
    }

    public static HttpClient Register(this HttpClient httpClient, MessageBodyReader reader) {
        _ = UnfuckedHttpHandler.FindHandler(httpClient)?.Register(reader) ?? throw UnfuckedWebTarget.ConfigUnavailable;
        return httpClient;
    }

    public static HttpClient Property<T>(this HttpClient httpClient, PropertyKey<T> key, T? value) where T: notnull {
        _ = UnfuckedHttpHandler.FindHandler(httpClient)?.Property(key, value) ?? throw UnfuckedWebTarget.ConfigUnavailable;
        return httpClient;
    }

    [Pure]
    public static bool Property<T>(this HttpClient httpClient, PropertyKey<T> key, out T? existingValue) where T: notnull =>
        UnfuckedHttpHandler.FindHandler(httpClient) is { } handler ? handler.Property(key, out existingValue) : throw UnfuckedWebTarget.ConfigUnavailable;

}