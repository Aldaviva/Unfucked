using System.Collections.Concurrent;
using System.Net.Http.Headers;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using Unfucked.HTTP.Config;
using ConstructorMethod =
#if NET8_0_OR_GREATER
    System.Reflection.ConstructorInvoker
#else
    System.Reflection.ConstructorInfo
#endif
    ;

namespace Unfucked.HTTP.Serialization;

/// <summary>Can parse types like <see cref="Version"/> or <see cref="Uri"/> from strings</summary>
public sealed class GenericBodyReader(StringBodyReader stringBodyReader): AbstractMessageBodyReader {

    private static readonly Lazy<ConcurrentDictionary<Type, ConstructorMethod?>> CONSTRUCTOR_CACHE     = new(LazyThreadSafetyMode.PublicationOnly);
    private static readonly Lazy<ConcurrentDictionary<Type, object?>>            PARSE_METHOD_CACHE    = new(LazyThreadSafetyMode.PublicationOnly); // type of value is ParseString<T>?
    private static readonly Lazy<ConcurrentDictionary<Type, object?>>            TRYPARSE_METHOD_CACHE = new(LazyThreadSafetyMode.PublicationOnly); // type of value is TryParseString<T>?

#if NET8_0_OR_GREATER
    private delegate T ParseUtf8Span<out T>(ReadOnlySpan<byte> serialized, IFormatProvider? formatProvider);

    private static readonly Lazy<ConcurrentDictionary<Type, object?>> UTF8_SPAN_PARSER_CACHE = new(LazyThreadSafetyMode.PublicationOnly); // type of value is ParseUtf8Span<T>
#endif

#if NET7_0_OR_GREATER
    private delegate T ParseStringWithFormat<out T>(string serialized, IFormatProvider? formatProvider);

    private static readonly Lazy<ConcurrentDictionary<Type, object?>> STRING_PARSER_CACHE = new(LazyThreadSafetyMode.PublicationOnly); // type of value is ParseStringWithFormat<T>?
#endif

    private delegate T ParseString<out T>(string serialized);

    private delegate bool TryParseString<T>(string serialized, out T? parsed);

    /// <inheritdoc />
    public override bool CanRead<T>(MediaTypeHeaderValue? contentType) =>
#if NET8_0_OR_GREATER
        ((contentType?.CharSet?.Replace("-", string.Empty).Equals(Encoding.UTF8.BodyName, StringComparison.OrdinalIgnoreCase) ?? true) &&
            UTF8_SPAN_PARSER_CACHE.Value.GetOrAdd(typeof(T), _ => FindUtf8SpanParser<T>()) != null) ||
#endif
#if NET7_0_OR_GREATER
        STRING_PARSER_CACHE.Value.GetOrAdd(typeof(T), _ => FindStringParser<T>()) != null ||
#endif
        CONSTRUCTOR_CACHE.Value.GetOrAdd(typeof(T), FindConstructor) != null ||
        PARSE_METHOD_CACHE.Value.GetOrAdd(typeof(T), _ => FindParseMethod<T>()) != null ||
        TRYPARSE_METHOD_CACHE.Value.GetOrAdd(typeof(T), _ => FindTryParseMethod<T>()) != null;

