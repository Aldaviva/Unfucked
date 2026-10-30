using System.Data;

namespace Unfucked.HTTP;

internal class PartiallyBufferedStream: Stream {

    private readonly Stream       inner;
    public readonly  long         BufferLength;
    private readonly MemoryStream prefixBuffer;

    private long position;

    internal PartiallyBufferedStream(Stream inner, MemoryStream buffer) {
        this.inner   = inner;
        prefixBuffer = buffer;
        BufferLength = buffer.Length;
        position     = inner.CanSeek ? inner.Position : 0;
    }

    public static async Task<PartiallyBufferedStream> CreateAndBuffer(Stream inner, int bufferLength) {
        byte[] buffer = new byte[bufferLength];
        bufferLength = await inner.ReadAsync(buffer, 0, buffer.Length).ConfigureAwait(false);

        return new PartiallyBufferedStream(inner, new MemoryStream(buffer, 0, bufferLength, false));
    }

    public override int Read(byte[] buffer, int offset, int count) {
        int bytesRead = 0;
        if (position < BufferLength) {
            bytesRead += prefixBuffer.Read(buffer, offset, count);
        }
        if (position + count > BufferLength) {
            // if (bytesRead != 0 && inner.CanSeek) {
            //     inner.Position = bufferLength;
            // }
            bytesRead += inner.Read(buffer, offset + bytesRead, count - bytesRead);
        }
        position += bytesRead;
        return bytesRead;
    }

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

    public override void SetLength(long value) => throw new ReadOnlyException();
    public override void Write(byte[] buffer, int offset, int count) => throw new ReadOnlyException();
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