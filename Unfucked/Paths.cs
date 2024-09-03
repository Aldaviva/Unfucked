namespace Unfucked;

public static class Paths {

#if NET6_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
    [return: NotNullIfNotNull(nameof(path))]
#endif
    [Pure]
    public static string? TrimSlashes(string? path) => path?.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

    public static string GetTempDirectory(string? parentDir = null) {
        parentDir ??= Path.GetTempPath();

        string tempDirectory;
        do {
            tempDirectory = Path.Combine(parentDir, "temp-" + Cryptography.GenerateRandomString(8));
        } while (Directory.Exists(tempDirectory));

        Directory.CreateDirectory(tempDirectory);

        return tempDirectory;
    }

    /// <summary>
    /// Replace Windows-style directory separators (backslash, \) with Unix-style ones (forward slash, /)
    /// </summary>
    /// <param name="dosPath">A file path that may contain backslashes</param>
    /// <returns>A copy of <paramref name="dosPath"/> with all backslashes replaced with forward slashes</returns>
#if NET6_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
    [return: NotNullIfNotNull(nameof(dosPath))]
#endif
    [Pure]
    public static string? Dos2UnixSlashes(string? dosPath) => dosPath?.Replace('\\', '/');

    /// <summary>
    /// See if a given filename has one of a set of expected extensions
    /// </summary>
    /// <param name="filename">filename or path to file</param>
    /// <param name="allowedExtensions">A set of lowercase file extensions with leading periods. Compound extensions like .tar.gz are not supported. Can be a <c>FrozenSet</c> for faster lookups (<c>new HashSet&lt;string&gt;{ ... }.ToFrozenSet()</c>, which may require the <c>System.Collections.Immutable</c> package in .NET 6 and earlier).</param>
    /// <returns><c>true</c> if the file extension is in <paramref name="allowedExtensions"/>, <c>false</c> otherwise</returns>
    public static bool MatchesExtensions(string filename, IEnumerable<string> allowedExtensions) {
        // don't use Contains(string, IEqualityComparer<string>) because that's an O(N) operation, in case we have a fast FrozenSet in allowedExtensions
        return allowedExtensions.Contains(Path.GetExtension(filename).ToLowerInvariant());
    }

}