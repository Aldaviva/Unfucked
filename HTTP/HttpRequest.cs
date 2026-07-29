using System.Text.Json;
using Unfucked.HTTP.Config;
using Unfucked.HTTP.Serialization;

namespace Unfucked.HTTP;

public readonly record struct HttpRequest {

    public HttpMethod Verb { get; init; }
    public Uri Uri { get; init; }
    public IEnumerable<KeyValuePair<string, string>> Headers { get; init; }
    public HttpContent? Body { get; init; }
    public IClientConfig? ClientConfig { get; init; }

    public HttpRequest(HttpMethod verb, Uri uri, IEnumerable<KeyValuePair<string, string>> headers, HttpContent? body, IClientConfig? clientConfig) {
        Verb         = verb;
        Uri          = uri;
        Headers      = headers;
        ClientConfig = clientConfig;
        Body = body is Entity.JsonHttpContent { JsonOptions: null } jsonBody ? jsonBody.WithJsonOptions(
            ClientConfig?.Property(PropertyKey.JsonSerializerOptions, out JsonSerializerOptions? requestJsonOptions) ?? false
                ? requestJsonOptions : JsonBodyReader.DefaultJsonOptions) : body;
    }

    public static async Task<HttpRequest> Copy(HttpRequestMessage original) {
        IEnumerable<KeyValuePair<string, string>> replayedHeaders = original.Headers.SelectMany(static header => header.Value.Select(val => new KeyValuePair<string, string>(header.Key, val)));

        HttpContent? replayedRequestBody = null;
        if (original.Content is {} originalBody) {
            MemoryStream replayedRequestBodyStream = new();
            // CopyToAsync may fail if the original stream isn't seekable, in which case we should buffer all request bodies before their original request is sent
            await originalBody.CopyToAsync(replayedRequestBodyStream).ConfigureAwait(false);
            replayedRequestBody = new StreamContent(replayedRequestBodyStream);
            foreach (KeyValuePair<string, IEnumerable<string>> originalRequestBodyHeader in originalBody.Headers) {
                replayedRequestBody.Headers.Add(originalRequestBodyHeader.Key, originalRequestBodyHeader.Value);
            }
        }

        return new HttpRequest(original.Method, original.RequestUri!, replayedHeaders, replayedRequestBody, null);
    }

    /*
     * These crappy methods are only used for testing, so some are synchronous and inefficient
     */
    public bool Equals(HttpRequest other) =>
        Verb.Equals(other.Verb) &&
        Uri.Equals(other.Uri) &&
        Headers.Count() == other.Headers.Count() &&
        new HashSet<KeyValuePair<string, string>>(Headers, HeaderNameEqualityComparer.Instance).SetEquals(new HashSet<KeyValuePair<string, string>>(other.Headers,
            HeaderNameEqualityComparer.Instance)) &&
        ((Body is null && other.Body is null) ||
            (Body is not null && other.Body is not null && (SerializeBodySync()?.Equals(other.SerializeBodySync(), StringComparison.InvariantCulture) ?? true)));

    public override int GetHashCode() {
        unchecked {
            int hashCode = Verb.GetHashCode();
            hashCode = (hashCode * 397) ^ Uri.GetHashCode();
            hashCode = (hashCode * 397) ^ Headers.GetHashCode();
            hashCode = (hashCode * 397) ^ (Body != null ? Body.GetHashCode() : 0);
            return hashCode;
        }
    }

    public override string ToString() =>
        $"""
        {Verb} {Uri}
        {Headers.Select(static pair => $"{pair.Key}: {pair.Value}").Join('\n')}

        {SerializeBodySync()}
        """;

    private string? SerializeBodySync() => Body?.ReadAsStringAsync().GetAwaiter().GetResult();

    public void Deconstruct(out HttpMethod verb, out Uri uri, out IEnumerable<KeyValuePair<string, string>> headers, out HttpContent? body, out IClientConfig? clientConfig) {
        verb         = Verb;
        uri          = Uri;
        headers      = Headers;
        body         = Body;
        clientConfig = ClientConfig;
    }

    private sealed class HeaderNameEqualityComparer: IEqualityComparer<KeyValuePair<string, string>> {

        public static readonly HeaderNameEqualityComparer Instance = new();

        public bool Equals(KeyValuePair<string, string> x, KeyValuePair<string, string> y) => string.Equals(x.Key, y.Key, StringComparison.InvariantCultureIgnoreCase);

        public int GetHashCode(KeyValuePair<string, string> obj) => StringComparer.InvariantCultureIgnoreCase.GetHashCode((object) obj.Key);

    }

}