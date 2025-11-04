using SharpCompress.Common;
using System.Buffers.Binary;
using System.Text;
using Unfucked.Compression.Writers.Tar;

// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable InconsistentNaming

namespace Unfucked.Compression.Common.Tar.Headers;

#nullable disable

/// <summary>
/// <para>Like <c>SharpCompress.Common.Tar.Headers.TarHeader</c> except you're not prevented from setting the file mode, owner, and group, or from adding symlinks.</para>
/// <para>This is generally used inside <see cref="TarWriter"/>, but if you're subclassing it, you may need to create an instance of this yourself.</para>
/// <para>Usage: construct a new instance and set all the properties you want like <see cref="Name"/> and <see cref="EntryType"/>, then call <see cref="Write"/>, passing the <see cref="Stream"/> from <c>TarWriter.OutputStream</c>.</para>
/// </summary>
public class TarHeader(ArchiveEncoding archiveEncoding) {

    internal static readonly DateTime EPOCH = new(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    internal string Name { get; set; }
    internal string LinkName { get; set; }

    /// <summary>
    /// Unix file permissions mode for the file or directory, or -1 to use the default 0o777 mode.
    /// </summary>
    public long Mode { get; set; } = -1;

    /// <summary>
    /// Unix owner user ID for the file or directory, or 0 to not specify an owner.
    /// </summary>
    public long UserId { get; set; }

    /// <summary>
    /// Unix group ID for the file or directory, or 0 to not specify a group.
    /// </summary>
    public long GroupId { get; set; }

    internal long Size { get; set; }
    internal DateTime LastModifiedTime { get; set; }

    /// <summary>
    /// Specifies the type of the filesystem object, such as file, directory, or symlink.
    /// </summary>
    public EntryType EntryType { get; set; }

    internal Stream PackedStream { get; set; }
    internal ArchiveEncoding ArchiveEncoding { get; } = archiveEncoding;

    internal const int BLOCK_SIZE = 512;

    /// <summary>
    /// Serialize the file into the TAR archive.
    /// </summary>
    /// <param name="output">TAR archive output stream, usually provided by <c>TarWriter.OutputStream</c> in <see cref="TarWriter"/>.</param>
    protected internal virtual void Write(Stream output) {
        byte[] buffer = new byte[BLOCK_SIZE];

        WriteOctalBytes(Mode != -1 ? Mode : 511, buffer, 100, 8); // file mode fixed by Ben: not hardcoded to 0o777
        WriteOctalBytes(UserId, buffer, 108, 8);                  // owner ID fixed by Ben: not hardcoded to 0
        WriteOctalBytes(GroupId, buffer, 116, 8);                 // group ID fixed by Ben: not hardcoded to 0

        int nameByteCount = ArchiveEncoding.GetEncoding().GetByteCount(Name);
        if (nameByteCount > 100) {
            // Set mock filename and filetype to indicate the next block is the actual name of the file
            WriteStringBytes("././@LongLink", buffer, 0, 100);
            buffer[156] = (byte) EntryType.LongName;
            WriteOctalBytes(nameByteCount + 1, buffer, 124, 12);
        } else {
            WriteStringBytes(ArchiveEncoding.Encode(Name), buffer, 100);
            WriteOctalBytes(Size, buffer, 124, 12);
            long time = (long) (LastModifiedTime.ToUniversalTime() - EPOCH).TotalSeconds;
            WriteOctalBytes(time, buffer, 136, 12);
            buffer[156] = (byte) EntryType;

            if (Size >= 0x1FFFFFFFF) {
                Span<byte> bytes12 = stackalloc byte[12];
                BinaryPrimitives.WriteInt64BigEndian(bytes12.Slice(4), Size);
                bytes12[0] |= 0x80;
                bytes12.CopyTo(buffer.AsSpan(124));
            }

            // added by Ben: serialize symlinks
            if (EntryType == EntryType.SymLink) {
                WriteStringBytes(ArchiveEncoding.Encode(LinkName), buffer.AsSpan(157, 100), 100);
            }
        }

        int crc = RecalculateChecksum(buffer);
        WriteOctalBytes(crc, buffer, 148, 8);

        output.Write(buffer, 0, buffer.Length);

        if (nameByteCount > 100) {
            WriteLongFilenameHeader(output);
            // update to short name lower than 100 - [max bytes of one character].
            // subtracting bytes is needed because preventing infinite loop(example code is here).
            //
            // var bytes = Encoding.UTF8.GetBytes(new string(0x3042, 100));
            // var truncated = Encoding.UTF8.GetBytes(Encoding.UTF8.GetString(bytes, 0, 100));
            //
            // and then infinite recursion is occured in WriteLongFilenameHeader because truncated.Length is 102.
            Name = ArchiveEncoding.Decode(
                ArchiveEncoding.Encode(Name),
                0,
                100 - ArchiveEncoding.GetEncoding().GetMaxByteCount(1)
            );
            Write(output);
        }
    }

    private void WriteLongFilenameHeader(Stream output) {
        byte[] nameBytes = ArchiveEncoding.Encode(Name);
        output.Write(nameBytes, 0, nameBytes.Length);

        // pad to multiple of BlockSize bytes, and make sure a terminating null is added
        int numPaddingBytes = BLOCK_SIZE - nameBytes.Length % BLOCK_SIZE;
        if (numPaddingBytes == 0) {
            numPaddingBytes = BLOCK_SIZE;
        }

        output.Write(stackalloc byte[numPaddingBytes]);
    }

    /// <exception cref="InvalidOperationException"></exception>
    /// <exception cref="FormatException"></exception>
    internal bool Read(BinaryReader reader) {
        byte[] buffer = ReadBlock(reader);
        if (buffer.Length == 0) {
            return false;
        }

        // for symlinks, additionally read the link name
        if (ReadEntryType(buffer) == EntryType.SymLink) {
            LinkName = ArchiveEncoding.Decode(buffer, 157, 100).TrimNulls();
        }

        if (ReadEntryType(buffer) == EntryType.LongName) {
            Name   = ReadLongName(reader, buffer);
            buffer = ReadBlock(reader);
        } else {
            Name = ArchiveEncoding.Decode(buffer, 0, 100).TrimNulls();
        }

        EntryType = ReadEntryType(buffer);
        Size      = ReadSize(buffer);

        Mode = ReadAsciiInt64Base8(buffer, 100, 7);
        if (EntryType == EntryType.Directory) {
            Mode |= 0b1_000_000_000;
        }

        UserId  = ReadAsciiInt64Base8oldGnu(buffer, 108, 7);
        GroupId = ReadAsciiInt64Base8oldGnu(buffer, 116, 7);
        long unixTimeStamp = ReadAsciiInt64Base8(buffer, 136, 11);
        LastModifiedTime = EPOCH.AddSeconds(unixTimeStamp).ToLocalTime();

        string magic = ArchiveEncoding.Decode(buffer, 257, 6).TrimNulls();

        if (!string.IsNullOrEmpty(magic) && "ustar".Equals(magic)) {
            string namePrefix = ArchiveEncoding.Decode(buffer, 345, 157);
            namePrefix = namePrefix.TrimNulls();
            if (!string.IsNullOrEmpty(namePrefix)) {
                Name = namePrefix + "/" + Name;
            }
        }

        if (EntryType != EntryType.LongName && Name.Length == 0) {
            return false;
        }

        return true;
    }

    /// <exception cref="FormatException"></exception>
    private string ReadLongName(BinaryReader reader, byte[] buffer) {
        long   size                 = ReadSize(buffer);
        int    nameLength           = (int) size;
        byte[] nameBytes            = reader.ReadBytes(nameLength);
        int    remainingBytesToRead = BLOCK_SIZE - nameLength % BLOCK_SIZE;

        // Read the rest of the block and discard the data
        if (remainingBytesToRead < BLOCK_SIZE) {
            reader.ReadBytes(remainingBytesToRead);
        }

        return ArchiveEncoding.Decode(nameBytes, 0, nameBytes.Length).TrimNulls();
    }

    private static EntryType ReadEntryType(byte[] buffer) => (EntryType) buffer[156];

    /// <exception cref="FormatException"></exception>
    private static long ReadSize(byte[] buffer) {
        if ((buffer[124] & 0x80) == 0x80) // if size in binary
        {
            return BinaryPrimitives.ReadInt64BigEndian(buffer.AsSpan(0x80));
        }

        return ReadAsciiInt64Base8(buffer, 124, 11);
    }

    /// <exception cref="InvalidOperationException"></exception>
    private static byte[] ReadBlock(BinaryReader reader) {
        byte[] buffer = reader.ReadBytes(BLOCK_SIZE);

        if (buffer.Length != 0 && buffer.Length < BLOCK_SIZE) {
            throw new InvalidOperationException("Buffer is invalid size");
        }

        return buffer;
    }

    private static void WriteStringBytes(ReadOnlySpan<byte> name, Span<byte> buffer, int length) {
        name.CopyTo(buffer);
        int i = Math.Min(length, name.Length);
        buffer.Slice(i, length - i).Clear();
    }

    private static void WriteStringBytes(string name, byte[] buffer, int offset, int length) {
        int i;

        for (i = 0; i < length && i < name.Length; ++i) {
            buffer[offset + i] = (byte) name[i];
        }

        for (; i < length; ++i) {
            buffer[offset + i] = 0;
        }
    }

    private static void WriteOctalBytes(long value, byte[] buffer, int offset, int length) {
        string val   = Convert.ToString(value, 8);
        int    shift = length - val.Length - 1;
        for (int i = 0; i < shift; i++) {
            buffer[offset + i] = (byte) ' ';
        }

        for (int i = 0; i < val.Length; i++) {
            buffer[offset + i + shift] = (byte) val[i];
        }
    }

    /// <exception cref="FormatException"></exception>
    private static int ReadAsciiInt32Base8(byte[] buffer, int offset, int count) {
        string s = Encoding.UTF8.GetString(buffer, offset, count).TrimNulls();
        if (string.IsNullOrEmpty(s)) {
            return 0;
        }

        return Convert.ToInt32(s, 8);
    }

    /// <exception cref="FormatException"></exception>
    private static long ReadAsciiInt64Base8(byte[] buffer, int offset, int count) {
        string s = Encoding.UTF8.GetString(buffer, offset, count).TrimNulls();
        if (string.IsNullOrEmpty(s)) {
            return 0;
        }

        return Convert.ToInt64(s, 8);
    }

    /// <exception cref="FormatException"></exception>
    private static long ReadAsciiInt64Base8oldGnu(byte[] buffer, int offset, int count) {
        if (buffer[offset] == 0x80 && buffer[offset + 1] == 0x00) {
            return (buffer[offset + 4] << 24)
                | (buffer[offset + 5] << 16)
                | (buffer[offset + 6] << 8)
                | buffer[offset + 7];
        }

        string s = Encoding.UTF8.GetString(buffer, offset, count).TrimNulls();

        if (string.IsNullOrEmpty(s)) {
            return 0;
        }

        return Convert.ToInt64(s, 8);
    }

    /// <exception cref="FormatException"></exception>
    private static long ReadAsciiInt64(byte[] buffer, int offset, int count) {
        string s = Encoding.UTF8.GetString(buffer, offset, count).TrimNulls();
        if (string.IsNullOrEmpty(s)) {
            return 0;
        }

        return Convert.ToInt64(s);
    }

    private static readonly byte[] eightSpaces = "        "u8.ToArray();

    internal static int RecalculateChecksum(byte[] buf) {
        // Set default value for checksum. That is 8 spaces.
        eightSpaces.CopyTo(buf, 148);

        // Calculate checksum
        return buf.Aggregate(0, (current, b) => current + b);
    }

    internal static int RecalculateAltChecksum(byte[] buf) {
        eightSpaces.CopyTo(buf, 148);
        int headerChecksum = 0;
        foreach (byte b in buf) {
            if ((b & 0x80) == 0x80) {
                headerChecksum -= b ^ 0x80;
            } else {
                headerChecksum += b;
            }
        }

        return headerChecksum;
    }

    // public long? DataStartPosition { get; set; }

    // public string Magic { get; set; }

}

/// <summary>
/// Specifies the type of a filesystem object, such as file, directory, or symlink.
/// </summary>
public enum EntryType: byte {

