namespace Unfucked;

/// <summary>
/// Methods that make it easier to work with filesystem directories.
/// </summary>
public static class Directories {

    /// <summary>Deletes the specified directory and, if indicated, any subdirectories and files in the directory.</summary>
    /// <param name="path">The name of the directory to remove.</param>
    /// <param name="recursive">
    /// <see langword="true" /> to remove directories, subdirectories, and files in <paramref name="path" />; otherwise, <see langword="false" />.</param>
    /// <exception cref="T:System.IO.IOException">A file with the same name and location specified by <paramref name="path" /> exists.
    /// 
    /// -or-
    /// 
    /// The directory specified by <paramref name="path" /> is read-only, or <paramref name="recursive" /> is <see langword="false" /> and <paramref name="path" /> is not an empty directory.
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
    /// <exception cref="T:System.UnauthorizedAccessException">The caller does not have the required permission.</exception>
    /// <exception cref="T:System.ArgumentException">.NET Framework and .NET Core versions older than 2.1: <paramref name="path" /> is a zero-length string, contains only white space, or contains one or more invalid characters. You can query for invalid characters by using the <see cref="M:System.IO.Path.GetInvalidPathChars" /> method.</exception>
    /// <exception cref="T:System.ArgumentNullException">
    /// <paramref name="path" /> is <see langword="null" />.</exception>
    /// <exception cref="T:System.IO.PathTooLongException">The specified path, file name, or both exceed the system-defined maximum length.</exception>
    public static void DeleteQuietly(string path, bool recursive = false) {
        try {
            Directory.Delete(path, recursive);
        } catch (DirectoryNotFoundException) { }
    }

}