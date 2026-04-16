using System.Diagnostics;
using System.Reflection;

namespace Unfucked;

/// <summary>
/// Methods that make it easier to work with version numbers.
/// </summary>
public static class Versions {

    extension(Version version) {

        /// <summary>
        /// <para>Print the version number as a string, allowing the caller to specify the minimum and maximum quantity of components/fields to print, trimming trailing zero components above the minimum.</para>
        /// <para>Never throws an exception if the version was initialized with fewer than the requested number of fields.</para>
        /// <para>Examples:
        /// <code>
        /// Version.Parse("1.0").ToString(4, 4) → "1.0.0.0" // does not throw an exception
        /// Version.Parse("1.0.0.0").ToString(1, 4) → "1" // trims trailing zeros above min
        /// Version.Parse("1.2.3.4").ToString(1, 4) → "1.2.3.4" // trims trailing zeros above min
        /// </code></para>
        /// </summary>
        /// <param name="minComponents">The minimum quantity of version number components to print. This method will always print at least this many components, even if they are zero or missing. Must be in the range [1,4]. Can be more than the number of components <paramref name="version"/> was initialized with.</param>
        /// <param name="maxComponents">The maximum quantity of version number components to print. This method will never print more than this many components, but fewer may be printed if <paramref name="minComponents"/> is less than <paramref name="maxComponents"/> and at least one of the trailing components is <c>0</c>. Can be more than the number of components <paramref name="version"/> was initialized with.</param>
        /// <returns>A stringified representation of <paramref name="version"/>, with between <paramref name="minComponents"/> and <paramref name="maxComponents"/> fields.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="minComponents"/> or <paramref name="maxComponents"/> are outside the range [1,4].</exception>
        public string ToString(int minComponents, int maxComponents) {
            switch (minComponents) {
                case < 1:
                    throw new ArgumentOutOfRangeException(nameof(minComponents), minComponents, "Must format at least one number");
                case > 4:
                    throw new ArgumentOutOfRangeException(nameof(minComponents), minComponents, "Version objects have at most 4 fields");
            }
            switch (maxComponents) {
                case < 1:
                    throw new ArgumentOutOfRangeException(nameof(maxComponents), maxComponents, "Must format at least one number");
                case > 4:
                    throw new ArgumentOutOfRangeException(nameof(maxComponents), maxComponents, "Version objects have at most 4 fields");
            }

            if (minComponents > maxComponents) {
                (minComponents, maxComponents) = (maxComponents, minComponents);
            }

            int nonZeroFieldCount = 1;
            int definedFields     = 1;
            for (int i = 1; i < maxComponents; i++) {
                int fieldValue = getVersionField(version, i);
                if (fieldValue != -1) {
                    definedFields++;
                    if (fieldValue != 0) {
                        nonZeroFieldCount++;
                    }
                } else {
                    break;
                }
            }

            Version normalizedVersion = definedFields >= minComponents ? version : new Version(
                version.Major,
                version.Minor,
                definedFields < 3 ? 0 : version.Build,
                definedFields < 4 ? 0 : version.Revision);

            return normalizedVersion.ToString(Math.Max(minComponents, nonZeroFieldCount));

            static int getVersionField(Version version, int fieldIndex) => fieldIndex switch {
                0 => version.Major,
                1 => version.Minor,
                2 => version.Patch,
                3 => version.Revision,
                _ => -2
            };
        }

        // ExceptionAdjustment: P:System.Diagnostics.Process.MainModule get -T:System.ComponentModel.Win32Exception
        /// <summary>If the program was launched with the <c>-v</c> or <c>--version</c> arguments, print the program product or file version to stdout, then exit with code 0, otherwise, do nothing.</summary>
        /// <param name="printAndExit">When <c>false</c>, just return the program version. When <c>true</c>, print the version to stdout and immediately exit with code 0.</param>
        /// <returns>The current program's product or file version, or <c>null</c> if the command-line arguments do not contain <c>-v</c> or <c>--version</c>.</returns>
        public static string? GetProgramVersion(bool printAndExit = false) {
            string[] args           = Environment.GetCommandLineArgs();
            string?  programVersion = null;
            if (args.Contains("--version") || args.Contains("-v")) {
                Assembly? assembly = Assembly.GetEntryAssembly();
                programVersion ??= assembly?.GetCustomAttributes<AssemblyInformationalVersionAttribute>().FirstOrDefault()?.InformationalVersion;
                programVersion ??= assembly?.GetName().Version?.ToString(4).TrimEnd(1, ".0");

                if (programVersion is null) {
                    using Process selfProcess = Process.GetCurrentProcess();
                    programVersion ??= selfProcess.MainModule?.FileVersionInfo.ProductVersion;
                    programVersion ??= selfProcess.MainModule?.FileVersionInfo.FileVersion?.TrimEnd(1, ".0");
                }

                programVersion ??= "?";

                if (printAndExit) {
                    Console.WriteLine(programVersion);
                    Environment.Exit(0);
                }
            }

            return programVersion;
        }

        /// <summary>The third component of the version number, or -1 if it is not defined.</summary>
        /// <seealso cref="Version.Build"/>
        public int Patch => version.Build;

    }

}