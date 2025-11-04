namespace Unfucked;

/// <summary>
/// Methods that make it easier to work with filesystem path names.
/// </summary>
public static class Paths {

    /// <summary>
    /// <para>Remove trailing slashes from a path.</para>
    /// <para>By default, the trailing slashes that are trimmed are dependent on the operating system that your program is running on. On Windows, forwards and backslashes will be trimmed, but only forward slashes will be trimmed on all other operating systems. To always trim backslashes on all operating systems, for example to output Windows-compatible paths on a Linux computer, set <paramref name="forceTrimBackslashes"/> to <c>true</c>.</para>
    /// </summary>
    /// <param name="path">Path to remove trailing slashes from.</param>
    /// <param name="forceTrimBackslashes">On non-Windows operating systems, also trim trailing backslashes from paths, not just trailing forward slashes. Has no effect on Windows, where trailing backslashes are always trimmed, regardless of this argument.</param>
    /// <returns>The same path as the <paramref name="path"/> argument, but without any trailing slashes.</returns>
#if NETCOREAPP3_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
    [return: NotNullIfNotNull(nameof(path))]
#endif
    [Pure]
    public static string? TrimTrailingSlashes(string? path, bool forceTrimBackslashes = false) =>
        path?.TrimEnd(Path.DirectorySeparatorChar, forceTrimBackslashes ? '\\' : Path.AltDirectorySeparatorChar);

    /// <summary>
    /// Create a new, empty subdirectory in a given parent directory with a random, 8-character alphanumeric name that starts with <c>temp-</c>, such as <c>temp-Hi884Xbd</c>.
    /// </summary>
    /// <param name="parentDir">Directory in which to create this new subdirectory. If <c>null</c>, it will be created in the OS user's temporary directory (generally either <c>%TEMP%</c> on Windows, or <c>%TMPDIR%</c> or <c>/tmp</c> on Linux).</param>
    /// <returns>Absolute path to the newly-created, empty directory.</returns>
    /// <remarks>See also <see cref="Path.GetTempPath"/></remarks>
    public static string CreateTempDir(string? parentDir = null) {

        parentDir ??= Path.GetTempPath();

        string tempDirectory;
        do {
            tempDirectory = Path.Combine(parentDir, "temp-" + Cryptography.GenerateRandomString(8));
        } while (Directory.Exists(tempDirectory));

        Directory.CreateDirectory(tempDirectory);

        return Path.GetFullPath(tempDirectory);
    }

    /// <summary>
    /// Replace Windows-style directory separators (backslash, <c>\</c>) with Unix-style ones (forward slash, <c>/</c>).
    /// </summary>
    /// <param name="dosPath">A file path that may contain backslashes.</param>
    /// <returns>A copy of <paramref name="dosPath"/> with all backslashes replaced with forward slashes.</returns>
#if NETCOREAPP3_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
    [return: NotNullIfNotNull(nameof(dosPath))]
#endif
    [Pure]
    public static string? Dos2UnixSlashes(string? dosPath) => dosPath?.Replace('\\', '/');

    /// <summary>
    /// Test if a given filename has one of a set of expected extensions.
    /// </summary>
    /// <param name="filename">Filename or path to file.</param>
    /// <param name="allowedExtensions">A set of lowercase file extensions with or without leading periods. Compound extensions like .tar.gz are not supported. Can be a <c>FrozenSet</c> for faster lookups (<c>new HashSet&lt;string&gt;{ ... }.ToFrozenSet()</c>, which may require the <c>System.Collections.Immutable</c> package in .NET 6 and earlier).</param>
    /// <returns><c>true</c> if the file extension is in <paramref name="allowedExtensions"/>, <c>false</c> otherwise</returns>
    [Pure]
    public static bool MatchesExtensions(string filename,
#if NET5_0_OR_GREATER
                                         IReadOnlySet<string>
#else
                                         ISet<string>
#endif
                                             allowedExtensions) {
        // don't use Contains(string, IEqualityComparer<string>) because that's an O(N) operation, in case we have a fast FrozenSet in allowedExtensions
        string actualExtensionWithLeadingPeriod = Path.GetExtension(filename).ToLowerInvariant();
        return allowedExtensions.Contains(actualExtensionWithLeadingPeriod) || allowedExtensions.Contains(actualExtensionWithLeadingPeriod.TrimStart('.'));
    }

}