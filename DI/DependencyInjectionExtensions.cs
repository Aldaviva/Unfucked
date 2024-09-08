using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;
using Unfucked.DI;
#if !NET6_0_OR_GREATER
using System.Diagnostics;
using System.Reflection;
#endif

namespace Unfucked;

/// <summary>
/// Methods that make it easier to work with the <c>Microsoft.Extensions.Hosting</c> dependency injection/inversion of control library, which is used in the Generic Host and ASP.NET Core.
/// </summary>
public static class DependencyInjectionExtensions {

    /// <summary>
    /// <para>Format console logging output using <see cref="ConsoleFormatter"/>, which prints messages with a level character (like 'i'), ISO 8601-like date and time with milliseconds, class (by default the simple name), message, and any stack trace, separated by vertical pipes (' | '), for example:</para>
    /// <para><c> i | 2024-09-08 13:27:00.000 | Program | Application starting</c></para>
    /// </summary>
    /// <param name="logging">Application builder's <see cref="HostApplicationBuilder.Logging"/>.</param>
    /// <param name="options">Options to pass to the formatter to disable colored output, show fully-qualified class names, change the column separator, or change the datetime format.</param>
    public static ILoggingBuilder AddUnfuckedConsole(this ILoggingBuilder logging, Action<ConsoleFormatter.ConsoleFormatterOptions>? options = null) {
        logging.AddConsole(opts => opts.FormatterName = ConsoleFormatter.Id);
        if (options != null) {
            logging.AddConsoleFormatter<ConsoleFormatter, ConsoleFormatter.ConsoleFormatterOptions>(options);
        } else {
            logging.AddConsoleFormatter<ConsoleFormatter, ConsoleFormatter.ConsoleFormatterOptions>();
        }
        return logging;
    }

    /// <summary>
    /// <para>By default, the .NET host only looks for configuration files in the working directory, not the installation directory, which breaks when you run the program from any other directory.</para>
    /// <para>Fix this by also looking for JSON configuration files in the same directory as this executable.</para>
    /// </summary>
    /// <param name="builder">see <c>HostApplicationBuilder.Configuration</c></param>
    /// <returns>the same <see cref="IConfigurationBuilder"/> for chaining</returns>
    // ExceptionAdjustment: M:System.Collections.Generic.IList`1.Insert(System.Int32,`0) -T:System.NotSupportedException
    // ExceptionAdjustment: P:System.Diagnostics.Process.MainModule get -T:System.ComponentModel.Win32Exception
    [ExcludeFromCodeCoverage]
    public static IConfigurationBuilder AlsoSearchForJsonFilesInExecutableDirectory(this IConfigurationBuilder builder) {
        string? installationDir;
        try {
            string? processPath;
#if NET6_0_OR_GREATER
            processPath = Environment.ProcessPath;
#else
            processPath = Assembly.GetEntryAssembly()?.Location;
            if (processPath == null) {
                using Process currentProcess = Process.GetCurrentProcess();
                processPath = currentProcess.MainModule!.FileName;
            }
#endif
            installationDir = Path.GetDirectoryName(processPath);
        } catch (PathTooLongException) {
            return builder;
        }

        if (installationDir != null) {
            PhysicalFileProvider fileProvider = new(installationDir);

            IEnumerable<(int index, IConfigurationSource source)> sourcesToAdd = builder.Sources.SelectMany<IConfigurationSource, (int, IConfigurationSource)>((src, oldIndex) =>
                src is JsonConfigurationSource { Path: { } path } source
                    ? [
                        (oldIndex, new JsonConfigurationSource {
                            FileProvider   = fileProvider,
                            Path           = path,
                            Optional       = true,
                            ReloadOnChange = source.ReloadOnChange,
                            ReloadDelay    = source.ReloadDelay
                        })
                    ]
                    : []).ToList();

            int sourcesAdded = 0;
            foreach ((int index, IConfigurationSource? source) in sourcesToAdd) {
                builder.Sources.Insert(index + sourcesAdded++, source);
            }
        }

        return builder;
    }

    /// <summary>
    /// <para>Declarative injection of dependencies with shorter lifetimes into dependents with longer lifetimes, like <c>javax.inject.Provider&lt;T&gt;</c>, without the complication of creating scopes, so you don't have a inject an <see cref="IServiceProvider"/> and imperatively request everything, which isn't very DI-like.</para>
    /// <para>Configure dependency injection context to allow you to inject <see cref="Provider{T}"/> instances into your dependent services.</para>
    /// <para>This allows you to inject not an instance of a dependency service into your consumer, but rather a factory method that lazily provides the dependency service when called in your dependent service.</para>
    /// <para>This is useful when the lifetime of the dependent is longer than the lifetime of the dependency, and you want the dependency to get cleaned up. For example, a singleton may depend on a prototype-scoped service that must be eagerly cleaned up after it's used to avoid leaking memory, or because the dependency cannot be reused.</para>
    /// <para>Register: <c>
    /// appBuilder.Services
    ///     .AddInjectableProviders()
    ///     .AddTransient&lt;MyDependency&gt;()
    ///     .AddSingleton&lt;MyDependent&gt;();
    /// </c></para>
    /// <para>Inject: <c>
    /// public class MyDependent(Provider&lt;MyDependency&gt; dependencyProvider) {
    ///     public void Start() {
    ///         using MyDependency dependency = dependencyProvider.Get();
    ///         dependency.Run();
    ///     }
    /// }
    /// </c></para>
    /// </summary>
    /// <param name="services">Application builder's <see cref="HostApplicationBuilder.Services"/>.</param>
    public static IServiceCollection AddInjectableProviders(this IServiceCollection services) {
        services.TryAddSingleton(typeof(Provider<>), typeof(MicrosoftDependencyInjectionServiceProvider<>));
        services.TryAddSingleton(typeof(OptionalProvider<>), typeof(MicrosoftDependencyInjectionServiceProvider<>));
        return services;
    }

}