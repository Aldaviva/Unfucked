using System.Text.Json;
using Unfucked.HTTP.Exceptions;

namespace Unfucked.HTTP.Config;

public class PropertyKey(string id) {

    private static readonly string NAMESPACE = typeof(UnfuckedHttpClient).Namespace!;

    #region Well-known properties

    /// <summary>
    /// Specify custom options for serializing and deserializing HTTP request and response bodies to and from JSON using <c>System.Text.Json</c>. By default, this uses the <see cref="JsonSerializerDefaults.Web"/> options and tolerates comments.
    /// </summary>
    public static PropertyKey<JsonSerializerOptions> JsonSerializerOptions { get; } = new($"{NAMESPACE}.{nameof(JsonSerializerOptions)}");

    /// <summary>
    /// Whether an HTTP response status code greater than or equal to 300 should throw a <see cref="WebApplicationException"/> (<c>true</c>, default), or proceed with handling the response body and treat the response as successful (<c>false</c>).
    /// </summary>
    public static PropertyKey<bool> ThrowOnUnsuccessfulStatusCode { get; } = new($"{NAMESPACE}.{nameof(ThrowOnUnsuccessfulStatusCode)}");

    /// <summary>Whether the client should transparently send a new request to the <c>Location</c> destination of a response with a status code in the range [300,400). Defaults to <c>true</c> to follow redirections up to <see cref="HttpClientHandler.MaxAutomaticRedirections"/> times per request. When <c>false</c>, attempting to deserialize the response will throw a <see cref="RedirectionException"/>, or just return the <see cref="HttpResponseMessage"/> when not deserializing.</summary>
    public static PropertyKey<bool> AutomaticallyFollowRedirections { get; } = new($"{NAMESPACE}.{nameof(AutomaticallyFollowRedirections)}");

    /// <summary>How long to wait before canceling a request. Defaults to 30 seconds. Can be set to a positive <see cref="TimeSpan"/> or <see cref="System.Threading.Timeout.InfiniteTimeSpan"/>. Like <see cref="HttpClient.Timeout"/>, except this is customizable on a per-request basis, even if the client has already sent a request.</summary>
    public static PropertyKey<TimeSpan> Timeout { get; } = new($"{NAMESPACE}.{nameof(Timeout)}");

    #endregion

    private readonly string id = id;

    protected bool Equals(PropertyKey other) => id == other.id;
    public override bool Equals(object? obj) => ReferenceEquals(this, obj) || (obj is PropertyKey other && Equals(other));

    public override int GetHashCode() => id.GetHashCode();

}

// ReSharper disable once UnusedTypeParameter - it's used for parameter constraints in Configurable
public sealed class PropertyKey<T>(string id): PropertyKey(id);