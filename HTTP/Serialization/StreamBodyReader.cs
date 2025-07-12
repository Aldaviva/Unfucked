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

        return (T) (object) new ResponseDisposingStream(await stream.ConfigureAwait(false), responseBody);
    }

    internal sealed class ResponseDisposingStream(Stream actualStream, IDisposable alsoDispose): Stream {

        public override bool CanRead => actualStream.CanRead;
        public override bool CanSeek => actualStream.CanSeek;
        public override bool CanWrite => actualStream.CanWrite;
        public override long Length => actualStream.Length;
        public override bool CanTimeout => actualStream.CanTimeout;
        public override long Position {
            get => actualStream.Position;
            set => actualStream.Position = value;
        }
        public override int ReadTimeout {
            get => actualStream.ReadTimeout;
            set => actualStream.ReadTimeout = value;
        }
        public override int WriteTimeout {
            get => actualStream.WriteTimeout;
            set => actualStream.WriteTimeout = value;
        }

        public override void Flush() => actualStream.Flush();

        public override int Read(byte[] buffer, int offset, int count) => actualStream.Read(buffer, offset, count);

        public override long Seek(long offset, SeekOrigin origin) => actualStream.Seek(offset, origin);

        public override void SetLength(long value) => actualStream.SetLength(value);

        public override void Write(byte[] buffer, int offset, int count) => actualStream.Write(buffer, offset, count);

        public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state) => actualStream.BeginRead(buffer, offset, count, callback, state);

        public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state) => actualStream.BeginWrite(buffer, offset, count, callback, state);

        public override void Close() => actualStream.Close();

        public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken) => actualStream.CopyToAsync(destination, bufferSize, cancellationToken);

        public override int EndRead(IAsyncResult asyncResult) => actualStream.EndRead(asyncResult);

        public override void EndWrite(IAsyncResult asyncResult) => actualStream.EndWrite(asyncResult);

        public override Task FlushAsync(CancellationToken cancellationToken) => actualStream.FlushAsync(cancellationToken);

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) => actualStream.ReadAsync(buffer, offset, count, cancellationToken);

        public override int ReadByte() => actualStream.ReadByte();

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) => actualStream.WriteAsync(buffer, offset, count, cancellationToken);

        public override void WriteByte(byte value) => actualStream.WriteByte(value);

        protected override void Dispose(bool disposing) {
            if (disposing) {
                actualStream.Dispose();
                alsoDispose.Dispose();
            }
            base.Dispose(disposing);
        }

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP2_0_OR_GREATER
        public override void CopyTo(Stream destination, int bufferSize) => actualStream.CopyTo(destination, bufferSize);
#endif

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP2_1_OR_GREATER
        public override int Read(Span<byte> buffer) => actualStream.Read(buffer);

        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) => actualStream.ReadAsync(buffer, cancellationToken);

        public override void Write(ReadOnlySpan<byte> buffer) => actualStream.Write(buffer);

        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default) => actualStream.WriteAsync(buffer, cancellationToken);
#endif

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        public override async ValueTask DisposeAsync() {
            await actualStream.DisposeAsync().ConfigureAwait(false);
            if (alsoDispose is IAsyncDisposable alsoDisposeAsyncDisposable) {
                await alsoDisposeAsyncDisposable.DisposeAsync().ConfigureAwait(false);
            } else {
                alsoDispose.Dispose();
            }
            await base.DisposeAsync().ConfigureAwait(false);
        }
#endif

    }

}