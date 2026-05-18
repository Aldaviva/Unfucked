using System.Globalization;
using System.Reflection;

namespace Unfucked;

public class BuildInfoAttribute(string buildDate): Attribute {

    public DateTimeOffset BuildDate { get; } = DateTimeOffset.ParseExact(buildDate, "O", CultureInfo.InvariantCulture);

    public static BuildInfoAttribute? Get(Assembly? assembly = null) =>
        (assembly ?? Assembly.GetEntryAssembly())?.GetCustomAttributes<BuildInfoAttribute>().FirstOrDefault();

}