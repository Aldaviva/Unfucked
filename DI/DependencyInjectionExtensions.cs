using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.Serialization;
using Unfucked.DI;
#if !NET6_0_OR_GREATER
using System.Diagnostics;
#endif

namespace Unfucked;

/// <summary>
/// Methods that make it easier to work with the <c>Microsoft.Extensions.Hosting</c> dependency injection/inversion of control library, which is used in the Generic Host and ASP.NET Core.
/// </summary>
public static class DependencyInjectionExtensions {

    private static readonly IEnumerable<Type> InterfaceRegistrationBlacklist = [
        typeof(IDisposable),
        typeof(IAsyncDisposable),
        typeof(ICloneable),
        typeof(ISerializable)
    ];

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

    #region Super registrations

    /// <summary>
    /// <para>Register a class in the dependency injection context, and also register it so it can be injected as any of its interfaces or superclasses, like Spring does by default and Autofac optionally allows.</para>
    /// <para>For example, if <c>MyClass</c> implements <c>IMyInterface1</c> and extends <c>MySuperClass</c>, this allows you to easily register <c>MyClass</c> in DI and inject either <c>IMyInterface1</c> or <c>MySuperClass</c> into a service's constructor, without injection casting or duplicative unrefactorable registration clutter.</para>
    /// </summary>
    /// <typeparam name="TImpl">Type of the class to register</typeparam>
    /// <param name="services"><see cref="IHostApplicationBuilder.Services"/> or similar</param>
    /// <param name="alsoRegister"><c>true</c> to also register the class as all of its extended superclasses or implemented interfaces, in addition to its own class, or <see cref="SuperRegistration.THIS_CLASS_ONLY"/> to only register it as its own type (default <c>Microsoft.Extensions.DependencyInjection</c> behavior)</param>
    /// <returns>The same collection of service registrations, for chained calls</returns>
    public static IServiceCollection AddSingleton<TImpl>(this IServiceCollection services, SuperRegistration alsoRegister) where TImpl: class =>
        Add<TImpl>(services, ServiceLifetime.Singleton, alsoRegister);

    /// <inheritdoc cref="AddSingleton{TImpl}(Microsoft.Extensions.DependencyInjection.IServiceCollection,SuperRegistration)" />
    /// <param name="instance">One concrete singleton instance that will always be returned when these types are injected</param>
    public static IServiceCollection AddSingleton<TImpl>(this IServiceCollection services, TImpl instance, SuperRegistration alsoRegister) where TImpl: class =>
        Add(services, ServiceLifetime.Singleton, alsoRegister, instance);

    /// <inheritdoc cref="AddSingleton{TImpl}(Microsoft.Extensions.DependencyInjection.IServiceCollection,SuperRegistration)" />
    /// <param name="factory">Function that creates instances of <typeparamref name="TImpl"/></param>
    public static IServiceCollection AddSingleton<TImpl>(this IServiceCollection services, Func<IServiceProvider, TImpl> factory, SuperRegistration alsoRegister) where TImpl: class =>
        Add(services, ServiceLifetime.Singleton, alsoRegister, factory);

    /// <inheritdoc cref="AddSingleton{TImpl}(Microsoft.Extensions.DependencyInjection.IServiceCollection,SuperRegistration)" />
    public static IServiceCollection AddTransient<TImpl>(this IServiceCollection services, SuperRegistration alsoRegister) where TImpl: class =>
        Add<TImpl>(services, ServiceLifetime.Transient, alsoRegister);

    /// <inheritdoc cref="AddSingleton{TImpl}(Microsoft.Extensions.DependencyInjection.IServiceCollection,SuperRegistration)" />
    /// <param name="factory">Function that creates instances of <typeparamref name="TImpl"/></param>
    public static IServiceCollection AddTransient<TImpl>(this IServiceCollection services, Func<IServiceProvider, TImpl> factory, SuperRegistration alsoRegister) where TImpl: class =>
        Add(services, ServiceLifetime.Transient, alsoRegister, factory);

