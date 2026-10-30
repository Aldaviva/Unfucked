using System.Data;

namespace Unfucked.HTTP;

/// <summary>Allow the first several bytes of an unseekable stream to be buffered and reread, to allow HTTP response body magic prefix sniffing when Content-Type is missing or vague.</summary>
internal sealed class PartiallyBufferedStream: Stream {

    private readonly Stream       inner;
    public readonly  long         BufferLength;
    private readonly MemoryStream prefixBuffer;

    private long position;

    // ExceptionAdjustment: P:System.IO.Stream.Position get -T:System.IO.IOException
    internal PartiallyBufferedStream(Stream inner, MemoryStream buffer) {
        this.inner   = inner;
        prefixBuffer = buffer;
        BufferLength = buffer.Length;
        position     = inner.CanSeek ? inner.Position : 0;
    }

    public static async Task<PartiallyBufferedStream> CreateAndBuffer(Stream inner, int bufferLength, CancellationToken cancellationToken = default) {
        byte[] buffer = new byte[bufferLength];
        bufferLength = await inner.ReadAsync(buffer, 0, buffer.Length, cancellationToken).ConfigureAwait(false);

        return new PartiallyBufferedStream(inner, new MemoryStream(buffer, 0, bufferLength, false));
    }

    /// <exception cref="IOException"></exception>
    public override int Read(byte[] buffer, int offset, int count) {
        int bytesRead = 0;
        if (position < BufferLength) {
            bytesRead += prefixBuffer.Read(buffer, offset, count);
        }
        if (position + count > BufferLength) {
            if (bytesRead != 0 && inner.CanSeek) {
                inner.Position = BufferLength;
            }
            bytesRead += inner.Read(buffer, offset + bytesRead, count - bytesRead);
        }
        position += bytesRead;
        return bytesRead;
    }

    /// <exception cref="IOException"></exception>
    public override long Position {
        get => inner.Position;
        set {
            position              = value;
            prefixBuffer.Position = value;
            if (position >= BufferLength) {
                inner.Position = value;
            }
        }
    }

    public override long Seek(long offset, SeekOrigin origin) {
        position = origin switch {
            SeekOrigin.Begin   => offset,
            SeekOrigin.Current => position + offset,
            SeekOrigin.End     => inner.Length + offset
        };
        prefixBuffer.Position = position;
        return position < BufferLength ? position : inner.Seek(offset, origin);
    }

    public override bool CanRead => inner.CanRead;
    public override bool CanSeek => inner.CanSeek;
    public override bool CanWrite => false;
    public override long Length => inner.Length;

    /// <exception cref="ReadOnlyException"></exception>
    public override void SetLength(long value) => throw new ReadOnlyException();

    /// <exception cref="ReadOnlyException"></exception>
    public override void Write(byte[] buffer, int offset, int count) => throw new ReadOnlyException();

    /// <exception cref="ReadOnlyException"></exception>
    public override void Flush() => throw new ReadOnlyException();

    protected override void Dispose(bool disposing) {
        if (disposing) {
            inner.Dispose();
        }
        base.Dispose(disposing);
    }

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
    public override async ValueTask DisposeAsync() {
        await inner.DisposeAsync().ConfigureAwait(false);
        await base.DisposeAsync().ConfigureAwait(false);
    }
#endif

}