    /// <inheritdoc />
    /// <exception cref="SerializationException"></exception>
    public override async Task<T> Read<T>(HttpResponseMessage response, Encoding? responseEncoding, Configurable? clientConfig, CancellationToken cancellationToken) {
#if NET8_0_OR_GREATER
        if (((responseEncoding?.Equals(Encoding.UTF8) ?? true) || responseEncoding.Equals(Encoding.ASCII)) &&
            UTF8_SPAN_PARSER_CACHE.Value.GetOrAdd(typeof(T), _ => FindUtf8SpanParser<T>()) is ParseUtf8Span<T> utf8SpanParser) {
            Stream       bodyStream = await GetBodyStream(response, cancellationToken);
            MemoryStream bodyBuffer = new();
            await bodyStream.CopyToAsync(bodyBuffer, int.MaxValue, cancellationToken).ConfigureAwait(false);
            ReadOnlySpan<byte> bodySpan = bodyBuffer.GetBuffer().AsSpan(0, (int) bodyBuffer.Length);
            return utf8SpanParser(bodySpan, null);
        }
#endif

#if NET7_0_OR_GREATER
        if (STRING_PARSER_CACHE.Value.GetOrAdd(typeof(T), _ => FindStringParser<T>()) is ParseStringWithFormat<T> stringParser) {
            string stringBody = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            return stringParser(stringBody, null);
        }
#endif

        if (CONSTRUCTOR_CACHE.Value.GetOrAdd(typeof(T), FindConstructor) is {} constructor) {
            string stringBody = await response.Content.ReadAsStringAsync(
#if NET5_0_OR_GREATER
                cancellationToken
#endif
            ).ConfigureAwait(false);

            return (T)
#if NET8_0_OR_GREATER
                constructor.Invoke(stringBody);
#else
                constructor.Invoke([stringBody]);
#endif
        }

        if (PARSE_METHOD_CACHE.Value.GetOrAdd(typeof(T), _ => FindParseMethod<T>()) is ParseString<T> parseMethod) {
            string stringBody = await response.Content.ReadAsStringAsync(
#if NET5_0_OR_GREATER
                cancellationToken
#endif
            ).ConfigureAwait(false);

            return parseMethod(stringBody);
        }

        if (TRYPARSE_METHOD_CACHE.Value.GetOrAdd(typeof(T), _ => FindTryParseMethod<T>()) is TryParseString<T> tryParseMethod) {
            string stringBody = await response.Content.ReadAsStringAsync(
#if NET5_0_OR_GREATER
                cancellationToken
#endif
            ).ConfigureAwait(false);

            return tryParseMethod(stringBody, out T? parsed) ? parsed! : throw new SerializationException($"{typeof(T).FullName}.TryParse failed to deserialize '{stringBody}'");
        }

        throw new SerializationException($"{typeof(T).FullName} can be deserialized with neither IUtf8SpanParsable.Parse nor IParsable.Parse nor new(string) nor static Parse(string)");
    }

#if NET8_0_OR_GREATER
    private static ParseUtf8Span<T>? FindUtf8SpanParser<T>() {
        Type type = typeof(T);
        try {
            type.IsAssignableTo(typeof(IUtf8SpanParsable<>).MakeGenericType(type));
        } catch (ArgumentException) {
            return null;
        }
        return type.GetMethod($"System.IUtf8SpanParsable<{type.FullName}>.Parse",
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic,
                [typeof(ReadOnlySpan<byte>), typeof(IFormatProvider)])?
            .CreateDelegate<ParseUtf8Span<T>>();
    }

#endif

#if NET7_0_OR_GREATER
    private static ParseStringWithFormat<T>? FindStringParser<T>() {
        Type type = typeof(T);
        try {
            type.IsAssignableTo(typeof(IParsable<>).MakeGenericType(type));
        } catch (ArgumentException) {
            return null;
        }
        return type.GetMethod($"System.IParsable<{type.FullName}>.Parse",
            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic,
            [typeof(string), typeof(IFormatProvider)])?.CreateDelegate<ParseStringWithFormat<T>>();
    }

#endif

    private static ConstructorMethod? FindConstructor(Type type) {
        var constructor = type.GetConstructor([typeof(string)]);
        return
#if NET8_0_OR_GREATER
            constructor != null ? ConstructorMethod.Create(constructor) : null;
#else
            constructor;
#endif
    }

    private static ParseString<T>? FindParseMethod<T>() =>
        typeof(T).GetMethod("Parse", BindingFlags.Static | BindingFlags.Public, null, [typeof(string)], null)?.CreateDelegate
#if NET5_0_OR_GREATER
            <ParseString<T>>();
#else
            (typeof(ParseString<T>)) as ParseString<T>;
#endif

    private static TryParseString<T>? FindTryParseMethod<T>() =>
        typeof(T).GetMethod("TryParse", BindingFlags.Static | BindingFlags.Public, null, [typeof(string), typeof(T).MakeByRefType()], null)?.CreateDelegate
#if NET5_0_OR_GREATER
            <TryParseString<T>>();
#else
            (typeof(TryParseString<T>)) as TryParseString<T>;
#endif

}