    /// <inheritdoc cref="AddSingleton{TImpl}(Microsoft.Extensions.DependencyInjection.IServiceCollection,SuperRegistration)" />
    public static IServiceCollection AddScoped<TImpl>(this IServiceCollection services, SuperRegistration alsoRegister) where TImpl: class =>
        Add<TImpl>(services, ServiceLifetime.Scoped, alsoRegister);

    /// <inheritdoc cref="AddSingleton{TImpl}(Microsoft.Extensions.DependencyInjection.IServiceCollection,SuperRegistration)" />
    /// <param name="factory">Function that creates instances of <typeparamref name="TImpl"/></param>
    public static IServiceCollection AddScoped<TImpl>(this IServiceCollection services, Func<IServiceProvider, TImpl> factory, SuperRegistration alsoRegister) where TImpl: class =>
        Add(services, ServiceLifetime.Scoped, alsoRegister, factory);

    private static IServiceCollection Add<TImpl>(IServiceCollection services, ServiceLifetime scope, SuperRegistration alsoRegister) where TImpl: class => Add<TImpl>(services, alsoRegister,
        () => new ServiceDescriptor(typeof(TImpl), typeof(TImpl), scope),
        super => scope == ServiceLifetime.Singleton ? new ServiceDescriptor(super, concreteClassProvider<TImpl>, scope) : new ServiceDescriptor(super, typeof(TImpl), scope));

    private static IServiceCollection Add<TImpl>(IServiceCollection services, ServiceLifetime scope, SuperRegistration alsoRegister, TImpl instance) where TImpl: class => Add<TImpl>(services,
        alsoRegister, () => new ServiceDescriptor(typeof(TImpl), instance, scope), super => new ServiceDescriptor(super, _ => instance, scope));

    private static IServiceCollection Add<TImpl>(IServiceCollection services, ServiceLifetime scope, SuperRegistration alsoRegister, Func<IServiceProvider, TImpl> factory) where TImpl: class =>
        Add<TImpl>(services,
            alsoRegister, () => new ServiceDescriptor(typeof(TImpl), factory, scope), super => new ServiceDescriptor(super, concreteClassProvider<TImpl>, scope));

    [SuppressMessage("ReSharper", "PossibleMultipleEnumeration")] // IEnumerable.Concat obviously never enumerates the lazy sequence, even if it's in a try block
    private static IServiceCollection Add<TImpl>(IServiceCollection services, SuperRegistration alsoRegister, Func<ServiceDescriptor> classRegistration,
                                                 Func<Type, ServiceDescriptor> superRegistration) where TImpl: class {
        IEnumerable<ServiceDescriptor> registrations = [classRegistration()];
        try {
            Type concreteType = typeof(TImpl);
            if (alsoRegister is SuperRegistration.INTERFACES or SuperRegistration.SUPERCLASSES_AND_INTERFACES) {
                registrations = registrations.Concat(concreteType.GetInterfaces().Except(InterfaceRegistrationBlacklist).Select(superRegistration));
            }

            if (alsoRegister is SuperRegistration.SUPERCLASSES or SuperRegistration.SUPERCLASSES_AND_INTERFACES) {
                IEnumerable<Type> superclasses = [];
                Type?             @class       = concreteType;
                Type              @object      = typeof(object), valueType = typeof(ValueType);
                while (@class.BaseType is { } superclass && superclass != @object && superclass != valueType) {
                    superclasses = superclasses.Append(superclass);
                    @class       = superclass;
                }
                registrations = registrations.Concat(superclasses.Select(superRegistration));
            }
        } catch (TargetInvocationException) {
            // if an interface's static initializer is throwing an exception, it will become obvious anyway
        }

        services.AddAll(registrations);
        return services;
    }

    private static object concreteClassProvider<TImpl>(IServiceProvider services) => services.GetService<TImpl>()!;

    #endregion

}