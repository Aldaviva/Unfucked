using System.Diagnostics.Contracts;
using Unfucked.HTTP.Config;
using Unfucked.HTTP.Filters;
using Unfucked.HTTP.Serialization;

namespace Unfucked.HTTP;

public partial class WebTarget {

    /// <inheritdoc />
    [Pure]
    public IReadOnlyList<ClientRequestFilter> RequestFilters => clientConfig?.RequestFilters ?? throw ConfigUnavailable;

    /// <inheritdoc />
    [Pure]
    public IReadOnlyList<ClientResponseFilter> ResponseFilters => clientConfig?.ResponseFilters ?? throw ConfigUnavailable;

    /// <inheritdoc />
    [Pure]
    public IEnumerable<MessageBodyReader> MessageBodyReaders => clientConfig?.MessageBodyReaders ?? throw ConfigUnavailable;

    /// <inheritdoc />
    [Pure]
    public WebTarget Register(Registrable registrable) =>
        clientConfig is not null ? new WebTarget(client, urlBuilder, clientHandler, clientConfig.Register(registrable)) : throw ConfigUnavailable;

    /// <inheritdoc />
    [Pure]
    public WebTarget Register<Option>(Registrable<Option> registrable, Option registrationOption) =>
        clientConfig is not null ? new WebTarget(client, urlBuilder, clientHandler, clientConfig.Register(registrable, registrationOption)) : throw ConfigUnavailable;

    /// <inheritdoc />
    [Pure]
    IWebTarget Configurable<IWebTarget>.Register(Registrable registrable) => Register(registrable);

    /// <inheritdoc />
    [Pure]
    IWebTarget Configurable<IWebTarget>.Register<Option>(Registrable<Option> registrable, Option registrationOption) => Register(registrable, registrationOption);

    /// <inheritdoc />
    [Pure]
    public WebTarget Property<T>(PropertyKey<T> key, T? newValue) where T: notnull =>
        clientConfig is not null ? new WebTarget(client, urlBuilder, clientHandler, clientConfig.Property(key, newValue)) : throw ConfigUnavailable;

    /// <inheritdoc />
    public bool Property<T>(PropertyKey<T> key,
#if !NETSTANDARD2_0
                            [NotNullWhen(true)]
#endif
                            out T? existingValue) where T: notnull {
        if (clientConfig is not null) {
            return clientConfig.Property(key, out existingValue);
        } else {
            existingValue = default;
            return false;
        }
    }

    /// <inheritdoc />
    [Pure]
    IWebTarget Configurable<IWebTarget>.Property<T>(PropertyKey<T> key, T? newValue) where T: default => Property(key, newValue);

    internal static InvalidOperationException ConfigUnavailable => new(
        $"Configuration is not available on this {nameof(WebTarget)} because the underlying {nameof(HttpClient)} was constructed with a default {nameof(HttpMessageHandler)}, or an {nameof(HttpMessageHandler)} that is neither an {nameof(UnfuckedHttpHandler)} nor a {nameof(DelegatingHandler)} that delegates to an inner {nameof(UnfuckedHttpHandler)}. Try instantiating the client by calling `new {nameof(UnfuckedHttpClient)}()` instead.");

}