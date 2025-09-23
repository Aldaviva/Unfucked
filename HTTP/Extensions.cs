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

    public static H Register<H>(this H httpClient, ClientRequestFilter filter, int position = ClientConfig.LastFilterPosition) where H: HttpClient {
        _ = UnfuckedHttpHandler.FindHandler(httpClient)?.Register(filter, position) ?? throw UnfuckedWebTarget.ConfigUnavailable;
        return httpClient;
    }

    public static H Register<H>(this H httpClient, ClientResponseFilter filter, int position = ClientConfig.LastFilterPosition) where H: HttpClient {
        _ = UnfuckedHttpHandler.FindHandler(httpClient)?.Register(filter, position) ?? throw UnfuckedWebTarget.ConfigUnavailable;
        return httpClient;
    }

    public static H Register<H>(this H httpClient, MessageBodyReader reader) where H: HttpClient {
        _ = UnfuckedHttpHandler.FindHandler(httpClient)?.Register(reader) ?? throw UnfuckedWebTarget.ConfigUnavailable;
        return httpClient;
    }

    public static H Register<H>(this H httpClient, Feature feature) where H: HttpClient {
        _ = UnfuckedHttpHandler.FindHandler(httpClient)?.Register(feature) ?? throw UnfuckedWebTarget.ConfigUnavailable;
        return httpClient;
    }

    public static H Property<H, T>(this H httpClient, PropertyKey<T> key, T? newValue) where H: HttpClient where T: notnull {
        _ = UnfuckedHttpHandler.FindHandler(httpClient)?.Property(key, newValue) ?? throw UnfuckedWebTarget.ConfigUnavailable;
        return httpClient;
    }

    [Pure]
    public static bool Property<T>(this HttpClient httpClient, PropertyKey<T> key, out T? existingValue) where T: notnull =>
        UnfuckedHttpHandler.FindHandler(httpClient) is { } handler ? handler.Property(key, out existingValue) : throw UnfuckedWebTarget.ConfigUnavailable;

    public static Task ThrowIfUnsuccessful(this HttpResponseMessage response, CancellationToken cancellationToken = default) => UnfuckedWebTarget.ThrowIfUnsuccessful(response, cancellationToken);

}