﻿using System.Collections.Immutable;
using System.Diagnostics.Contracts;
using Unfucked.HTTP.Filters;
using Unfucked.HTTP.Serialization;

namespace Unfucked.HTTP.Config;

public interface IClientConfig: Configurable<IClientConfig>, ICloneable;

public class ClientConfig: IClientConfig {

    public const int FirstFilterPosition = 0;
    public const int LastFilterPosition  = int.MaxValue;

    private ImmutableList<ClientRequestFilter> ReqFilters { get; init; }
    private ImmutableList<ClientResponseFilter> ResFilters { get; init; }
    private ImmutableHashSet<MessageBodyReader> Readers { get; init; }
    private ImmutableDictionary<PropertyKey, object> Properties { get; init; }

    [Pure]
    public IReadOnlyList<ClientRequestFilter> RequestFilters => ReqFilters.AsReadOnly();

    [Pure]
    public IReadOnlyList<ClientResponseFilter> ResponseFilters => ResFilters.AsReadOnly();

    [Pure]
    public IEnumerable<MessageBodyReader> MessageBodyReaders => Readers.AsEnumerable();

    internal ClientConfig() {
        ReqFilters = ImmutableList<ClientRequestFilter>.Empty;
        ResFilters = ImmutableList<ClientResponseFilter>.Empty;
        Readers    = ImmutableHashSet<MessageBodyReader>.Empty;
        Properties = ImmutableDictionary<PropertyKey, object>.Empty;
    }

    private ClientConfig(ClientConfig other) {
        ReqFilters = other.ReqFilters;
        ResFilters = other.ResFilters;
        Readers    = other.Readers;
        Properties = other.Properties;
    }

    [Pure]
    public object Clone() => new ClientConfig(this);

    [Pure]
    public IClientConfig Register(ClientRequestFilter? filter, int position = LastFilterPosition) => new ClientConfig(this) { ReqFilters = Register(ReqFilters, filter, position) };

    [Pure]
    public IClientConfig Register(ClientResponseFilter? filter, int position = LastFilterPosition) => new ClientConfig(this) { ResFilters = Register(ResFilters, filter, position) };

    [Pure]
    public IClientConfig Register(MessageBodyReader reader) => new ClientConfig(this) { Readers = Readers.Add(reader) };

    private static ImmutableList<T> Register<T>(ImmutableList<T> list, T? newItem, int position) {
        int  oldCount  = list.Count;
        bool isRemoval = newItem is null;
        position = position switch {
            < FirstFilterPosition                    => 0,
            _ when position >= oldCount && isRemoval => oldCount - 1,
            _ when position > oldCount               => oldCount,
            _                                        => position
        };
        return isRemoval ? list.RemoveAt(position) : list.Insert(position, newItem!);
    }

    [Pure]
    public IClientConfig Property<T>(PropertyKey<T> key, T? value) where T: notnull =>
        new ClientConfig(this) { Properties = value is not null ? Properties.SetItem(key, value) : Properties.Remove(key) };

    [Pure]
    public bool Property<T>(PropertyKey<T> key,
#if !NETSTANDARD2_0
                            [NotNullWhen(true)]
#endif
                            out T? existingValue) where T: notnull {
        bool exists = Properties.TryGetValue(key, out object? rawValue);
        existingValue = exists ? (T?) rawValue : default;
        return exists;
    }

}