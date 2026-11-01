using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Unfucked.HTTP.Config;
#if NET6_0_OR_GREATER
using System.Net.Mime;
#endif

namespace Unfucked.HTTP.Serialization;

public sealed class JsonBodyReader: PrefixDetectingMessageBodyReader {

    private const string APPLICATION_JSON_MEDIA_TYPE =
#if NET6_0_OR_GREATER
        MediaTypeNames.Application.Json;
#else
        "application/json";
#endif

    internal static readonly JsonSerializerOptions DefaultJsonOptions = new(JsonSerializerDefaults.Web) {
        ReadCommentHandling = JsonCommentHandling.Skip,
        Converters = {
            new JsonStringEnumConverter(new KernighanRitchieCEnumToLowerCamelCaseNamingPolicy())
        }
    };

    /// <inheritdoc />
    public override bool CanRead<T>(MediaTypeHeaderValue? contentType, string? bodyPrefix) =>
        typeof(T) == typeof(JsonDocument) ||
        typeof(T) == typeof(JsonNode) ||
        typeof(T) == typeof(JsonObject) ||
        typeof(T) == typeof(JsonArray) ||
        typeof(T) == typeof(JsonValue) ||
        contentType?.MediaType == APPLICATION_JSON_MEDIA_TYPE ||
        (contentType?.MediaType?.EndsWith("+json") ?? false) ||
        (bodyPrefix != null && (
            bodyPrefix.StartsWith('{') ||
            bodyPrefix.StartsWith('[') ||
            bodyPrefix.Contains("\"$schema\"")));

    /*
     * Don't parse using the convenience extension methods from System.Net.Http.Json because that closes the response body stream even when an exception is thrown, so we can't read the body to create the exception.
     * Instead, rely on UnfuckedWebTarget.DisposeIfNotStream<T> to close the stream when the entire response processing is done.
     */
    /// <inheritdoc />
    /// <exception cref="JsonException"></exception>
    public override async Task<T> Read<T>(Stream responseBody, HttpStatusCode status, HttpResponseHeaders headers, HttpContentHeaders bodyHeaders, HttpRequestMessage request, IDisposable response,
                                          Encoding? responseEncoding, Configurable? clientConfig, CancellationToken cancellationToken) {
        JsonSerializerOptions jsonOptions = (clientConfig?.Property(PropertyKey.JsonSerializerOptions, out JsonSerializerOptions? j) ?? false ? j : DefaultJsonOptions)!;

        // parser assumes and requires response body encoding to be UTF-8
        return (await JsonSerializer.DeserializeAsync<T>(responseBody, jsonOptions, cancellationToken).ConfigureAwait(false))!;
    }

}

/// <summary>
/// <para>Serialize Kernighan &amp; Ritchie C-style UPPER_SNAKE_CASE C# enums (like <c>MY_ENUM</c>) to various customizable JSON forms, such as <c>myEnum</c> or <c>my-enum</c>. Also handles other C# cases like UpperTitleCase and lowerTitleCase.</para>
/// <para>This is an improved alternative to <see cref="JsonStringEnumConverter"/> that doesn't require C# enums to always be named in UpperCamelCase or lowerCamelCase.</para>
/// <para>The JSON form of the enum name defaults to lowerCamelCase, such as <c>myEnum</c>, but can be customized by specifying <paramref name="jsonNamingPolicy"/>.</para>
/// </summary>
/// <param name="jsonNamingPolicy">Controls the style of the JSON names. Defaults to lowerCamelCase JSON names when <c>null</c>. For example, to serialize the C# enum <c>MY_ENUM</c> as <c>my-enum</c> instead, set this argument to <see cref="JsonNamingPolicy.KebabCaseLower"/>.</param>
public sealed class KernighanRitchieCEnumToLowerCamelCaseNamingPolicy(JsonNamingPolicy? jsonNamingPolicy = null): JsonNamingPolicy {

    /// <inheritdoc />
    public override string ConvertName(string name) {
        if (!name.HasText) return name;

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

        string lowerCamelCaseName =
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP2_1_OR_GREATER
            new(output[..outputIndex]);
#else
            new(output.Slice(0, outputIndex).ToArray());
#endif

        return jsonNamingPolicy?.Equals(CamelCase) ?? true ? lowerCamelCaseName : jsonNamingPolicy.ConvertName(lowerCamelCaseName);
    }

}