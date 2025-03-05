using System.Text;
using Unfucked.HTTP.Config;

namespace Unfucked.HTTP.Serialization;

public class ByteArrayBodyReader: MessageBodyReader {

    public bool CanRead<T>(string? mimeType, string? bodyPrefix) => typeof(T) == typeof(byte[]);

    public async Task<T> Read<T>(HttpContent responseBody, Encoding? responseEncoding, IHttpConfiguration? clientConfig, CancellationToken cancellationToken) =>
        (T) (object) await ReadByteArray(responseBody, cancellationToken).ConfigureAwait(false);

    private static Task<byte[]> ReadByteArray(HttpContent responseBody, CancellationToken cancellationToken) =>
#if NET6_0_OR_GREATER
        responseBody.ReadAsByteArrayAsync(cancellationToken);
#else
        responseBody.ReadAsByteArrayAsync();
#endif

    public class MemoryBodyReader: MessageBodyReader {

        public bool CanRead<T>(string? mimeType, string? bodyPrefix) => typeof(T) == typeof(Memory<byte>);

        public async Task<T> Read<T>(HttpContent responseBody, Encoding? responseEncoding, IHttpConfiguration? clientConfig, CancellationToken cancellationToken) =>
            (T) (object) (await ReadByteArray(responseBody, cancellationToken).ConfigureAwait(false)).AsMemory();

    }

    public class ReadOnlyMemoryBodyReader: MessageBodyReader {

        public bool CanRead<T>(string? mimeType, string? bodyPrefix) => typeof(T) == typeof(ReadOnlyMemory<byte>);

        public async Task<T> Read<T>(HttpContent responseBody, Encoding? responseEncoding, IHttpConfiguration? clientConfig, CancellationToken cancellationToken) =>
            (T) (object) new ReadOnlyMemory<byte>(await ReadByteArray(responseBody, cancellationToken).ConfigureAwait(false));

    }

}