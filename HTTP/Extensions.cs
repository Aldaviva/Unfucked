using System.Diagnostics.Contracts;
using Unfucked.HTTP.Config;
using Unfucked.HTTP.Exceptions;
using Unfucked.HTTP.Filters;
using Unfucked.HTTP.Serialization;

namespace Unfucked.HTTP;

public static class Extensions {

    /// <summary>
    /// Begin building an HTTP request for a given URL
    /// </summary>
    /// <param name="httpClient">HTTP client</param>
    /// <param name="uri">URL to send request to</param>
    /// <returns>web target, on which you can call builder methods to set up the request, ending in one of the verb methods like <c>Get</c> to send the built request</returns>
    [Pure]
    public static WebTarget Target(this HttpClient httpClient, Uri uri) => new UnfuckedWebTarget(httpClient, uri);

    /// <inheritdoc cref="Target(System.Net.Http.HttpClient,System.Uri)" />
    [Pure]
    public static WebTarget Target(this HttpClient httpClient, string uri) => new UnfuckedWebTarget(httpClient, uri);

    /// <summary>
    /// Begin building an HTTP request for a given URL
    /// </summary>
    /// <param name="httpClient">HTTP client</param>
    /// <param name="urlBuilder">URL to send request to</param>
    /// <returns>web target, on which you can call builder methods to set up the request, ending in one of the verb methods like <c>Get</c> to send the built request</returns>
    [Pure]
    public static WebTarget Target(this HttpClient httpClient, UrlBuilder urlBuilder) => new UnfuckedWebTarget(httpClient, urlBuilder);

    /// <summary>
    /// Begin building an HTTP request for a given URL
    /// </summary>
    /// <param name="httpClient">HTTP client</param>
    /// <param name="uriBuilder">URL to send request to</param>
    /// <returns>web target, on which you can call builder methods to set up the request, ending in one of the verb methods like <c>Get</c> to send the built request</returns>
    [Pure]
    public static WebTarget Target(this HttpClient httpClient, UriBuilder uriBuilder) => new UnfuckedWebTarget(httpClient, uriBuilder);

    /// <exception cref="InvalidOperationException">the <paramref name="httpClient"/> does not have a usable configuration because of how it was constructed</exception>
    public static H Register<H>(this H httpClient, ClientRequestFilter filter, int position = ClientConfig.LastFilterPosition) where H: HttpClient {
        _ = UnfuckedHttpHandler.FindHandler(httpClient)?.Register(filter, position) ?? throw UnfuckedWebTarget.ConfigUnavailable;
        return httpClient;
    }

    /// <exception cref="InvalidOperationException">the <paramref name="httpClient"/> does not have a usable configuration because of how it was constructed</exception>
    public static H Register<H>(this H httpClient, ClientResponseFilter filter, int position = ClientConfig.LastFilterPosition) where H: HttpClient {
        _ = UnfuckedHttpHandler.FindHandler(httpClient)?.Register(filter, position) ?? throw UnfuckedWebTarget.ConfigUnavailable;
        return httpClient;
    }

    /// <exception cref="InvalidOperationException">the <paramref name="httpClient"/> does not have a usable configuration because of how it was constructed</exception>
    public static H Register<H>(this H httpClient, MessageBodyReader reader) where H: HttpClient {
        _ = UnfuckedHttpHandler.FindHandler(httpClient)?.Register(reader) ?? throw UnfuckedWebTarget.ConfigUnavailable;
        return httpClient;
    }

    /// <exception cref="InvalidOperationException">the <paramref name="httpClient"/> does not have a usable configuration because of how it was constructed</exception>
    public static H Property<H, T>(this H httpClient, PropertyKey<T> key, T? newValue) where H: HttpClient where T: notnull {
        _ = UnfuckedHttpHandler.FindHandler(httpClient)?.Property(key, newValue) ?? throw UnfuckedWebTarget.ConfigUnavailable;
        return httpClient;
    }

    /// <exception cref="InvalidOperationException">the <paramref name="httpClient"/> does not have a usable configuration because of how it was constructed</exception>
    [Pure]
    public static bool Property<T>(this HttpClient httpClient, PropertyKey<T> key, out T? existingValue) where T: notnull =>
        UnfuckedHttpHandler.FindHandler(httpClient) is { } handler ? handler.Property(key, out existingValue) : throw UnfuckedWebTarget.ConfigUnavailable;

    /// <summary>
    /// <para>Immediately throw a <see cref="WebApplicationException"/> if this HTTP response did not have a successful status code.</para>
    /// <para>This is a per-request, imperative alternative to leaving <see cref="PropertyKey.ThrowOnUnsuccessfulStatusCode"/> set to <c>true</c> on your <see cref="IClientConfig"/>, <see cref="IUnfuckedHttpHandler"/>, <see cref="HttpClient"/>, or <see cref="WebTarget"/>, which applies to all requests sent. Since that property defaults to <c>true</c>, calling this method is only useful if you manually changed that property to <c>false</c>.</para>
    /// </summary>
    /// <param name="response">completed HTTP response</param>
    /// <param name="cancellationToken">cancel reading response body</param>
    /// <returns>a Task that resolves with no return value if if the response status code is successful</returns>
    /// <exception cref="WebApplicationException">the response status code was not successful</exception>
    public static Task ThrowIfUnsuccessful(this HttpResponseMessage response, CancellationToken cancellationToken = default) => UnfuckedWebTarget.ThrowIfUnsuccessful(response, cancellationToken);

}