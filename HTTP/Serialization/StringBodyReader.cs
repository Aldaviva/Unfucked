using System.Text;
using Unfucked.HTTP.Config;

namespace Unfucked.HTTP.Serialization;

public class StringBodyReader: MessageBodyReader {

    public bool CanRead<T>(string? mimeType, string? bodyPrefix) => typeof(T) == typeof(string);

    public async Task<T> Read<T>(HttpContent responseBody, Encoding? responseEncoding, IHttpConfiguration? clientConfig, CancellationToken cancellationToken) {
        Task<string> readAsStringAsync =
#if NET6_0_OR_GREATER
            responseBody.ReadAsStringAsync(cancellationToken);
#else
            responseBody.ReadAsStringAsync();
#endif

        return (T) (object) await readAsStringAsync.ConfigureAwait(false);
    }

}