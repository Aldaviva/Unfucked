using System.Diagnostics.Contracts;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using Unfucked.HTTP.Filters;
using Unfucked.HTTP.Serialization;

namespace Unfucked.HTTP.Config;

public interface Configurable {

    /// <summary>
    /// Interceptors that run before an HTTP client request is sent. Empty by default.
    /// </summary>
    IReadOnlyList<ClientRequestFilter> RequestFilters { get; }

    /// <summary>
    /// Interceptors that run after an HTTP client response is received. Empty by default.
    /// </summary>
    IReadOnlyList<ClientResponseFilter> ResponseFilters { get; }

    /// <summary>
    /// <para>Marshallers that deserialize HTTP client response bodies from byte streams into types, such as classes that are mapped from JSON objects.</para>
    /// <para>By default, these can read <see cref="string"/>, <see cref="Stream"/>, <see cref="byte"/><c>[]</c>, JSON (both mapped to types, and unmapped like <see cref="JsonDocument"/> and <see cref="JsonNode"/>), and XML (including mapped to types, DOM <see cref="XmlDocument"/>, LINQ <see cref="XDocument"/>, and XPath <see cref="XPathNavigator"/>)</para>
    /// </summary>
    IEnumerable<MessageBodyReader> MessageBodyReaders { get; }

    /// <summary>
    /// Get a property value used to configure HTTP requests and responses.
    /// </summary>
    /// <typeparam name="T">Type of the property value</typeparam>
    /// <param name="key">See <see cref="PropertyKey"/> for built-in keys, such as <see cref="PropertyKey.JsonSerializerOptions"/>.</param>
    /// <param name="existingValue">This will be set to the value of the property which from this client or target, or <c>default</c> if it has not been set and the return value is <c>false</c></param>
    /// <returns><c>true</c> if the property exists in the configuration, or <c>false</c> if it has not been set</returns>
    [Pure]
    bool Property<T>(PropertyKey<T> key,
#if !NETSTANDARD2_0
                     [NotNullWhen(true)]
#endif
                     out T? existingValue) where T: notnull;

}

public interface Configurable<out TContainer>: Configurable {

    [Pure]
    TContainer Register(Registrable registrable);

    [Pure]
    TContainer Register<Option>(Registrable<Option> registrable, Option registrationOption);

    /// <summary>
    /// Set a property value used to configure HTTP requests and responses.
    /// </summary>
    /// <typeparam name="T">Type of the property value</typeparam>
    /// <param name="key">See <see cref="PropertyKey"/> for built-in keys, such as <see cref="PropertyKey.JsonSerializerOptions"/>.</param>
    /// <param name="value">The new value you want to set in the client or target configuration, or <c>null</c> to unset the property</param>
    /// <returns>The updated subject to use in the future, which may be a different immutable instance than the original <c>this</c> subject on which this method was called.</returns>
    [Pure]
    TContainer Property<T>(PropertyKey<T> key, T? value) where T: notnull;

}