    /// <summary>
    /// A regular file
    /// </summary>
    File = 0,

    /// <summary>
    /// A regular file
    /// </summary>
    OldFile = (byte) '0',

    /// <summary>
    /// A hardlink
    /// </summary>
    HardLink = (byte) '1',

    /// <summary>
    /// A symbolic link
    /// </summary>
    SymLink = (byte) '2',

    /// <summary>
    /// Character device node
    /// </summary>
    CharDevice = (byte) '3',

    /// <summary>
    /// Block device node
    /// </summary>
    BlockDevice = (byte) '4',

    /// <summary>
    /// A directory/folder
    /// </summary>
    Directory = (byte) '5',

    /// <summary>
    /// FIFO node
    /// </summary>
    Fifo = (byte) '6',

    /// <summary>
    /// The data for this entry is a long link name for the following regular entry.
    /// </summary>
    LongLink = (byte) 'K',

    /// <summary>
    /// The data for this entry is a long pathname for the following regular entry.
    /// </summary>
    LongName = (byte) 'L',

    /// <summary>
    /// This is a "sparse" regular file. Sparse files are stored as a series of fragments. The header contains a list of fragment offset/length pairs. If more than four such entries are required, the header is extended as necessary with “extra” header extensions (an older format that is no longer used), or "sparse" extensions.
    /// </summary>
    SparseFile = (byte) 'S',

    /// <summary>
    /// The <c>name</c> field should be interpreted as a tape/volume header name. This entry should generally be ignored on extraction.
    /// </summary>
    VolumeHeader = (byte) 'V',

    /// <summary>
    /// Global extended header
    /// </summary>
    GlobalExtendedHeader = (byte) 'g'

}