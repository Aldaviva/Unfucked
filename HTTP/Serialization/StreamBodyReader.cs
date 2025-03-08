using System.Text;
using Unfucked.HTTP.Config;

namespace Unfucked.HTTP.Serialization;

public class StreamBodyReader: MessageBodyReader {

    public bool CanRead<T>(string? mimeType, string? bodyPrefix) => typeof(T) == typeof(Stream);

    public async Task<T> Read<T>(HttpContent responseBody, Encoding? responseEncoding, Configurable? clientConfig, CancellationToken cancellationToken) {
        Task<Stream> stream =
#if NET6_0_OR_GREATER
            responseBody.ReadAsStreamAsync(cancellationToken);
#else
            responseBody.ReadAsStreamAsync();
#endif

        return (T) (object) await stream.ConfigureAwait(false);
    }

}