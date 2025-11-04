using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Unfucked.HTTP.Config;
#if NET6_0_OR_GREATER
using System.Net.Mime;
#endif

namespace Unfucked.HTTP.Serialization;

public class JsonBodyReader: MessageBodyReader {

    private const string APPLICATION_JSON_MEDIA_TYPE =
#if NET6_0_OR_GREATER
        MediaTypeNames.Application.Json;
#else
        "application/json";
#endif

    private static readonly JsonSerializerOptions DEFAULT_JSON_OPTIONS = new(JsonSerializerDefaults.Web) { ReadCommentHandling = JsonCommentHandling.Skip };

    public bool CanRead<T>(string? mimeType, string? bodyPrefix) =>
        typeof(T) == typeof(JsonDocument) ||
        typeof(T) == typeof(JsonNode) ||
        typeof(T) == typeof(JsonObject) ||
        typeof(T) == typeof(JsonArray) ||
        typeof(T) == typeof(JsonValue) ||
        mimeType == APPLICATION_JSON_MEDIA_TYPE ||
        (mimeType?.EndsWith("+json") ?? false) ||
        (bodyPrefix != null && (
            bodyPrefix.StartsWith('{') ||
            bodyPrefix.StartsWith('[') ||
            bodyPrefix.Contains("\"$schema\"")));

    /*
     * Don't parse using the convenience extension methods from System.Net.Http.Json because that closes the response body stream even when an exception is thrown, so we can't read the body to create the exception.
     * Instead, rely on UnfuckedWebTarget.DisposeIfNotStream<T> to close the stream when the entire response processing is done.
     */
    public async Task<T> Read<T>(HttpContent responseBody, Encoding? responseEncoding, Configurable? clientConfig, CancellationToken cancellationToken) {
        JsonSerializerOptions jsonOptions = (clientConfig?.Property(PropertyKey.JsonSerializerOptions, out JsonSerializerOptions? j) ?? false ? j : DEFAULT_JSON_OPTIONS)!;

        Task<Stream> readAsStreamAsync =
#if NET5_0_OR_GREATER
            responseBody.ReadAsStreamAsync(cancellationToken);
#else
            responseBody.ReadAsStreamAsync();
#endif

        return (await JsonSerializer.DeserializeAsync<T>(await readAsStreamAsync.ConfigureAwait(false), jsonOptions, cancellationToken))!;
    }

}