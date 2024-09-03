using SharpCompress;
using SharpCompress.Writers.Tar;
using Unfucked.Compression.Common.Tar.Headers;

namespace Unfucked.Compression.Writers.Tar;

/// <summary>
/// Like <see cref="SharpCompress.Writers.Tar.TarWriter"/> except you're not prevented from setting the file mode, owner, and group on files when creating TAR archives.
/// </summary>
/// <param name="destination">stream to write TAR to</param>
/// <param name="options">options for how to write the TAR file</param>
// ReSharper disable InconsistentNaming - extension methods
public class TarWriter(Stream destination, TarWriterOptions options): SharpCompress.Writers.Tar.TarWriter(destination, options) {

    /// <exception cref="ArgumentException"><paramref name="source"/> cannot seek and <paramref name="size"/> is <c>null</c></exception>
    public virtual void WriteFile(string filename, Stream source, DateTime? modificationTime, long? size, int? fileMode, int ownerId = 0, int groupId = 0) {
        if (!source.CanSeek && size is null) {
            throw new ArgumentException("Seekable stream is required if no size is given.");
        }

        long realSize = size ?? source.Length;

        TarHeader header = new(WriterOptions.ArchiveEncoding) {
            LastModifiedTime = modificationTime ?? TarHeader.EPOCH,
            Name             = NormalizeFilename(filename),
            Size             = realSize,
            UserId           = ownerId, //added by Ben: set newly-public properties
            GroupId          = groupId  //added by Ben: set newly-public properties
        };
        if (fileMode.HasValue) {
            //added by Ben: set newly-public properties
            header.Mode = fileMode.Value;
        }

        header.Write(OutputStream);

        size = source.TransferTo(OutputStream);
        PadTo512(size.Value);
    }

    public virtual void WriteDirectory(string directoryName, DateTime? modificationTime, int? fileMode, int ownerId = 0, int groupId = 0) {
        TarHeader header = new(WriterOptions.ArchiveEncoding) {
            LastModifiedTime = modificationTime ?? TarHeader.EPOCH,
            Name             = NormalizeFilename(directoryName),
            UserId           = ownerId,
            GroupId          = groupId,
            EntryType        = EntryType.Directory
        };
        if (fileMode.HasValue) {
            header.Mode = fileMode.Value;
        }

        header.Write(OutputStream);
    }

    public virtual void WriteSymLink(string source, string destination, DateTime? modificationTime, int ownerId = 0, int groupId = 0) {
        TarHeader header = new(WriterOptions.ArchiveEncoding) {
            LastModifiedTime = modificationTime ?? TarHeader.EPOCH,
            Name             = NormalizeFilename(source),
            LinkName         = NormalizeFilename(destination),
            UserId           = ownerId,
            GroupId          = groupId,
            EntryType        = EntryType.SymLink,
            Mode             = 511 //0o777
        };

        header.Write(OutputStream);
    }

    protected void PadTo512(long size) {
        int        length = unchecked((int) (((size + 511L) & ~511L) - size));
        Span<byte> zeroes = stackalloc byte[length];

        OutputStream.Write(zeroes);
    }

    protected static string NormalizeFilename(string filename) {
        filename = filename.Replace('\\', '/');

        int pos = filename.IndexOf(':');
        if (pos >= 0) {
            filename = filename.Remove(0, pos + 1);
        }

        return filename.Trim('/');
    }

}