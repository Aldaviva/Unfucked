using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.FileProviders.Physical;
using Microsoft.Extensions.Hosting;
using Unfucked.DI;
#if !NET6_0_OR_GREATER
using System.Reflection;
using System.Diagnostics;
#endif

namespace Unfucked;

/// <summary>
/// Methods that make it easier to work with the <c>Microsoft.Extensions.Hosting</c> dependency injection/inversion of control library, which is used in the Generic Host and ASP.NET Core.
/// </summary>
public static partial class DependencyInjectionExtensions {

    /// <summary>
    /// <para>By default, the .NET host only looks for CWD configuration files in the working directory, not the installation directory, which breaks when you run the program from any other directory.</para>
    /// <para>Fix this by also looking for CWD JSON configuration files in the same directory as this executable.</para>
    /// </summary>
    /// <param name="builder">see <c>HostApplicationBuilder.Configuration</c></param>
    /// <returns>the same <see cref="IConfigurationBuilder"/> for chaining</returns>
    // ExceptionAdjustment: M:System.Collections.Generic.IList`1.Insert(System.Int32,`0) -T:System.NotSupportedException
    // ExceptionAdjustment: P:System.Diagnostics.Process.MainModule get -T:System.ComponentModel.Win32Exception
    public static IConfigurationBuilder AlsoSearchForJsonFilesInExecutableDirectory(this IConfigurationBuilder builder) {
        string? installationDir;
        try {
            string? processPath =
#if NET6_0_OR_GREATER
                Environment.ProcessPath;
#else
                Assembly.GetEntryAssembly()?.Location;
            if (processPath == null) {
                using Process currentProcess = Process.GetCurrentProcess();
                processPath = currentProcess.MainModule!.FileName;
            }
#endif
            installationDir = Path.GetDirectoryName(processPath);
        } catch (PathTooLongException) {
            return builder;
        }

        if (installationDir != null && !installationDir.Equals(Environment.CurrentDirectory, StringComparison.FilesystemPaths)) {
            PhysicalFileProvider fileProvider = new(installationDir, ExclusionFilters.None);

            IEnumerable<(int index, IConfigurationSource source)> sourcesToAdd = builder.Sources.SelectMany<IConfigurationSource, (int, IConfigurationSource)>((src, oldIndex) =>
                src is JsonConfigurationSource { Path: {} path } source ? [
                    (oldIndex, new JsonConfigurationSource {
                        FileProvider   = fileProvider,
                        Path           = path,
                        Optional       = true,
                        ReloadOnChange = source.ReloadOnChange,
                        ReloadDelay    = source.ReloadDelay
                    })
                ] : []).ToList();

            int sourcesAdded = 0;
            foreach ((int index, IConfigurationSource source) in sourcesToAdd) {
                builder.Sources.Insert(index + sourcesAdded++, source);
            }
        }

        return builder;
    }

    /// <summary>
    /// <para>Mutate all existing physical file configuration sources to allow them to load hidden, system, and dot files without ignoring them or crashing. Does not affect subsequently added configuration sources, so call this last.</para>
    /// <para>This won't help if it is called after a required hidden file has already been added, because that will have crashed before calling this method. To fix this, either call <see cref="AddJsonFile"/> and pass <c>true</c> as the <c>includeHidden</c> argument, or pass a custom <see cref="PhysicalFileProvider"/> to <see cref="JsonConfigurationExtensions.AddJsonFile(IConfigurationBuilder,IFileProvider,string,bool,bool)"/> with its <c>filters</c> constructor argument set to <see cref="ExclusionFilters.None"/>.</para>
    /// </summary>
    /// <param name="builder">The <c>Configuration</c> property value of the <see cref="HostApplicationBuilder"/> or other application builder.</param>
    /// <seealso cref="AddJsonFile"/>
    public static IConfigurationBuilder UnignoreHiddenFiles(this IConfigurationBuilder builder) {
        IDictionary<PhysicalFileProvider, PhysicalFileProvider> replacementProviders = new Dictionary<PhysicalFileProvider, PhysicalFileProvider>();
        foreach (FileConfigurationSource source in builder.Sources.OfType<FileConfigurationSource>()) {
            if (source.FileProvider is PhysicalFileProvider fileProvider && !replacementProviders.Values.Contains(fileProvider)) {
                source.FileProvider = replacementProviders.GetOrAdd(fileProvider, static replaced => new PhysicalFileProvider(replaced.Root, ExclusionFilters.None), out bool _);
            }
        }
        foreach (PhysicalFileProvider replaced in replacementProviders.Keys) {
            replaced.Dispose();
        }
        (builder as IConfigurationRoot)?.Reload();
        return builder;
    }

