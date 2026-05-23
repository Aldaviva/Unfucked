using System.Globalization;
using System.Reflection;

namespace Unfucked;

/// <summary>Metadata about the program's build, automatically generated at compile time.</summary>
/// <param name="buildDate">When the program was built, in ISO 8601 ("O") format</param>
public sealed class BuildInfoAttribute(string buildDate): Attribute {

    /// <summary>When the program was built</summary>
    public DateTimeOffset BuildDate { get; } = DateTimeOffset.ParseExact(buildDate, "O", CultureInfo.InvariantCulture);

    /// <summary>Look up a build metadata instance.</summary>
    /// <param name="assembly">The assembly of the program. Defaults to <see cref="Assembly.GetEntryAssembly"/>.</param>
    /// <returns>The <see cref="BuildInfoAttribute"/> instance that was generated at compile time, or <c>null</c> if it was not generated (such as if the <c>Unfucked</c> package was not a dependency of the build).</returns>
    public static BuildInfoAttribute? Get(Assembly? assembly = null) =>
        (assembly ?? Assembly.GetEntryAssembly())?.GetCustomAttributes<BuildInfoAttribute>().FirstOrDefault();

}