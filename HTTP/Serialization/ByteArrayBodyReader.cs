using System.Net.Http.Headers;
using System.Text;
using Unfucked.HTTP.Config;

namespace Unfucked.HTTP.Serialization;

public sealed class ByteArrayBodyReader: MessageBodyReader {

    public bool CanRead<T>(MediaTypeHeaderValue? contentType) => typeof(T) == typeof(byte[]);

    public async Task<T> Read<T>(HttpResponseMessage response, Encoding? responseEncoding, Configurable? clientConfig, CancellationToken cancellationToken) =>
        (T) (object) await ReadByteArray(response, cancellationToken).ConfigureAwait(false);

    public sealed class MemoryBodyReader: MessageBodyReader {

        public bool CanRead<T>(MediaTypeHeaderValue? contentType) => typeof(T) == typeof(Memory<byte>);

        public async Task<T> Read<T>(HttpResponseMessage response, Encoding? responseEncoding, Configurable? clientConfig, CancellationToken cancellationToken) =>
            (T) (object) (await ReadByteArray(response, cancellationToken).ConfigureAwait(false)).AsMemory();

    }

    public sealed class ReadOnlyMemoryBodyReader: MessageBodyReader {

        public bool CanRead<T>(MediaTypeHeaderValue? contentType) => typeof(T) == typeof(ReadOnlyMemory<byte>);

        public async Task<T> Read<T>(HttpResponseMessage response, Encoding? responseEncoding, Configurable? clientConfig, CancellationToken cancellationToken) =>
            (T) (object) new ReadOnlyMemory<byte>(await ReadByteArray(response, cancellationToken).ConfigureAwait(false));

    }

    private static async Task<byte[]> ReadByteArray(HttpResponseMessage response, CancellationToken cancellationToken) {
        Task<byte[]> bodyTask =
#if NET5_0_OR_GREATER
            response.Content.ReadAsByteArrayAsync(cancellationToken);
#else
            response.Content.ReadAsByteArrayAsync();
#endif

        return await bodyTask.ConfigureAwait(false);
    }

}