#if NET6_0_OR_GREATER
using System.Text.Json;
#endif

namespace Unfucked.HTTP;

public class PropertyKey {

#if NET6_0_OR_GREATER
    public static PropertyKey<JsonSerializerOptions> JsonSerializerOptions { get; } = new();
#endif

    public static PropertyKey<bool> ThrowOnUnsuccessfulStatusCode { get; } = new();

}

// ReSharper disable once UnusedTypeParameter - it's used for parameter constraints in HttpConfiguration
public class PropertyKey<T>: PropertyKey {

    // public Type ValueType => typeof(T);

}