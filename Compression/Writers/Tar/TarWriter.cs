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

    /// <summary>
    /// Add a file to the TAR archive. This overload allows you to additionally specify the file permissions mode, owner ID, and group ID.
    /// </summary>
    /// <param name="filename">The name of the file that will appear in the archive for this entry, such as <c>a/b/c/file.txt</c>. If this filename is a path that contains parent directories such as <c>a/b/c/</c>, you should first call <see cref="WriteDirectory"/> on each segment of that path (<c>a</c>, <c>a/b</c>, and <c>a/b/c</c>).</param>
    /// <param name="source">Input byte stream to read from to get the contents of this entry when compressing.</param>
    /// <param name="modificationTime">The date and time this file was last modified, or <c>null</c> to use the start of the Unix epoch (1970-01-01T00:00Z).</param>
    /// <param name="size">The number of bytes in the file, or <c>null</c> to determine the size automatically from <paramref name="source"/> input stream.</param>
    /// <param name="fileMode">Unix file permissions mode to set on this file during extraction, or <c>null</c> to use the most permissive <c>777</c> mode (world readable and writable).</param>
    /// <param name="ownerId">User ID of the owner of this file to set during extraction, or <c>0</c> to inherit the user ID from the parent directory.</param>
    /// <param name="groupId">Group ID of the group of this file to set during extraction, or <c>0</c> to inherit the group ID from the parent directory.</param>
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

    /// <summary>
    /// <para>Add an empty directory/folder to the TAR archive. This overload allows you to additionally specify the file permissions mode, owner ID, and group ID.</para>
    /// <para>Does not recursively create missing parent directories, so if you want to create directory <c>a/b/c</c>, you must first add directories <c>a</c> and <c>a/b</c>.</para>
    /// </summary>
    /// <param name="directoryName">The name of the directory that will appear in the archive for this entry, such as <c>a/b/c</c>.</param>
    /// <param name="modificationTime">The date and time this directory was last modified, or <c>null</c> to use the start of the Unix epoch (1970-01-01T00:00Z).</param>
    /// <param name="directoryMode">Unix file permissions mode to set on this directory during extraction, or <c>null</c> to use the most permissive <c>777</c> mode (world readable and writable).</param>
    /// <param name="ownerId">User ID of the owner of this directory to set during extraction, or <c>0</c> to inherit the user ID from the parent directory on extraction.</param>
    /// <param name="groupId">Group ID of the group of this directory to set during extraction, or <c>0</c> to inherit the group ID from the parent directory on extraction.</param>
    public virtual void WriteDirectory(string directoryName, DateTime? modificationTime, int? directoryMode, int ownerId = 0, int groupId = 0) {
        TarHeader header = new(WriterOptions.ArchiveEncoding) {
            LastModifiedTime = modificationTime ?? TarHeader.EPOCH,
            Name             = NormalizeFilename(directoryName),
            UserId           = ownerId,
            GroupId          = groupId,
            EntryType        = EntryType.Directory
        };
        if (directoryMode.HasValue) {
            header.Mode = directoryMode.Value;
        }

        header.Write(OutputStream);
    }

    /// <summary>
    /// <para>Add a relative symbolic link to the TAR archive. This overload allows you to additionally specify the owner ID and group ID.</para>
    /// <para>Does not handle absolute symlinks.</para>
    /// </summary>
    /// <param name="source">The name of the new symbolic link to create, possibly with parent directories in the path.</param>
    /// <param name="destination">What the symbolic link should point to, possibly with parent directories and <c>..</c> in the path.</param>
    /// <param name="modificationTime">The date and time this symlink was last modified, or <c>null</c> to use the start of the Unix epoch (1970-01-01T00:00Z).</param>
    /// <param name="ownerId">User ID of the owner of this symlink to set during extraction, or <c>0</c> to inherit the user ID from the parent directory on extraction.</param>
    /// <param name="groupId">Group ID of the group of this symlink to set during extraction, or <c>0</c> to inherit the group ID from the parent directory on extraction.</param>
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

    /// <summary>
    /// For an<c>TarWriter.OutputStream</c> of size <paramref name="size"/>, write enough null bytes to the stream to make its new length an integer multiple of 512.
    /// </summary>
    /// <param name="size">Existing length of <c>TarWriter.OutputStream</c>.</param>
    protected void PadTo512(long size) {
        int        length = unchecked((int) (((size + 511L) & ~511L) - size));
        Span<byte> zeroes = stackalloc byte[length];

        OutputStream.Write(zeroes);
    }

    /// <summary>
    /// Convert filenames from Windows to Unix format, with no backslashes or colons. Also trim leading and trailing forward slashes.
    /// </summary>
    /// <param name="filename">Possibly Windows-style filename.</param>
    /// <returns>Normalized filename</returns>
    protected static string NormalizeFilename(string filename) {
        filename = filename.Replace('\\', '/');

        int pos = filename.IndexOf(':');
        if (pos >= 0) {
            filename = filename.Remove(0, pos + 1);
        }

        return filename.Trim('/');
    }

}