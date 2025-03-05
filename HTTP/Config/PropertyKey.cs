using System.Text.Json;

namespace Unfucked.HTTP.Config;

public class PropertyKey(string id) {

    private static readonly string Namespace = typeof(UnfuckedHttpClient).Namespace!;

    public static PropertyKey<JsonSerializerOptions> JsonSerializerOptions { get; } = new($"{Namespace}.{nameof(JsonSerializerOptions)}");
    public static PropertyKey<bool> ThrowOnUnsuccessfulStatusCode { get; } = new($"{Namespace}.{nameof(ThrowOnUnsuccessfulStatusCode)}");

    private readonly string id = id;

    protected bool Equals(PropertyKey other) => id == other.id;
    public override bool Equals(object? obj) => ReferenceEquals(this, obj) || (obj is PropertyKey other && Equals(other));

    public override int GetHashCode() => id.GetHashCode();

}

// ReSharper disable once UnusedTypeParameter - it's used for parameter constraints in HttpConfiguration
public class PropertyKey<T>(string id): PropertyKey(id);