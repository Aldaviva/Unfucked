using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;
#if !NET6_0_OR_GREATER
using System.Diagnostics;
using System.Reflection;
#endif

namespace Unfucked;

public static class DependencyInjection {

    public static ILoggingBuilder AddUnfuckedConsole(this ILoggingBuilder logging, Action<ConsoleFormatter.Options>? options = null) {
        logging.AddConsole(opts => opts.FormatterName = ConsoleFormatter.Id);
        if (options != null) {
            logging.AddConsoleFormatter<ConsoleFormatter, ConsoleFormatter.Options>(options);
        } else {
            logging.AddConsoleFormatter<ConsoleFormatter, ConsoleFormatter.Options>();
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
                // this list instance is not read-only
                builder.Sources.Insert(index + sourcesAdded++, source);
            }
        }

        return builder;
    }

    public static IServiceCollection AddInjectableProviders(this IServiceCollection services) {
        services.TryAddSingleton(typeof(Provider<>), typeof(MicrosoftDependencyInjectionServiceProvider<>));
        services.TryAddSingleton(typeof(OptionalProvider<>), typeof(MicrosoftDependencyInjectionServiceProvider<>));
        return services;
    }

}