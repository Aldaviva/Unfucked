namespace Unfucked;

/// <summary>
/// Methods that make it easier to work with filesystem files.
/// </summary>
public static class Files {

    /// <summary>Deletes the specified file. If the file is read-only, makes it writable and then deletes it. Does not throw an exception for nonexistant files.</summary>
    /// <param name="path">The name of the file to be deleted. Wildcard characters are not supported.</param>
    /// <exception cref="ArgumentException">.NET Framework and .NET Core versions older than 2.1: <paramref name="path" /> is a zero-length string, contains only white space, or contains one or more invalid characters. You can query for invalid characters by using the <see cref="M:System.IO.Path.GetInvalidPathChars" /> method.</exception>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="path" /> is <see langword="null" />.</exception>
    /// <exception cref="DirectoryNotFoundException">The specified path is invalid (for example, it is on an unmapped drive).</exception>
    /// <exception cref="IOException">The specified file is in use.
    /// 
    /// -or-
    /// 
    /// There is an open handle on the file, and the operating system is Windows XP or earlier. This open handle can result from enumerating directories and files. For more information, see How to: Enumerate Directories and Files.</exception>
    /// <exception cref="NotSupportedException"><paramref name="path" /> is in an invalid format.</exception>
    /// <exception cref="PathTooLongException">The specified path, file name, or both exceed the system-defined maximum length.</exception>
    /// <exception cref="UnauthorizedAccessException">The caller does not have the required permission.
    /// 
    /// -or-
    /// 
    /// The file is an executable file that is in use.
    /// 
    /// -or-
    /// 
    /// <paramref name="path" /> is a directory.</exception>
    public static void Delete(string path) {
        try {
            File.Delete(path);
        } catch (UnauthorizedAccessException) {
            FileAttributes oldAttributes = File.GetAttributes(path);
            if ((oldAttributes & FileAttributes.ReadOnly) != 0) {
                File.SetAttributes(path, oldAttributes & ~FileAttributes.ReadOnly);
                File.Delete(path);
            } else {
                throw;
            }
        }
    }

}