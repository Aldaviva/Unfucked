using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Unfucked.HTTP.Config;

namespace Unfucked.HTTP.Serialization;

public sealed class ByteArrayBodyReader: MessageBodyReader {

    public bool CanRead<T>(MediaTypeHeaderValue? contentType, string? bodyPrefix) => typeof(T) == typeof(byte[]);

    public async Task<T> Read<T>(Stream responseBody, HttpStatusCode status, HttpResponseHeaders headers, HttpContentHeaders bodyHeaders, HttpRequestMessage request, IDisposable response,
                                 Encoding? responseEncoding, Configurable? clientConfig, CancellationToken cancellationToken) =>
        (T) (object) await ReadByteArray(responseBody, cancellationToken).ConfigureAwait(false);

    public sealed class MemoryBodyReader: MessageBodyReader {

        public bool CanRead<T>(MediaTypeHeaderValue? contentType, string? bodyPrefix) => typeof(T) == typeof(Memory<byte>);

        public async Task<T> Read<T>(Stream responseBody, HttpStatusCode status, HttpResponseHeaders headers, HttpContentHeaders bodyHeaders, HttpRequestMessage request, IDisposable response,
                                     Encoding? responseEncoding, Configurable? clientConfig, CancellationToken cancellationToken) =>
            (T) (object) (await ReadByteArray(responseBody, cancellationToken).ConfigureAwait(false)).AsMemory();

    }

    public sealed class ReadOnlyMemoryBodyReader: MessageBodyReader {

        public bool CanRead<T>(MediaTypeHeaderValue? contentType, string? bodyPrefix) => typeof(T) == typeof(ReadOnlyMemory<byte>);

        public async Task<T> Read<T>(Stream responseBody, HttpStatusCode status, HttpResponseHeaders headers, HttpContentHeaders bodyHeaders, HttpRequestMessage request, IDisposable response,
                                     Encoding? responseEncoding, Configurable? clientConfig, CancellationToken cancellationToken) =>
            (T) (object) new ReadOnlyMemory<byte>(await ReadByteArray(responseBody, cancellationToken).ConfigureAwait(false));

    }

    private static async Task<byte[]> ReadByteArray(Stream responseBody, CancellationToken cancellationToken) {
        MemoryStream buffer = new();

        Task copyToAsync = responseBody
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP2_1_OR_GREATER
            .CopyToAsync(buffer, cancellationToken);
#else
            .CopyToAsync(buffer);
#endif

        await copyToAsync.ConfigureAwait(false);

        return buffer.ToArray();
    }

}