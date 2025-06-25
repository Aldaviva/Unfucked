using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Unfucked.DI;
#if !NET6_0_OR_GREATER
using System.Diagnostics;
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
            foreach ((int index, IConfigurationSource source) in sourcesToAdd) {
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

    /// <summary>
    /// <para>By default in .NET 6 and later, an uncaught exception in a <see cref="BackgroundService"/> will log a critical error and cause the host application to exit with status code 0. This makes it very difficult to automatically determine if the application crashed, such as when it's run from a script or Task Scheduler.</para>
    /// <para>This extension allows you to change the exit code returned by this program when it exits due to a <see cref="BackgroundService"/> throwing an exception. By default, this will return 1 on exceptions, but you can customize the exit code too. The exit code is only changed if a <see cref="BackgroundService"/> threw an exception, so the program will still exit with 0 normally.</para>
    /// <para>Usage:</para>
    /// <para><code>builder.Services.SetExitCodeOnBackgroundServiceException(1);</code></para>
    /// </summary>
    /// <param name="services">From <see cref="HostApplicationBuilder.Services"/> or similar.</param>
    /// <param name="exitCode">The numeric status code you want the application to exit with when a <see cref="BackgroundService"/> throws an uncaught exception. To customize the exit code for different exceptions, use the overload that takes a function for this parameter.</param>
    public static IServiceCollection SetExitCodeOnBackgroundServiceException(this IServiceCollection services, int exitCode = 1) => SetExitCodeOnBackgroundServiceException(services, _ => exitCode);

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

    internal class BackgroundServiceExceptionListener(IServiceProvider services, Func<Exception, int> exitCodeGenerator): BackgroundService {

        protected override Task ExecuteAsync(CancellationToken stoppingToken) {
            IEnumerable<BackgroundService> backgroundServices = services.GetServices<IHostedService>().OfType<BackgroundService>().Where(service => service is not BackgroundServiceExceptionListener);

            services.GetRequiredService<IHostApplicationLifetime>().ApplicationStopped.Register(() => {
                if (backgroundServices.Select(service => service.ExecuteTask?.Exception?.InnerException).FirstOrDefault() is { } exception) {
                    Environment.ExitCode = exitCodeGenerator(exception);
                }
            });

            return Task.CompletedTask;
        }

    }

    #region Register all interfaces

    /// <summary>
    /// <para>Register a class in the dependency injection context, and also register it so it can be injected as any of its interfaces, like Spring does by default and Autofac optionally allows.</para>
    /// <para>For example, if <c>MyClass</c> implements both <c>IMyInterface1</c> and <c>IMyInterface2</c>, this allows you to easily register <c>MyClass</c> in DI and inject either <c>IMyInterface1</c> or <c>IMyInterface2</c> into a service's constructor, without injection casting or duplicative unrefactorable registration clutter.</para>
    /// </summary>
    /// <typeparam name="TImpl">Type of the class to register</typeparam>
    /// <param name="services"><see cref="IHostApplicationBuilder.Services"/> or similar</param>
    /// <param name="registerAllInterfaces"><c>true</c> to also register the class as all of its implemented interfaces, in addition to its own class, or <c>false</c> to only register it as its own class (default <c>Microsoft.Extensions.DependencyInjection</c> behavior)</param>
    /// <returns>The same collection of service registrations, for chained calls</returns>
    public static IServiceCollection AddSingleton<TImpl>(this IServiceCollection services, bool registerAllInterfaces) where TImpl: class =>
        Add<TImpl>(services, ServiceLifetime.Singleton, registerAllInterfaces);

    /// <inheritdoc cref="AddSingleton{TImpl}(Microsoft.Extensions.DependencyInjection.IServiceCollection,bool)" />
    /// <param name="instance">One concrete singleton instance that will always be returned when these types are injected</param>
    public static IServiceCollection AddSingleton<TImpl>(this IServiceCollection services, TImpl instance, bool registerAllInterfaces) where TImpl: class =>
        Add(services, ServiceLifetime.Singleton, registerAllInterfaces, instance);

    /// <inheritdoc cref="AddSingleton{TImpl}(Microsoft.Extensions.DependencyInjection.IServiceCollection,bool)" />
    /// <param name="factory">Function that creates instances of <typeparamref name="TImpl"/></param>
    public static IServiceCollection AddSingleton<TImpl>(this IServiceCollection services, Func<IServiceProvider, TImpl> factory, bool registerAllInterfaces) where TImpl: class =>
        Add(services, ServiceLifetime.Singleton, registerAllInterfaces, factory);

    /// <inheritdoc cref="AddSingleton{TImpl}(Microsoft.Extensions.DependencyInjection.IServiceCollection,bool)" />
    public static IServiceCollection AddTransient<TImpl>(this IServiceCollection services, bool registerAllInterfaces) where TImpl: class =>
        Add<TImpl>(services, ServiceLifetime.Transient, registerAllInterfaces);

    /// <inheritdoc cref="AddSingleton{TImpl}(Microsoft.Extensions.DependencyInjection.IServiceCollection,bool)" />
    /// <param name="factory">Function that creates instances of <typeparamref name="TImpl"/></param>
    public static IServiceCollection AddTransient<TImpl>(this IServiceCollection services, Func<IServiceProvider, TImpl> factory, bool registerAllInterfaces) where TImpl: class =>
        Add(services, ServiceLifetime.Transient, registerAllInterfaces, factory);

    /// <inheritdoc cref="AddSingleton{TImpl}(Microsoft.Extensions.DependencyInjection.IServiceCollection,bool)" />
    public static IServiceCollection AddScoped<TImpl>(this IServiceCollection services, bool registerAllInterfaces) where TImpl: class =>
        Add<TImpl>(services, ServiceLifetime.Scoped, registerAllInterfaces);

    /// <inheritdoc cref="AddSingleton{TImpl}(Microsoft.Extensions.DependencyInjection.IServiceCollection,bool)" />
    /// <param name="factory">Function that creates instances of <typeparamref name="TImpl"/></param>
    public static IServiceCollection AddScoped<TImpl>(this IServiceCollection services, Func<IServiceProvider, TImpl> factory, bool registerAllInterfaces) where TImpl: class =>
        Add(services, ServiceLifetime.Scoped, registerAllInterfaces, factory);

    private static IServiceCollection Add<TImpl>(IServiceCollection services, ServiceLifetime scope, bool registerAllInterfaces) where TImpl: class => Add<TImpl>(services, registerAllInterfaces,
        () => new ServiceDescriptor(typeof(TImpl), typeof(TImpl), scope),
        @interface => scope == ServiceLifetime.Singleton ? new ServiceDescriptor(@interface, interfaceImplProvider<TImpl>, scope) : new ServiceDescriptor(@interface, typeof(TImpl), scope));

    private static IServiceCollection Add<TImpl>(IServiceCollection services, ServiceLifetime scope, bool registerAllInterfaces, TImpl instance) where TImpl: class => Add<TImpl>(services,
        registerAllInterfaces, () => new ServiceDescriptor(typeof(TImpl), instance, scope), @interface => new ServiceDescriptor(@interface, _ => instance, scope));

    private static IServiceCollection Add<TImpl>(IServiceCollection services, ServiceLifetime scope, bool registerAllInterfaces, Func<IServiceProvider, TImpl> factory) where TImpl: class =>
        Add<TImpl>(services,
            registerAllInterfaces, () => new ServiceDescriptor(typeof(TImpl), factory, scope), @interface => new ServiceDescriptor(@interface, interfaceImplProvider<TImpl>, scope));

    private static IServiceCollection Add<TImpl>(IServiceCollection services, bool registerAllInterfaces, Func<ServiceDescriptor> classRegistration,
                                                 Func<Type, ServiceDescriptor> interfaceRegistration) where TImpl: class {
        services.Add(classRegistration());
        if (registerAllInterfaces) {
            try {
                services.AddAll(typeof(TImpl).GetInterfaces().Select(interfaceRegistration));
            } catch (TargetInvocationException) {
                // if an interface's static initializer is throwing an exception, it will become obvious anyway
            }
        }
        return services;
    }

    private static object interfaceImplProvider<TImpl>(IServiceProvider services) => services.GetService<TImpl>()!;

    #endregion

    /// <summary>
    /// <para>Increase the level of log messages from certain categories/classes with certain event IDs, because the original developers foolishly logged errors at the debug level in the same class that logs lots of noisy, low-severity debug messages too, so you can't just set the log level for the provider to debug for this class without getting flooded with extra garbage.</para>
    /// <para>For example, if you have mismatched serialization settings between your sender and receiver, SignalR by default won't log anything, until you turn on debug logs for the correct class, at which point you get that error message as well as way too many unrelated logs that cry wolf.</para>
    /// <para>To fix this problem, this logger provider increases the log level of certain log messages in that class from debug to a higher level like warning, based on their event ID, so that messages which truly represent errors and other unexpected behavior are shown at the correct level.</para>
    /// <para>Usage:</para>
    /// <para>1. Register this logger provider by calling <see cref="DependencyInjectionExtensions.AmplifyMessageLevels"/>. In the options callback, call <see cref="AmplifiedLogOptions.Amplify"/> one or more times to give it the class name or category of the log message source, the desired new log level, and one or more event IDs that should be changed to the new log level.</para>
    /// <para><c>WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
    /// builder.Logging.AmplifyMessageLevels(options =&gt; options.Amplify("Microsoft.AspNetCore.SignalR.Internal.DefaultHubDispatcher", LogLevel.Warning, 2, 3, 5, 11, 13, 14, 15, 19, 21, 22, 23, 24));</c></para>
    /// <para>2. Set your application log level, in <c>appsettings.json</c> or wherever you set them. Make sure the level for the categories you specified (or their ancestors) is both greater than the original level (like debug) so that the noisy unwanted messages are hidden, and less than or equal to the amplified level (like warning), so you can see the important messages you amplified.</para>
    /// <para><c>{
    ///     "logging": {
    ///         "logLevel": {
    ///             "Microsoft.AspNetCore": "warning"
    ///         }
    ///     }
    /// }</c></para>
    /// <para>This will change the log messages from <c>Microsoft.AspNetCore.SignalR.Internal.DefaultHubDispatcher</c> which have any of the specified event IDs from their default debug level to warning. Other messages from that category with different event IDs will be logged at their original levels.</para>
    /// </summary>
    public static ILoggingBuilder AmplifyMessageLevels(this ILoggingBuilder loggingBuilder, Action<AmplifiedLogOptions> options) {
        IDictionary<string, IDictionary<int, LogLevel>> categoryAndEventIdToAmplifiedLevels = new Dictionary<string, IDictionary<int, LogLevel>>();
        options(new AmplifiedLogOptions(categoryAndEventIdToAmplifiedLevels));

        loggingBuilder.Services.AddSingleton<ILoggerProvider>(provider => new AmplifyingLoggerProvider(provider, categoryAndEventIdToAmplifiedLevels));

        foreach (string categoryToAmplify in categoryAndEventIdToAmplifiedLevels.Keys) {
            loggingBuilder.AddFilter<AmplifyingLoggerProvider>(categoryToAmplify, LogLevel.Trace);
        }

        return loggingBuilder;
    }

}