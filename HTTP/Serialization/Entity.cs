using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace Unfucked.HTTP.Serialization;

/// <summary>
/// <para>Easily generate request bodies for JSON and XML payloads.</para>
/// <para>JSON serialization is better than <see cref="JsonContent"/> because it respects the default <see cref="JsonSerializerOptions"/> from the <see cref="UnfuckedHttpClient"/>, which intelligently serializes enums and doesn't crash on comments.</para>
/// <para>XML serialization handles converting various XML document types (DOM, LINQ, and mapped objects) to streams for you automatically.</para>
/// </summary>
public static class Entity {

    internal static readonly UTF8Encoding UTF8 = new(false, true);

    /// <summary>
    /// Like <see cref="JsonContent.Create{T}(T,MediaTypeHeaderValue,JsonSerializerOptions)"/>, but it respects the default <see cref="JsonSerializerOptions"/> from the <see cref="UnfuckedHttpClient"/>, which intelligently serializes enums and doesn't crash on comments.
    /// </summary>
    /// <typeparam name="T">CLR type of <paramref name="json"/>.</typeparam>
    /// <param name="json">Value to serialize.</param>
    /// <param name="mediaType">Content/MIME type of request, defaults to <c>application/json;charset=utf-8</c>.</param>
    /// <param name="options">Customize JSON serialization. By default, this uses case-insensitive camelCase property names, lower camelCase enum values, numbers that are encoded as small as possible with optional stringification, and comments are ignored.</param>
    /// <returns>Request body ready to be passed to <see cref="IWebTarget.Post"/> or other request sending methods.</returns>
#if NET7_0_OR_GREATER
    [RequiresDynamicCode("JSON serialization"), RequiresUnreferencedCode("JSON serialization")]
#endif
    public static HttpContent Json<T>(T? json, MediaTypeHeaderValue? mediaType = null, JsonSerializerOptions? options = null) => Json(json, typeof(T), mediaType, options);

    /// <summary>
    /// Like <see cref="JsonContent.Create{T}(T,MediaTypeHeaderValue,JsonSerializerOptions)"/>, but it respects the default <see cref="JsonSerializerOptions"/> from the <see cref="UnfuckedHttpClient"/>, which intelligently serializes enums and doesn't crash on comments.
    /// </summary>
    /// <param name="json">Value to serialize.</param>
    /// <param name="jsonType">CLR type of <paramref name="json"/>.</param>
    /// <param name="mediaType">Content/MIME type of request, defaults to <c>application/json;charset=utf-8</c>.</param>
    /// <param name="options">Customize JSON serialization. By default, this uses case-insensitive camelCase property names, lower camelCase enum values, numbers that are encoded as small as possible with optional stringification, and comments are ignored.</param>
    /// <returns>Request body ready to be passed to <see cref="IWebTarget.Post"/> or other request sending methods.</returns>
#if NET7_0_OR_GREATER
    [RequiresDynamicCode("JSON serialization"), RequiresUnreferencedCode("JSON serialization")]
#endif
    public static HttpContent Json(object? json, Type jsonType, MediaTypeHeaderValue? mediaType = null, JsonSerializerOptions? options = null) =>
        new JsonHttpContent(json, jsonType, mediaType, options);

    /// <summary>
    /// Convert an XML document to an XML HTTP request body.
    /// </summary>
    /// <param name="xml">XML document.</param>
    /// <returns>Request body ready to be passed to <see cref="IWebTarget.Post"/> or other request sending methods.</returns>
    public static HttpContent Xml(XDocument xml) => XmlHttpContent.Create(xml);

    /// <inheritdoc cref="Xml(System.Xml.Linq.XDocument)" />
    public static HttpContent Xml(XmlDocument xml) => XmlHttpContent.Create(xml);

    /// <summary>
    /// Convert an object to an XML HTTP request body.
    /// </summary>
    /// <param name="xml">Object to serialize as an XML document.</param>
    /// <param name="xmlType">CLR type of <paramref name="xml"/>.</param>
    /// <returns>Request body ready to be passed to <see cref="IWebTarget.Post"/> or other request sending methods.</returns>
    public static HttpContent Xml(object xml, Type xmlType) => XmlHttpContent.Create(xml, xmlType);

    /// <summary>
    /// Convert an object to an XML HTTP request body.
    /// </summary>
    /// <typeparam name="T"> name="xmlType">CLR type of <paramref name="xml"/>.</typeparam>
    /// <param name="xml">Object to serialize as an XML document.</param>
    /// <returns>Request body ready to be passed to <see cref="IWebTarget.Post"/> or other request sending methods.</returns>
    public static HttpContent Xml<T>(T xml) => XmlHttpContent.Create(xml);

