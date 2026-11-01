using System.Collections.Concurrent;
using System.Net.Http.Headers;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using Unfucked.HTTP.Config;

namespace Unfucked.HTTP.Serialization;

/// <summary>Can parse types like <see cref="Version"/> or <see cref="Uri"/> from strings</summary>
public sealed class GenericBodyReader: AbstractMessageBodyReader {

    private static readonly Lazy<ConcurrentDictionary<Type, ConstructorInfo?>> CONSTRUCTOR_CACHE     = new(LazyThreadSafetyMode.PublicationOnly);
    private static readonly Lazy<ConcurrentDictionary<Type, MethodInfo?>>      PARSE_METHOD_CACHE    = new(LazyThreadSafetyMode.PublicationOnly);
    private static readonly Lazy<ConcurrentDictionary<Type, MethodInfo?>>      TRYPARSE_METHOD_CACHE = new(LazyThreadSafetyMode.PublicationOnly);

#if NET8_0_OR_GREATER
    private delegate object ParseUtf8Span(ReadOnlySpan<byte> serialized, IFormatProvider? formatProvider);

    private static readonly Lazy<ConcurrentDictionary<Type, object?>> UTF8_SPAN_PARSER_CACHE = new(LazyThreadSafetyMode.PublicationOnly); // type of value is delegate ParseUtf8Span
#endif

#if NET7_0_OR_GREATER
    // private delegate T ParseStringWithFormat<out T>(string serialized, IFormatProvider? formatProvider);

    private static readonly Lazy<ConcurrentDictionary<Type, MethodInfo?>> STRING_PARSER_CACHE = new(LazyThreadSafetyMode.PublicationOnly);
#endif

    // private delegate T ParseString<out T>(string serialized);

    // private delegate bool TryParseString<T>(string serialized, out T? parsed);

    /// <inheritdoc />
    public override bool CanRead<T>(MediaTypeHeaderValue? contentType) =>
#if NET8_0_OR_GREATER
        ((contentType?.CharSet?.Replace("-", string.Empty).Equals(Encoding.UTF8.BodyName, StringComparison.OrdinalIgnoreCase) ?? true) &&
            UTF8_SPAN_PARSER_CACHE.Value.GetOrAdd(typeof(T), FindUtf8SpanParser) != null) ||
#endif
#if NET7_0_OR_GREATER
        STRING_PARSER_CACHE.Value.GetOrAdd(typeof(T), FindStringParser) != null ||
#endif
        CONSTRUCTOR_CACHE.Value.GetOrAdd(typeof(T), FindConstructor) != null ||
        PARSE_METHOD_CACHE.Value.GetOrAdd(typeof(T), FindParseMethod) != null ||
        TRYPARSE_METHOD_CACHE.Value.GetOrAdd(typeof(T), FindTryParseMethod) != null;

    /// <inheritdoc />
    /// <exception cref="SerializationException"></exception>
    public override async Task<T> Read<T>(HttpResponseMessage response, Encoding? responseEncoding, Configurable? clientConfig, CancellationToken cancellationToken) {
#if NET8_0_OR_GREATER
        if (((responseEncoding?.Equals(Encoding.UTF8) ?? true) || responseEncoding.Equals(Encoding.ASCII)) &&
            UTF8_SPAN_PARSER_CACHE.Value.GetOrAdd(typeof(T), FindUtf8SpanParser) is ParseUtf8Span utf8SpanParser) {
            Stream       bodyStream = await GetBodyStream(response, cancellationToken);
            MemoryStream bodyBuffer = new();
            await bodyStream.CopyToAsync(bodyBuffer, int.MaxValue, cancellationToken).ConfigureAwait(false);
            ReadOnlySpan<byte> bodySpan = bodyBuffer.GetBuffer().AsSpan(0, (int) bodyBuffer.Length);
            return (T) utf8SpanParser(bodySpan, null);
        }
#endif

#if NET7_0_OR_GREATER
        if (STRING_PARSER_CACHE.Value.GetOrAdd(typeof(T), FindStringParser) is {} stringParser) {
            string stringBody = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            return (T) stringParser.Invoke(null, [stringBody, null])!;
        }
#endif

        if (CONSTRUCTOR_CACHE.Value.GetOrAdd(typeof(T), FindConstructor) is {} constructor) {
            string stringBody = await getBodyString().ConfigureAwait(false);
            return (T) constructor.Invoke([stringBody]);
        }

        if (PARSE_METHOD_CACHE.Value.GetOrAdd(typeof(T), FindParseMethod) is {} parseMethod) {
            string stringBody = await getBodyString().ConfigureAwait(false);
            return (T) parseMethod.Invoke(null, [stringBody])!;
        }

        if (TRYPARSE_METHOD_CACHE.Value.GetOrAdd(typeof(T), FindTryParseMethod) is {} tryParseMethod) {
            string    stringBody = await getBodyString().ConfigureAwait(false);
            object?[] parameters = [stringBody, null];
            return (bool) tryParseMethod.Invoke(null, parameters)! ? (T) parameters[1]! : throw new SerializationException($"{typeof(T).FullName}.TryParse failed to deserialize '{stringBody}'");
        }

        throw new SerializationException(
            $"{typeof(T).FullName} can be deserialized with neither IUtf8SpanParsable.Parse nor IParsable.Parse nor new(string) nor static Parse(string) nor static TryParse(string, out T?)");

        Task<string> getBodyString() => response.Content.ReadAsStringAsync(
#if NET5_0_OR_GREATER
            cancellationToken
#endif
        );
    }

#if NET8_0_OR_GREATER
    private static ParseUtf8Span? FindUtf8SpanParser(Type type) {
        try {
            type.IsAssignableTo(typeof(IUtf8SpanParsable<>).MakeGenericType(type));
        } catch (ArgumentException) {
            return null;
        }
        return type.GetMethod($"System.IUtf8SpanParsable<{type.FullName}>.Parse",
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic,
                [typeof(ReadOnlySpan<byte>), typeof(IFormatProvider)])?
            .CreateDelegate<ParseUtf8Span>();
    }

#endif

#if NET7_0_OR_GREATER
    private static MethodInfo? FindStringParser(Type type) {
        try {
            type.IsAssignableTo(typeof(IParsable<>).MakeGenericType(type));
        } catch (ArgumentException) {
            return null;
        }
        return type.GetMethod($"System.IParsable<{type.FullName}>.Parse",
            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic,
            [typeof(string), typeof(IFormatProvider)]);
    }

#endif

    private static ConstructorInfo? FindConstructor(Type type) {
        return type.GetConstructor([typeof(string)]);
    }

    private static MethodInfo? FindParseMethod(Type type) =>
        type.GetMethod("Parse", BindingFlags.Static | BindingFlags.Public, null, [typeof(string)], null);

    private static MethodInfo? FindTryParseMethod(Type type) =>
        type.GetMethod("TryParse", BindingFlags.Static | BindingFlags.Public, null, [typeof(string), type.MakeByRefType()], null);

}