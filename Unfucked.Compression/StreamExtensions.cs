using System.Buffers;

namespace Unfucked.Compression;

public static class StreamExtensions {

    // Copied from internal SharpCompress.StreamExtensions.Write(Stream,ReadOnlySpan<byte>)
    internal static void Write(this Stream stream, ReadOnlySpan<byte> buffer) {
        byte[] temp = ArrayPool<byte>.Shared.Rent(buffer.Length);

        buffer.CopyTo(temp);

        try {
            stream.Write(temp, 0, buffer.Length);
        } finally {
            ArrayPool<byte>.Shared.Return(temp);
        }
    }

}