    internal static Encoding? ParseEncoding(string? encoding) {
        if (encoding is not null) {
            try {
                return Encoding.GetEncoding(encoding);
            } catch (ArgumentException) {}
        }
        return null;
    }

    internal sealed class JsonHttpContent: HttpContent {

        private readonly Lazy<HttpContent?>     inner;
        private readonly object?                json;
        private readonly Type                   jsonType;
        private readonly JsonSerializerOptions? userOptions;

        public JsonSerializerOptions? ClientOptions { get; set; }

        private static bool hasJsonContent = true;

        public JsonHttpContent(object? json, Type jsonType, MediaTypeHeaderValue? mediaType, JsonSerializerOptions? userOptions) {
            this.json        = json;
            this.jsonType    = jsonType;
            this.userOptions = userOptions;

            inner = new Lazy<HttpContent?>(() => {
                if (!hasJsonContent) return null;

                HttpContent jsonContent;
                try {
                    jsonContent = JsonContentDelegate.Create(json, jsonType, mediaType, userOptions ?? ClientOptions);
                } catch (FileNotFoundException) {
                    hasJsonContent = false;
                    return null;
                }

                foreach (KeyValuePair<string, IEnumerable<string>> header in jsonContent.Headers) {
                    Headers.TryAddWithoutValidation(header.Key, header.Value);
                }
                return jsonContent;
            }, LazyThreadSafetyMode.ExecutionAndPublication);
        }

        protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context) => SerializeToStreamAsyncInner(stream, context, CancellationToken.None);

#if NET5_0_OR_GREATER
        protected override void SerializeToStream(Stream stream, TransportContext? context, CancellationToken cancellationToken) {
            if (inner.Value is not null) {
                inner.Value.CopyTo(stream, context, cancellationToken);
            } else {
                JsonSerializer.Serialize(stream, json, jsonType, userOptions ?? ClientOptions);
            }
        }

        protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context, CancellationToken cancellationToken) =>
            SerializeToStreamAsyncInner(stream, context, cancellationToken);
#endif

        private async Task SerializeToStreamAsyncInner(Stream stream, TransportContext? context, CancellationToken cancellationToken) {
            if (inner.Value is not null) {
#if NET5_0_OR_GREATER
                await inner.Value.CopyToAsync(stream, context, cancellationToken).ConfigureAwait(false);
#else
                await inner.Value.CopyToAsync(stream, context).ConfigureAwait(false);
#endif
            } else {
                await JsonSerializer.SerializeAsync(stream, json, jsonType, userOptions ?? ClientOptions, cancellationToken).ConfigureAwait(false);
            }
        }

        protected override bool TryComputeLength(out long length) {
            length = inner.Value?.Headers.ContentLength ?? 0;
            return length != 0;
        }

    }

    internal sealed class XmlHttpContent: HttpContent {

        private XDocument?                 xDocument;
        private XmlDocument?               xmlDocument;
        private (object value, Type type)? mappable;

        public static XmlHttpContent Create(XDocument xDocument) {
            XmlHttpContent result = new();
            result.xDocument = xDocument;
            return result;
        }

        public static XmlHttpContent Create(XmlDocument xmlDocument) {
            XmlHttpContent result = new();
            result.xmlDocument = xmlDocument;
            return result;
        }

        public static XmlHttpContent Create(object mappable, Type type) {
            XmlHttpContent result = new();
            result.mappable = (mappable, type);
            return result;
        }

        public static XmlHttpContent Create<T>(T mappable) {
            XmlHttpContent result = new();
            result.mappable = (mappable!, typeof(T));
            return result;
        }

        protected override bool TryComputeLength(out long length) {
            length = 0;
            return false;
        }

#if NET5_0_OR_GREATER
        protected override void SerializeToStream(Stream stream, TransportContext? context, CancellationToken cancellationToken) {
            XmlWriter? xmlWriter = null;
            try {
                if (xDocument is not null) {
                    xmlWriter = createWriter(ParseEncoding(xDocument.Declaration?.Encoding));
                    xDocument.Save(xmlWriter);
                } else if (xmlDocument is not null) {
                    xmlWriter = createWriter(ParseEncoding(xmlDocument.ChildNodes.OfType<XmlDeclaration>().FirstOrDefault()?.Encoding));
                    xmlDocument.Save(xmlWriter);
                } else if (mappable is not null) {
                    xmlWriter = createWriter(null);
                    XmlSerializer serializer = new(mappable.Value.type);
                    serializer.Serialize(xmlWriter, mappable.Value.value);
                }
            } finally {
                xmlWriter?.Dispose();
            }

            XmlWriter createWriter(Encoding? encodingFallback) => XmlWriter.Create(stream, new XmlWriterSettings {
                Encoding = this.Encoding ?? encodingFallback ?? UTF8
            });
        }

        protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context, CancellationToken cancellationToken) =>
            SerializeToStreamAsyncInner(stream, context, cancellationToken);
