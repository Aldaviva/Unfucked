using System.Text.Json;
using Unfucked.HTTP.Exceptions;

namespace Unfucked.HTTP.Config;

public class PropertyKey(string id) {

    private static readonly string Namespace = typeof(UnfuckedHttpClient).Namespace!;

    #region Well-known properties

    /// <summary>
    /// Specify custom options for serializing and deserializing HTTP request and response bodies to and from JSON using <c>System.Text.Json</c>. By default, this uses the <see cref="JsonSerializerDefaults.Web"/> options and tolerates comments.
    /// </summary>
    public static PropertyKey<JsonSerializerOptions> JsonSerializerOptions { get; } = new($"{Namespace}.{nameof(JsonSerializerOptions)}");

    /// <summary>
    /// Whether an HTTP response status code greater than 299 should throw a <see cref="WebApplicationException"/> (<c>true</c>, default), or proceed with handling the response body and treat the response as successful (<c>false</c>)
    /// </summary>
    public static PropertyKey<bool> ThrowOnUnsuccessfulStatusCode { get; } = new($"{Namespace}.{nameof(ThrowOnUnsuccessfulStatusCode)}");

    #endregion

    private readonly string id = id;

    protected bool Equals(PropertyKey other) => id == other.id;
    public override bool Equals(object? obj) => ReferenceEquals(this, obj) || (obj is PropertyKey other && Equals(other));

    public override int GetHashCode() => id.GetHashCode();

}

// ReSharper disable once UnusedTypeParameter - it's used for parameter constraints in HttpConfiguration
public class PropertyKey<T>(string id): PropertyKey(id);