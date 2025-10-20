using Microsoft.Extensions.Logging.Console;
using System.Diagnostics.CodeAnalysis;

namespace Unfucked.DI.Logging;

/// <summary>
/// Options to control the <see cref="UnfuckedConsoleFormatter"/>.
/// </summary>
[SuppressMessage("ReSharper", "PropertyCanBeMadeInitOnly.Global")] // users must be able to set these in a builder pattern call where the class instance has already been constructed for them
public class UnfuckedConsoleFormatterOptions: ConsoleFormatterOptions {

    /// <summary>
    /// <para>By default, the type name of the class that emitted the log message is printed as the class' simple name, without its namespace (such as <c>MyClass</c>).</para>
    /// <para>By setting this to <c>true</c>, you can print the full type name including its namespace, such as <c>MyNamespace.MyClass</c>.</para>
    /// </summary>
    public bool IncludeNamespaces { get; set; }

    /// <summary>
    /// String that is printed between each column in log lines, <c>" | "</c> by default.
    /// </summary>
    public string ColumnSeparator { get; set; } = " | ";

    /// <summary>
    /// <para>By default, the console logger prints messages in color depending on the log level (errors are white text on a red background, for example).</para>
    /// <para>You may disable this by setting this property to <c>false</c>, for example, in response to the user passing a <c>--no-color</c> option on the command line.</para>
    /// <para>Color output will also be automatically disabled if the stdout console does not support color, such as on Windows versions before Windows 10 version 1511.</para>
    /// </summary>
    public bool Color { get; set; } = true;

}