#endif

        protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context) => SerializeToStreamAsyncInner(stream, context, CancellationToken.None);

        private async Task SerializeToStreamAsyncInner(Stream stream, TransportContext? context, CancellationToken cancellationToken) {
            XmlWriter? xmlWriter = null;
            try {
                if (xDocument is not null) {
                    xmlWriter = createWriter(ParseEncoding(xDocument.Declaration?.Encoding), true);
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP2_0_OR_GREATER
                    await xDocument.SaveAsync(xmlWriter, cancellationToken).ConfigureAwait(false);
#else
                    xDocument.Save(xmlWriter);
#endif
                } else if (xmlDocument is not null) {
                    xmlWriter = createWriter(ParseEncoding(xmlDocument.ChildNodes.OfType<XmlDeclaration>().FirstOrDefault()?.Encoding), false);
                    xmlDocument.Save(xmlWriter);
                } else if (mappable is not null) {
                    xmlWriter = createWriter(null, false);
                    XmlSerializer serializer = new(mappable.Value.type);
                    serializer.Serialize(xmlWriter, mappable.Value.value);
                }
            } finally {
#if NET5_0_OR_GREATER
                if (xmlWriter is not null) {
                    await xmlWriter.DisposeAsync().ConfigureAwait(false);
                }
#else
                xmlWriter?.Dispose();
#endif
            }

            XmlWriter createWriter(Encoding? encodingFallback, bool isAsync) => XmlWriter.Create(stream, new XmlWriterSettings {
                Encoding = this.Encoding ?? encodingFallback ?? UTF8,
                Async    = isAsync
            });
        }

    }

}

/// <summary>
/// Separate class that delegates all calls to methods in <see cref="JsonContent"/>. This is useful because if <see cref="JsonContent"/> is not on the classpath, these method calls will throw <see cref="FileNotFoundException"/>, instead of the caller throwing it. This can happen if both the runtime is either .NET Framework or .NET Core ≤ 3.1, and the project does not depend on the <c>System.Net.Http.Json</c> package.
/// </summary>
internal static class JsonContentDelegate {

    /// <inheritdoc cref="JsonContent.Create{T}(T,System.Net.Http.Headers.MediaTypeHeaderValue?,System.Text.Json.JsonSerializerOptions?)" />
    /// <exception cref="FileNotFoundException"><c>System.Net.Http.Json</c> is not on the classpath</exception>
    public static JsonContent Create<T>(T inputValue, MediaTypeHeaderValue? mediaType = null, JsonSerializerOptions? options = null) => JsonContent.Create(inputValue, mediaType, options);

    /// <inheritdoc cref="JsonContent.Create(object?,System.Type,System.Net.Http.Headers.MediaTypeHeaderValue?,System.Text.Json.JsonSerializerOptions?)" />
    /// <exception cref="FileNotFoundException"><c>System.Net.Http.Json</c> is not on the classpath</exception>
    public static JsonContent Create(object? inputValue, Type inputType, MediaTypeHeaderValue? mediaType = null, JsonSerializerOptions? options = null) =>
        JsonContent.Create(inputValue, inputType, mediaType, options);

    /// <inheritdoc cref="JsonContent.Create{T}(T?,System.Text.Json.Serialization.Metadata.JsonTypeInfo{T},System.Net.Http.Headers.MediaTypeHeaderValue?)" />
    /// <exception cref="FileNotFoundException"><c>System.Net.Http.Json</c> is not on the classpath</exception>
    public static JsonContent Create<T>(T? inputValue, JsonTypeInfo<T> jsonTypeInfo, MediaTypeHeaderValue? mediaType = null) => JsonContent.Create(inputValue, jsonTypeInfo, mediaType);

    /// <inheritdoc cref="JsonContent.Create(object?,System.Text.Json.Serialization.Metadata.JsonTypeInfo,System.Net.Http.Headers.MediaTypeHeaderValue?)" />
    /// <exception cref="FileNotFoundException"><c>System.Net.Http.Json</c> is not on the classpath</exception>
    public static JsonContent Create(object? inputValue, JsonTypeInfo jsonTypeInfo, MediaTypeHeaderValue? mediaType = null) => JsonContent.Create(inputValue, jsonTypeInfo, mediaType);

}