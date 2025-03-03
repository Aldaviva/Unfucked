using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Unfucked.HTTP.Config;
#if NET6_0_OR_GREATER
using System.Net.Mime;
#endif

namespace Unfucked.HTTP.Serialization;

public class JsonReader: MessageBodyReader {

    private const string ApplicationJsonMediaType =
#if NET6_0_OR_GREATER
        MediaTypeNames.Application.Json;
#else
        "application/json";
#endif

    private static readonly JsonSerializerOptions DefaultJsonOptions = new(JsonSerializerDefaults.Web);

    public bool CanRead<T>(string? mimeType, string? bodyPrefix) =>
        typeof(T) == typeof(JsonObject) ||
        typeof(T) == typeof(JsonArray) ||
        mimeType == ApplicationJsonMediaType ||
        (bodyPrefix != null && (
            bodyPrefix.StartsWith('{') ||
            bodyPrefix.StartsWith('[') ||
            bodyPrefix.Contains("\"$schema\"")));

    public async Task<T> Read<T>(HttpContent responseBody, Encoding? responseEncoding, IHttpConfiguration? clientConfig, CancellationToken cancellationToken) {
        try {
            return (await responseBody
                .ReadFromJsonAsync<T>(clientConfig?.Property(PropertyKey.JsonSerializerOptions, out JsonSerializerOptions? j) ?? false ? j : DefaultJsonOptions, cancellationToken)
                .ConfigureAwait(false))!;
        } catch (JsonException e) {
            throw new MessageBodyReader.FailedToRead(e);
        } catch (NotSupportedException e) {
            throw new MessageBodyReader.FailedToRead(e);
        }
    }

}