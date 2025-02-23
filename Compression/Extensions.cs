using System.Buffers;

namespace Unfucked.Compression;

internal static class Extensions {

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

    // Copied from internal SharpCompress.Helpers.Utility.TrimNulls(string)   
    public static string TrimNulls(this string source) => source.Replace('\0', ' ').Trim();

    // Copied from internal SharpCompress.Helpers.Utility.TransferTo(Stream, Stream)
    public static long TransferTo(this Stream source, Stream destination) {
        byte[] array = GetTransferByteArray();
        try {
            long total = 0;
            while (ReadTransferBlock(source, array, out int count)) {
                destination.Write(array, 0, count);
                total += count;
            }
            return total;
        } finally {
            ArrayPool<byte>.Shared.Return(array);
        }
    }

    // Copied from internal SharpCompress.Helpers.Utility.GetTransferByteArray()
    private static byte[] GetTransferByteArray() => ArrayPool<byte>.Shared.Rent(81920);

    // Copied from internal SharpCompress.Helpers.Utility.ReadTransferBlock(Stream, byte[], out int)
    private static bool ReadTransferBlock(Stream source, byte[] array, out int count) => (count = source.Read(array, 0, array.Length)) != 0;

}