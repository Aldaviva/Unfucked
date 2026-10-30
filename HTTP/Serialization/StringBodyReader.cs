using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Unfucked.HTTP.Config;

namespace Unfucked.HTTP.Serialization;

public sealed class StringBodyReader: MessageBodyReader {

    public bool CanRead<T>(MediaTypeHeaderValue? contentType, string? bodyPrefix) => typeof(T) == typeof(string);

    public async Task<T> Read<T>(Stream responseBody, HttpStatusCode status, HttpResponseHeaders headers, HttpContentHeaders bodyHeaders, HttpRequestMessage request, IDisposable response,
                                 Encoding? responseEncoding, Configurable? clientConfig, CancellationToken cancellationToken) {
        using StreamReader streamReader = new(responseBody, responseEncoding ?? Entity.Utf8, true);
        Task<string> readToEndAsync = streamReader
#if NET7_0_OR_GREATER
            .ReadToEndAsync(cancellationToken);
#else
            .ReadToEndAsync();
#endif

        return (T) (object) await readToEndAsync.ConfigureAwait(false);
    }

}