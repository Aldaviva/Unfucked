namespace Unfucked;

public static class Versions {

    public static string ToString(this Version version, int fieldCount, bool trimEndingZeros) {
        switch (fieldCount) {
            case > 4:
                throw new ArgumentOutOfRangeException(nameof(fieldCount), fieldCount, "Version objects have at most 4 fields");
            case < 1:
                throw new ArgumentOutOfRangeException(nameof(fieldCount), fieldCount, "Must format at least one number");
        }

        Version normalizedVersion =
            (fieldCount == 4 && version.Revision == -1) ||
            (fieldCount >= 3 && version.Build == -1)
                ? new Version(
                    version.Major,
                    version.Minor,
                    version.Build == -1 ? 0 : version.Build,
                    version.Revision == -1 ? 0 : version.Revision
                )
                : version;

        if (trimEndingZeros) {
            if (fieldCount == 4 && normalizedVersion.Revision == 0) {
                fieldCount--;
            }
            if (fieldCount == 3 && normalizedVersion.Build == 0) {
                fieldCount--;
            }
            if (fieldCount == 2 && normalizedVersion.Minor == 0) {
                fieldCount--;
            }
        }
        return normalizedVersion.ToString(fieldCount);
    }

}