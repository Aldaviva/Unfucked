﻿using System.Diagnostics.Contracts;
using Unfucked.HTTP.Config;
using Unfucked.HTTP.Filters;
using Unfucked.HTTP.Serialization;

namespace Unfucked.HTTP;

public partial class UnfuckedWebTarget {

    [Pure]
    public IReadOnlyList<ClientRequestFilter> RequestFilters => clientConfig?.RequestFilters ?? throw ConfigUnavailable;

    [Pure]
    public IReadOnlyList<ClientResponseFilter> ResponseFilters => clientConfig?.ResponseFilters ?? throw ConfigUnavailable;

    [Pure]
    public IEnumerable<MessageBodyReader> MessageBodyReaders => clientConfig?.MessageBodyReaders ?? throw ConfigUnavailable;

    [Pure]
    public UnfuckedWebTarget Register(ClientRequestFilter? filter, int position = ClientConfig.LastFilterPosition) =>
        clientConfig is not null ? new UnfuckedWebTarget(client, urlBuilder, clientHandler, clientConfig.Register(filter, position)) : throw ConfigUnavailable;

    [Pure]
    public UnfuckedWebTarget Register(ClientResponseFilter? filter, int position = ClientConfig.LastFilterPosition) =>
        clientConfig is not null ? new UnfuckedWebTarget(client, urlBuilder, clientHandler, clientConfig.Register(filter, position)) : throw ConfigUnavailable;

    [Pure]
    public UnfuckedWebTarget Register(MessageBodyReader reader) =>
        clientConfig is not null ? new UnfuckedWebTarget(client, urlBuilder, clientHandler, clientConfig.Register(reader)) : throw ConfigUnavailable;

    [Pure]
    WebTarget Configurable<WebTarget>.Register(ClientRequestFilter? filter, int position) => Register(filter, position);

    [Pure]
    WebTarget Configurable<WebTarget>.Register(ClientResponseFilter? filter, int position) => Register(filter, position);

    [Pure]
    WebTarget Configurable<WebTarget>.Register(MessageBodyReader reader) => Register(reader);

    [Pure]
    public UnfuckedWebTarget Property<T>(PropertyKey<T> key, T? value) where T: notnull =>
        clientConfig is not null ? new UnfuckedWebTarget(client, urlBuilder, clientHandler, clientConfig.Property(key, value)) : throw ConfigUnavailable;

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

    [Pure]
    WebTarget Configurable<WebTarget>.Property<T>(PropertyKey<T> key, T? value) where T: default => Property(key, value);

    internal static InvalidOperationException ConfigUnavailable => new(
        $"Configuration is not available on this {nameof(UnfuckedWebTarget)} because the underlying {nameof(HttpClient)} was constructed with a default {nameof(HttpMessageHandler)}, or an {nameof(HttpMessageHandler)} that is neither a {nameof(UnfuckedHttpHandler)} nor a {nameof(DelegatingHandler)} that delegates to an inner {nameof(UnfuckedHttpHandler)}. Try instantiating it by calling `new {nameof(HttpClient)}(new {nameof(UnfuckedHttpHandler)}())` instead.");

}