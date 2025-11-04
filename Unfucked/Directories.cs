namespace Unfucked;

/// <summary>
/// Methods that make it easier to work with filesystem directories.
/// </summary>
public static class Directories {

    /// <summary>Deletes the specified directory and, if indicated, any subdirectories and files in the directory.</summary>
    /// <param name="directory">The name of the directory to remove.</param>
    /// <param name="recursive"><c>true</c> to remove directories, subdirectories, and files in <paramref name="directory" />; otherwise, <c>false</c>.</param>
    /// <returns><c>true</c> if <paramref name="directory"/> was deleted, or <c>false</c> if it was not found.</returns>
    /// <exception cref="IOException">A file with the same name and location specified by <paramref name="directory" /> exists.
    /// 
    /// -or-
    /// 
    /// The directory specified by <paramref name="directory" /> is read-only, or <paramref name="recursive" /> is <c>false</c> and <paramref name="directory" /> is not an empty directory.
    /// 
    /// -or-
    /// 
    /// The directory is the application's current working directory.
    /// 
    /// -or-
    /// 
    /// The directory contains a read-only file.
    /// 
    /// -or-
    /// 
    /// The directory is being used by another process.</exception>
    /// <exception cref="UnauthorizedAccessException">The caller does not have the required permission.</exception>
    /// <exception cref="ArgumentException">.NET Framework and .NET Core versions older than 2.1: <paramref name="directory" /> is a zero-length string, contains only white space, or contains one or more invalid characters. You can query for invalid characters by using the <see cref="M:System.IO.Path.GetInvalidPathChars" /> method.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="directory" /> is <c>null</c>.</exception>
    /// <exception cref="PathTooLongException">The specified path, file name, or both exceed the system-defined maximum length.</exception>
    public static bool TryDelete(string directory, bool recursive = false) {
        try {
            Directory.Delete(directory, recursive);
            return true;
        } catch (DirectoryNotFoundException) {
            return false;
        }
    }

}