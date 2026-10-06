using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
#if NET9_0_OR_GREATER
using System.Buffers;
#endif

namespace Unfucked;

/// <summary>
/// Methods that make it easier to work with version numbers.
/// </summary>
public static class Versions {

    private static readonly Lazy<string?> PROGRAM_VERSION = new(static () => {
        string?   programVersion = null;
        Assembly? assembly       = Assembly.GetEntryAssembly();
        programVersion ??= normalizeVersion(assembly?.GetCustomAttributes<AssemblyInformationalVersionAttribute>().FirstOrDefault()?.InformationalVersion);
        programVersion ??= assembly?.GetName().Version?.ToString(1, 4);

        if (programVersion is null) {
            using Process    selfProcess       = Process.GetCurrentProcess();
            FileVersionInfo? mainModuleVersion = selfProcess.MainModule?.FileVersionInfo;
            programVersion ??= normalizeVersion(mainModuleVersion?.ProductVersion);
            programVersion ??= normalizeVersion(mainModuleVersion?.FileVersion);
        }

        if (programVersion?.IndexOf('+') is not -1 and {} plusIndex) {
            programVersion = programVersion.Substring(0, plusIndex);
        }

        static string? normalizeVersion(string? version) {
            string? normalized = version?.IndexOf('+') is not -1 and {} plusIndex ? version.Substring(0, plusIndex) : version;
            return normalized is not null && Version.TryParse(normalized, out Version? result) ? result.ToString(1, 4) : normalized;
        }

        return programVersion;
    }, LazyThreadSafetyMode.PublicationOnly);

#if NET9_0_OR_GREATER
    private static readonly SearchValues<string> VERSION_ARGUMENTS = SearchValues.Create(
#else
    private static readonly string[] VERSION_ARGUMENTS =
#endif
            ["--version", "-v", "-version"]
#if NET9_0_OR_GREATER
            , StringComparison.Ordinal)
#endif
        ;

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

        /// <summary>The current program's product or file version.</summary>
        public static string? ProgramVersion => PROGRAM_VERSION.Value;

        /// <summary>If the program was launched with the <c>-v</c>, <c>-version</c>, or <c>--version</c> arguments, print the program's product/file version and build date to stdout (or a MessageBox for Windows GUI programs), then exit with code 0; otherwise, do nothing.</summary>
        public static void PrintProgramVersionAndExitIfRequested() {
            string[] args = Environment.GetCommandLineArgs();

            bool userRequestedVersion =
#if NET8_0_OR_GREATER
                args.ContainsAny(VERSION_ARGUMENTS);
#else
                args.Intersect(VERSION_ARGUMENTS).Any();
#endif

            if (userRequestedVersion) {
                bool isGui = Environment.IsWindowsGuiProgram;
                StringBuilder versionTextBuilder = new StringBuilder("Version:")
                    .Append(isGui ? "  " : " ")
                    .Append(Version.ProgramVersion);
                if (BuildInfoAttribute.Get() is {} buildInfo) {
                    if (buildInfo.CommitHash is not null) {
                        versionTextBuilder.AppendLine()
                            .Append("Commit:")
                            .Append(isGui ? " " : "  ")
                            .Append(buildInfo.CommitHash);
                    }

                    DateTimeOffset buildDate = buildInfo.BuildDate.ToLocalTime();
                    versionTextBuilder.AppendLine()
                        .Append("Built:")
                        .Append(isGui ? "                   " : "   ")
                        .Append(buildDate.ToString("F"))
                        .Append(" (")
                        .Append(buildDate.ToString("zzzz"))
                        .Append(')');
                }

                if (isGui) {
                    using Process currentProcess = Process.GetCurrentProcess();
                    Assembly?     entryAssembly  = Assembly.GetEntryAssembly();
                    string programName = entryAssembly?.GetCustomAttributes<AssemblyProductAttribute>().FirstOrDefault()?.Product.EmptyToNull
                        ?? entryAssembly?.GetCustomAttributes<AssemblyTitleAttribute>().FirstOrDefault()?.Title.EmptyToNull
                        ?? currentProcess.MainWindowTitle.EmptyToNull
#if NET6_0_OR_GREATER
                        ?? Path.GetFileNameWithoutExtension(Environment.ProcessPath)
#endif
                        ?? currentProcess.ProcessName;

                    _ = MessageBoxW(currentProcess.MainWindowHandle, versionTextBuilder.ToString(), programName, 0x40 /* info icon */);
                } else {
                    Console.WriteLine(versionTextBuilder.ToString());
                }
                Environment.Exit(0);
            }
        }

        /// <summary>The third component of the version number, or -1 if it is not defined.</summary>
        /// <seealso cref="Version.Build"/>
        public int Patch => version.Build;

    }

    // Avoid creating a separate Windows-specific TFM for this project just to call one Win32 API method, because it poisons the dependent chain and often breaks tests
    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true, ExactSpelling = true)]
    private static extern int MessageBoxW(IntPtr ownerWindow, string body, string title, uint type);

    /// <summary>Metadata about the program's build, automatically generated at compile time.</summary>
    /// <param name="buildDate">When the program was built, in ISO 8601 ("O") format, like <c>2026-10-05T22:50:53.5265278+00:00</c></param>
    /// <param name="commitHash">Source Code Management identifier in lowercase for the commit when the program was built, like <c>69c08f95d44d4cd706ae2b1c1daabf45728d2878</c>, or <c>null</c> if the project was not version controlled by Git</param>
    [EditorBrowsable(EditorBrowsableState.Advanced)]
    public sealed class BuildInfoAttribute(string buildDate, string? commitHash = null): Attribute {

        /// <summary>When the program was built, in ISO 8601 ("O") format, like <c>2026-10-05T22:50:53.5265278+00:00</c></summary>
        public DateTimeOffset BuildDate { get; } = DateTimeOffset.ParseExact(buildDate, "O", CultureInfo.InvariantCulture);

        /// <summary>Source Code Management identifier in lowercase for the commit when the program was built, like <c>69c08f95d44d4cd706ae2b1c1daabf45728d2878</c>, or <c>null</c> if the project was not version controlled by Git</summary>
        public string? CommitHash { get; } = commitHash;

        /// <summary>Look up a build metadata instance.</summary>
        /// <param name="assembly">The assembly of the program. Defaults to <see cref="Assembly.GetEntryAssembly"/>.</param>
        /// <returns>The <see cref="BuildInfoAttribute"/> instance that was generated at compile time, or <c>null</c> if it was not generated (such as if the <c>Unfucked</c> package was not a dependency of the build).</returns>
        public static BuildInfoAttribute? Get(Assembly? assembly = null) =>
            (assembly ?? Assembly.GetEntryAssembly())?.GetCustomAttributes<BuildInfoAttribute>().FirstOrDefault();

    }

}