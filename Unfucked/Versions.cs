namespace Unfucked;

/// <summary>
/// Methods that make it easier to work with version numbers.
/// </summary>
public static class Versions {

    /// <summary>Print the version number as a string, optionally allowing the caller to specify that trailing zero fields should be trimmed. Never throws an exception if the version was initialized with fewer than the requested number of fields.</summary>
    /// <param name="version">A version number to serialize.</param>
    /// <param name="maxFieldCount">The maximum number of version components (the numbers between the periods) to print. Fewer may be printed if <paramref name="trimEndingZeros"/> is <c>true</c>. Can be more than the number of fields <paramref name="version"/> was initialized with.</param>
    /// <param name="trimEndingZeros">When <c>true</c>, all trailing <c>".0"</c> instances will be trimmed from the end of the output string, otherwise, they will be preserved. The major (most significant) version number will never be trimmed, regardless of this argument.</param>
    /// <returns>A stringified representation of <paramref name="version"/>, with at most <paramref name="maxFieldCount"/> fields. If <paramref name="trimEndingZeros"/> is <c>true</c>, it will not end with <c>".0"</c>.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="maxFieldCount"/> is not in the range [1,4].</exception>
    public static string ToString(this Version version, int maxFieldCount, bool trimEndingZeros) {
        switch (maxFieldCount) {
            case > 4:
                throw new ArgumentOutOfRangeException(nameof(maxFieldCount), maxFieldCount, "Version objects have at most 4 fields");
            case < 1:
                throw new ArgumentOutOfRangeException(nameof(maxFieldCount), maxFieldCount, "Must format at least one number");
        }

        Version normalizedVersion =
            (maxFieldCount == 4 && version.Revision == -1) ||
            (maxFieldCount >= 3 && version.Build == -1)
                ? new Version(
                    version.Major,
                    version.Minor,
                    version.Build == -1 ? 0 : version.Build,
                    version.Revision == -1 ? 0 : version.Revision
                )
                : version;

        if (trimEndingZeros) {
            if (maxFieldCount == 4 && normalizedVersion.Revision == 0) {
                maxFieldCount--;
            }
            if (maxFieldCount == 3 && normalizedVersion.Build == 0) {
                maxFieldCount--;
            }
            if (maxFieldCount == 2 && normalizedVersion.Minor == 0) {
                maxFieldCount--;
            }
        }
        return normalizedVersion.ToString(maxFieldCount);
    }

}