using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Unfucked.HTTP.Config;
#if NET6_0_OR_GREATER
using System.Net.Mime;
#endif

namespace Unfucked.HTTP.Serialization;

public sealed class JsonBodyReader: MessageBodyReader {

    private const string APPLICATION_JSON_MEDIA_TYPE =
#if NET6_0_OR_GREATER
        MediaTypeNames.Application.Json;
#else
        "application/json";
#endif

    internal static readonly JsonSerializerOptions DEFAULT_JSON_OPTIONS = new(JsonSerializerDefaults.Web) {
        ReadCommentHandling = JsonCommentHandling.Skip,
        Converters = {
            new JsonStringEnumConverter(new KernighanRitchieCEnumToLowerCamelCaseNamingPolicy())
        }
    };

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
    /// <exception cref="JsonException"></exception>
    public async Task<T> Read<T>(HttpContent responseBody, Encoding? responseEncoding, Configurable? clientConfig, CancellationToken cancellationToken) {
        JsonSerializerOptions jsonOptions = (clientConfig?.Property(PropertyKey.JsonSerializerOptions, out JsonSerializerOptions? j) ?? false ? j : DEFAULT_JSON_OPTIONS)!;

        Task<Stream> readAsStreamAsync =
#if NET5_0_OR_GREATER
            responseBody.ReadAsStreamAsync(cancellationToken);
#else
            responseBody.ReadAsStreamAsync();
#endif

        return (await JsonSerializer.DeserializeAsync<T>(await readAsStreamAsync.ConfigureAwait(false), jsonOptions, cancellationToken).ConfigureAwait(false))!;
    }

}

internal sealed class KernighanRitchieCEnumToLowerCamelCaseNamingPolicy: JsonNamingPolicy {

    public override string ConvertName(string name) {
        if (!name.HasText()) return name;

        bool isNewWord    = false;
        bool allUppercase = true;

        Span<char> output      = stackalloc char[name.Length];
        int        outputIndex = 0;
        for (int inputIndex = 0; inputIndex < name.Length; inputIndex++) {
            char c = name[inputIndex];

            if (c == '_' && inputIndex != 0 && name[inputIndex - 1] != '_') {
                isNewWord = true;
                continue;
            } else if (char.IsLower(c)) {
                allUppercase = false;
            } else if (!allUppercase || (inputIndex != 0 && inputIndex + 1 < name.Length && char.IsLower(name[inputIndex + 1]))) {
                isNewWord = true;
            }

            output[outputIndex++] = isNewWord ? char.ToUpperInvariant(c) : char.ToLowerInvariant(c);
            isNewWord             = char.IsNumber(c);
        }

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP2_1_OR_GREATER
        return new string(output[..outputIndex]);
#else
        return new string(output.Slice(0, outputIndex).ToArray());
#endif
    }

}