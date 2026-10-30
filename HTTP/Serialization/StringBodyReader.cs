using System.Net.Http.Headers;
using System.Text;
using Unfucked.HTTP.Config;

namespace Unfucked.HTTP.Serialization;

public sealed class StringBodyReader: MessageBodyReader {

    public bool CanRead<T>(MediaTypeHeaderValue? contentType) => typeof(T) == typeof(string);

    public async Task<T> Read<T>(HttpResponseMessage response, Encoding? responseEncoding, Configurable? clientConfig, CancellationToken cancellationToken) {
        Task<string> bodyStringTask =
#if NET5_0_OR_GREATER
            response.Content.ReadAsStringAsync(cancellationToken);
#else
            response.Content.ReadAsStringAsync();
#endif
        return (T) (object) await bodyStringTask.ConfigureAwait(false);
    }

}