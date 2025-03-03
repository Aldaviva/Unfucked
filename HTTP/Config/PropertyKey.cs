using System.Text.Json;

namespace Unfucked.HTTP.Config;

public class PropertyKey {

    public static PropertyKey<JsonSerializerOptions> JsonSerializerOptions { get; } = new();
    public static PropertyKey<bool> ThrowOnUnsuccessfulStatusCode { get; } = new();

}

// ReSharper disable once UnusedTypeParameter - it's used for parameter constraints in HttpConfiguration
public class PropertyKey<T>: PropertyKey;