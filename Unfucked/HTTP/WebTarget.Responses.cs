using System.Net.Http.Headers;
using System.Net.Mime;
using System.Runtime.Serialization;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
#if NET6_0_OR_GREATER
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
#endif

namespace Unfucked.HTTP;

public partial class WebTarget {

    private const string ApplicationXmlMediaType =
#if NET6_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
        MediaTypeNames.Application.Xml;
#else
        "application/xml";
#endif

#if NET6_0_OR_GREATER
    private static readonly JsonSerializerOptions DefaultJsonOptions = new(JsonSerializerDefaults.Web);
#endif

    private async Task<T> ParseResponseBody<T>(HttpResponseMessage response, CancellationToken cancellationToken) {
        if (!Property(PropertyKey.ThrowOnUnsuccessfulStatusCode, out bool value) || value) {
            response.EnsureSuccessStatusCode();
        }

        Type                  deserializedType    = typeof(T);
        MediaTypeHeaderValue? responseContentType = response.Content.Headers.ContentType;
        Encoding?             responseEncoding    = null;
        try {
            responseEncoding = responseContentType?.CharSet is { } responseEncodingName ? Encoding.GetEncoding(responseEncodingName) : null;
        } catch (ArgumentException) { }

        if (deserializedType == typeof(XmlDocument)) {
            return (T) (object) await response.Content.ReadDomFromXmlAsync(responseEncoding, cancellationToken).ConfigureAwait(false);
        } else if (deserializedType == typeof(XPathNavigator)) {
            return (T) (object) await response.Content.ReadXPathFromXmlAsync(responseEncoding, cancellationToken).ConfigureAwait(false);
        } else if (deserializedType == typeof(XDocument)) {
            return (T) (object) await response.Content.ReadLinqFromXmlAsync(responseEncoding, cancellationToken: cancellationToken).ConfigureAwait(false);
        }
#if NET6_0_OR_GREATER
        else if (deserializedType == typeof(JsonObject) || deserializedType == typeof(JsonArray) || responseContentType?.MediaType == MediaTypeNames.Application.Json) {
            return await ParseJson<T>(response, cancellationToken).ConfigureAwait(false);
        }
#endif
        else if (responseContentType?.MediaType is MediaTypeNames.Text.Xml or ApplicationXmlMediaType) {
            return await ParseXml<T>(response, responseEncoding, cancellationToken).ConfigureAwait(false);
        } else {
#if NET6_0_OR_GREATER
            await using Stream responseBodyStream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
#elif NETSTANDARD2_1_OR_GREATER
            await using Stream responseBodyStream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
#else
            using Stream responseBodyStream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
#endif
            using StreamReader streamReader = new(responseBodyStream, responseEncoding ?? Strings.Utf8, true);
            char[]             headBuffer   = new char[32];
            int                headSize     = await streamReader.ReadAsync(headBuffer, 0, headBuffer.Length).ConfigureAwait(false);
            string             head         = new string(headBuffer, 0, headSize).Trim();
#if NET6_0_OR_GREATER
            if (head.StartsWith('{') || head.StartsWith('[') || head.Contains("\"$schema\"")) {
                return await ParseJson<T>(response, cancellationToken).ConfigureAwait(false);
            } else
#endif
            if (head.StartsWith('<') || head.Contains("<?xml", StringComparison.OrdinalIgnoreCase) || head.Contains("<!--") || head.Contains("xmlns", StringComparison.OrdinalIgnoreCase) ||
                head.Contains("<!doctype", StringComparison.OrdinalIgnoreCase)) {
                return await ParseXml<T>(response, responseEncoding, cancellationToken).ConfigureAwait(false);
            }
        }

        throw new SerializationException(
            $"Could not determine content type of response body to deserialize (URI: {response.RequestMessage?.RequestUri}, Content-Type: {responseContentType}, .NET type: {typeof(T)})");

    }

#if NET6_0_OR_GREATER
    private async Task<T> ParseJson<T>(HttpResponseMessage response, CancellationToken cancellationToken) => (await response.Content
        .ReadFromJsonAsync<T>(Property(PropertyKey.JsonSerializerOptions, out JsonSerializerOptions? j) ? j : DefaultJsonOptions, cancellationToken).ConfigureAwait(false))!;
#endif

    private static async Task<T> ParseXml<T>(HttpResponseMessage response, Encoding? responseEncoding, CancellationToken cancellationToken) =>
        await response.Content.ReadObjectFromXmlAsync<T>(responseEncoding, cancellationToken: cancellationToken).ConfigureAwait(false);

}