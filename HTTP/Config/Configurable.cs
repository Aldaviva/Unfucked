using System.Diagnostics.Contracts;
using Unfucked.HTTP.Filters;
using Unfucked.HTTP.Serialization;

namespace Unfucked.HTTP.Config;

public interface Configurable {

    IReadOnlyList<ClientRequestFilter> RequestFilters { get; }
    IReadOnlyList<ClientResponseFilter> ResponseFilters { get; }
    IEnumerable<MessageBodyReader> MessageBodyReaders { get; }

    [Pure]
    bool Property<T>(PropertyKey<T> key,
#if !NETSTANDARD2_0
                     [NotNullWhen(true)]
#endif
                     out T? existingValue) where T: notnull;

}

public interface Configurable<out TContainer>: Configurable {

    [Pure]
    TContainer Register(ClientRequestFilter? filter, int position = ClientConfig.LastFilterPosition);

    [Pure]
    TContainer Register(ClientResponseFilter? filter, int position = ClientConfig.LastFilterPosition);

    [Pure]
    TContainer Register(MessageBodyReader reader);

    [Pure]
    TContainer Property<T>(PropertyKey<T> key, T? value) where T: notnull;

}