    /// <inheritdoc cref="JsonConfigurationExtensions.AddJsonFile(IConfigurationBuilder,string,bool,bool)" />
    /// <param name="includeHidden">If <c>false</c> (default), hidden, system, and dot files will be ignored and will not be loaded, causing a crash if <paramref name="optional"/> is <c>true</c>. Otherwise, if <c>true</c>, then hidden, system, and dot files will be treated normally and loaded properly.</param>
    /// <seealso cref="UnignoreHiddenFiles"/>
    public static IConfigurationBuilder AddJsonFile(this IConfigurationBuilder builder, string path, bool optional, bool reloadOnChange, bool includeHidden = false) {

        return includeHidden
            ? builder.AddJsonFile(source => {
                source.Path           = path;
                source.Optional       = optional;
                source.ReloadOnChange = reloadOnChange;
                resolveFileProvider(source);
            })
            : JsonConfigurationExtensions.AddJsonFile(builder, path, optional, reloadOnChange);

        // Copied from Microsoft.Extensions.Configuration.FileConfigurationSource.ResolveFileProvider(), which stupidly hardcodes always excluding hidden/sensitive files
        static void resolveFileProvider(JsonConfigurationSource source) {
            const ExclusionFilters filter = ExclusionFilters.None;

            if (source.FileProvider == null && !string.IsNullOrEmpty(source.Path) && Path.IsPathRooted(source.Path)) {
                string? directory  = Path.GetDirectoryName(source.Path);
                string  pathToFile = Path.GetFileName(source.Path);
                while (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory)) {
                    pathToFile = Path.Combine(Path.GetFileName(directory), pathToFile);
                    directory  = Path.GetDirectoryName(directory);
                }
                if (Directory.Exists(directory)) {
                    source.FileProvider = new PhysicalFileProvider(directory, filter);
                    source.Path         = pathToFile;
                }
            }
        }
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

    /// <summary>
    /// <para>By default in .NET 6 and later, an uncaught exception in a <see cref="BackgroundService"/> will log a critical error and cause the host application to exit with status code 0. This makes it very difficult to automatically determine if the application crashed, such as when it's run from a script or Task Scheduler.</para>
    /// <para>This extension allows you to change the exit code returned by this program when it exits due to a <see cref="BackgroundService"/> throwing an exception. By default, this will return 1 on exceptions, but you can customize the exit code too. The exit code is only changed if a <see cref="BackgroundService"/> threw an exception, so the program will still exit with 0 normally.</para>
    /// <para>Usage:</para>
    /// <para><code>builder.Services.SetExitCodeOnBackgroundServiceException(1);</code></para>
    /// </summary>
    /// <param name="services">From <see cref="HostApplicationBuilder.Services"/> or similar.</param>
    /// <param name="exitCode">The numeric status code you want the application to exit with when a <see cref="BackgroundService"/> throws an uncaught exception. To customize the exit code for different exceptions, use the overload that takes a function for this parameter.</param>
    public static IServiceCollection SetExitCodeOnBackgroundServiceException(this IServiceCollection services, int exitCode = 1) => services.SetExitCodeOnBackgroundServiceException(_ => exitCode);

    /// <summary>
    /// <para>By default in .NET 6 and later, an uncaught exception in a <see cref="BackgroundService"/> will log a critical error and cause the host application to exit with status code 0. This makes it very difficult to automatically determine if the application crashed, such as when it's run from a script or Task Scheduler.</para>
    /// <para>This extension allows you to change the exit code returned by this program when it exits due to a <see cref="BackgroundService"/> throwing an exception. By default, this will return 1 on exceptions, but you can customize the exit code too. The exit code is only changed if a <see cref="BackgroundService"/> threw an exception, so the program will still exit with 0 normally.</para>
    /// <para>Usage:</para>
    /// <para><code>builder.Services.SetExitCodeOnBackgroundServiceException(exception => exception is ApplicationException ? 1 : 2);</code></para>
    /// </summary>
    /// <param name="services">From <see cref="HostApplicationBuilder.Services"/> or similar.</param>
    /// <param name="exitCodeGenerator">A function that takes the <see cref="Exception"/> thrown by the <see cref="BackgroundService"/> and returns the status code to exit this process with.</param>
    public static IServiceCollection SetExitCodeOnBackgroundServiceException(this IServiceCollection services, Func<Exception, int> exitCodeGenerator) {
        services.AddHostedService(context => new BackgroundServiceExceptionListener(context, exitCodeGenerator));
        return services;
    }

    internal sealed class BackgroundServiceExceptionListener(IServiceProvider services, Func<Exception, int> exitCodeGenerator): BackgroundService {

        protected override Task ExecuteAsync(CancellationToken stoppingToken) {
            IEnumerable<BackgroundService> backgroundServices =
                services.GetServices<IHostedService>().OfType<BackgroundService>().Where(static service => service is not BackgroundServiceExceptionListener);

            services.GetRequiredService<IHostApplicationLifetime>().ApplicationStopped.Register(() => {
                if (backgroundServices.Select(static service => service.ExecuteTask?.Exception?.InnerException).FirstOrDefault() is {} exception) {
                    Environment.ExitCode = exitCodeGenerator(exception);
                }
            });

            return Task.CompletedTask;
        }

    }

}