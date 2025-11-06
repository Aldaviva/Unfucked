using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Unfucked.DI;

namespace Unfucked;

public static partial class DependencyInjectionExtensions {

    #region Normal registrations

    /// <summary>
    /// <para>Register a class in the dependency injection context, and also register it so it can be injected as any of its interfaces or superclasses, like Spring does by default and Autofac optionally allows.</para>
    /// <para>For example, if <c>MyClass</c> implements <c>IMyInterface</c> and extends <c>MySuperClass</c>, this allows you to easily register <c>MyClass</c> in DI and inject either <c>IMyInterface</c> or <c>MySuperClass</c> into a service's constructor, without injection casting or duplicative unrefactorable registration clutter.</para>
    /// </summary>
    /// <typeparam name="TImpl">Type of the class to register</typeparam>
    /// <param name="services"><see cref="IHostApplicationBuilder.Services"/> or similar</param>
    /// <param name="alsoRegister">Also register the class as its own concrete class, all of its extended superclasses, or all of its implemented interfaces, or <see cref="SuperRegistration.NONE"/> to only register it as its own type (default <c>Microsoft.Extensions.DependencyInjection</c> behavior). A union of multiple values can be passed with logical OR (<see cref="SuperRegistration.SUPERCLASSES"/><c> | </c><see cref="SuperRegistration.INTERFACES"/>).</param>
    /// <returns>The same collection of service registrations, for chained calls</returns>
    public static IServiceCollection AddSingleton<TImpl>(this IServiceCollection services, SuperRegistration alsoRegister) where TImpl: class =>
        Add<TImpl>(services, ServiceLifetime.Singleton, alsoRegister);

    /// <inheritdoc cref="AddSingleton{TImpl}(Microsoft.Extensions.DependencyInjection.IServiceCollection,SuperRegistration)" />
    /// <param name="instance">One concrete singleton instance that will always be returned when these types are injected</param>
    public static IServiceCollection AddSingleton<TImpl>(this IServiceCollection services, TImpl instance, SuperRegistration alsoRegister) where TImpl: class =>
        Add(services, alsoRegister, instance);

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

    #endregion

    #region Hosted services

    /// <inheritdoc cref="AddSingleton{TImpl}(Microsoft.Extensions.DependencyInjection.IServiceCollection,SuperRegistration)" />
    public static IServiceCollection AddHostedService<TImpl>(this IServiceCollection services, SuperRegistration alsoRegister) where TImpl: class, IHostedService =>
        Add<TImpl>(services, alsoRegister, () => new ServiceDescriptor(typeof(IHostedService), typeof(TImpl), ServiceLifetime.Singleton),
            extra => new ServiceDescriptor(extra, HostedServiceProvider<TImpl>, ServiceLifetime.Singleton));

    /// <inheritdoc cref="AddSingleton{TImpl}(Microsoft.Extensions.DependencyInjection.IServiceCollection,SuperRegistration)" />
    /// <param name="factory">Function that creates instances of <typeparamref name="TImpl"/></param>
    public static IServiceCollection AddHostedService<TImpl>(this IServiceCollection services, Func<IServiceProvider, TImpl> factory, SuperRegistration alsoRegister)
        where TImpl: class, IHostedService => Add<TImpl>(services, alsoRegister, () => new ServiceDescriptor(typeof(IHostedService), factory, ServiceLifetime.Singleton),
        extra => new ServiceDescriptor(extra, HostedServiceProvider<TImpl>, ServiceLifetime.Singleton));

    #endregion

    #region Keyed services

    /// <inheritdoc cref="AddSingleton{TImpl}(Microsoft.Extensions.DependencyInjection.IServiceCollection,SuperRegistration)" />
    /// <param name="serviceKey">Key that identifies this registration, to be used when injecting using <see cref="ServiceKeyAttribute"/>.</param>
    public static IServiceCollection AddKeyedSingleton<TImpl>(this IServiceCollection services, object? serviceKey, SuperRegistration alsoRegister) where TImpl: class =>
        Add<TImpl>(services, ServiceLifetime.Singleton, alsoRegister, serviceKey);

    /// <inheritdoc cref="AddKeyedSingleton{TImpl}(Microsoft.Extensions.DependencyInjection.IServiceCollection,object?,Unfucked.DI.SuperRegistration)" />
    /// <param name="instance">One concrete singleton instance that will always be returned when these types are injected</param>
    public static IServiceCollection AddKeyedSingleton<TImpl>(this IServiceCollection services, object? serviceKey, TImpl instance, SuperRegistration alsoRegister) where TImpl: class =>
        Add(services, alsoRegister, instance, serviceKey);

    /// <inheritdoc cref="AddKeyedSingleton{TImpl}(Microsoft.Extensions.DependencyInjection.IServiceCollection,object?,Unfucked.DI.SuperRegistration)" />
    /// <param name="factory">Function that creates instances of <typeparamref name="TImpl"/></param>
    public static IServiceCollection AddKeyedSingleton<TImpl>(this IServiceCollection services, object? serviceKey, Func<IServiceProvider, object?, TImpl> factory, SuperRegistration alsoRegister)
        where TImpl: class => Add(services, ServiceLifetime.Singleton, alsoRegister, factory, serviceKey);

    /// <inheritdoc cref="AddKeyedSingleton{TImpl}(Microsoft.Extensions.DependencyInjection.IServiceCollection,object?,Unfucked.DI.SuperRegistration)" />
    public static IServiceCollection AddKeyedTransient<TImpl>(this IServiceCollection services, object? serviceKey, SuperRegistration alsoRegister) where TImpl: class =>
        Add<TImpl>(services, ServiceLifetime.Transient, alsoRegister, serviceKey);

    /// <inheritdoc cref="AddKeyedSingleton{TImpl}(Microsoft.Extensions.DependencyInjection.IServiceCollection,object?,Unfucked.DI.SuperRegistration)" />
    /// <param name="factory">Function that creates instances of <typeparamref name="TImpl"/></param>
    public static IServiceCollection AddKeyedTransient<TImpl>(this IServiceCollection services, object? serviceKey, Func<IServiceProvider, object?, TImpl> factory, SuperRegistration alsoRegister)
        where TImpl: class => Add(services, ServiceLifetime.Transient, alsoRegister, factory, serviceKey);

    /// <inheritdoc cref="AddKeyedSingleton{TImpl}(Microsoft.Extensions.DependencyInjection.IServiceCollection,object?,Unfucked.DI.SuperRegistration)" />
    public static IServiceCollection AddKeyedScoped<TImpl>(this IServiceCollection services, object? serviceKey, SuperRegistration alsoRegister) where TImpl: class =>
        Add<TImpl>(services, ServiceLifetime.Scoped, alsoRegister, serviceKey);

    /// <inheritdoc cref="AddKeyedSingleton{TImpl}(Microsoft.Extensions.DependencyInjection.IServiceCollection,object?,Unfucked.DI.SuperRegistration)" />
    /// <param name="factory">Function that creates instances of <typeparamref name="TImpl"/></param>
    public static IServiceCollection AddKeyedScoped<TImpl>(this IServiceCollection services, object? serviceKey, Func<IServiceProvider, object?, TImpl> factory, SuperRegistration alsoRegister)
        where TImpl: class =>
        Add(services, ServiceLifetime.Scoped, alsoRegister, factory, serviceKey);

    #endregion

    private static IServiceCollection Add<TImpl>(IServiceCollection services, ServiceLifetime scope, SuperRegistration alsoRegister) where TImpl: class => Add<TImpl>(services, alsoRegister,
        () => new ServiceDescriptor(typeof(TImpl), typeof(TImpl), scope),
        extra => scope == ServiceLifetime.Singleton ? new ServiceDescriptor(extra, ConcreteClassProvider<TImpl>, scope) : new ServiceDescriptor(extra, typeof(TImpl), scope));

    private static IServiceCollection Add<TImpl>(IServiceCollection services, SuperRegistration alsoRegister, TImpl instance) where TImpl: class => Add<TImpl>(services,
        alsoRegister, () => new ServiceDescriptor(typeof(TImpl), instance, ServiceLifetime.Singleton), extra => new ServiceDescriptor(extra, _ => instance, ServiceLifetime.Singleton));

    private static IServiceCollection Add<TImpl>(IServiceCollection services, ServiceLifetime scope, SuperRegistration alsoRegister, Func<IServiceProvider, TImpl> factory) where TImpl: class =>
        Add<TImpl>(services, alsoRegister, () => new ServiceDescriptor(typeof(TImpl), factory, scope), extra => new ServiceDescriptor(extra, ConcreteClassProvider<TImpl>, scope));

    private static IServiceCollection Add<TImpl>(IServiceCollection services, ServiceLifetime scope, SuperRegistration alsoRegister, object? serviceKey) where TImpl: class => Add<TImpl>(services,
        alsoRegister, () => new ServiceDescriptor(typeof(TImpl), serviceKey, typeof(TImpl), scope),
        extra => scope == ServiceLifetime.Singleton ? new ServiceDescriptor(extra, serviceKey, KeyedConcreteClassProvider<TImpl>, scope)
            : new ServiceDescriptor(extra, serviceKey, typeof(TImpl), scope));

    private static IServiceCollection Add<TImpl>(IServiceCollection services, SuperRegistration alsoRegister, TImpl instance, object? serviceKey) where TImpl: class => Add<TImpl>(services,
        alsoRegister, () => new ServiceDescriptor(typeof(TImpl), serviceKey, instance), extra => new ServiceDescriptor(extra, serviceKey, (_, _) => instance, ServiceLifetime.Singleton));

    private static IServiceCollection Add<TImpl>(IServiceCollection services, ServiceLifetime scope, SuperRegistration alsoRegister, Func<IServiceProvider, object?, TImpl> factory, object? serviceKey)
        where TImpl: class => Add<TImpl>(services, alsoRegister, () => new ServiceDescriptor(typeof(TImpl), serviceKey, factory, scope),
        extra => new ServiceDescriptor(extra, serviceKey, KeyedConcreteClassProvider<TImpl>, scope));

    [SuppressMessage("ReSharper", "PossibleMultipleEnumeration")] // IEnumerable.Concat obviously never enumerates the lazy sequence, even if it's in a try block
    private static IServiceCollection Add<TImpl>(IServiceCollection services, SuperRegistration alsoRegister, Func<ServiceDescriptor> defaultRegistration,
                                                 Func<Type, ServiceDescriptor> extraRegistration) where TImpl: class {
        IEnumerable<ServiceDescriptor> registrations = [defaultRegistration()];
        try {
            Type concreteType = typeof(TImpl);

            if ((alsoRegister & SuperRegistration.CONCRETE_CLASS) != 0) {
                registrations = registrations.Append(extraRegistration(concreteType));
            }

            if ((alsoRegister & SuperRegistration.INTERFACES) != 0) {
                registrations = registrations.Concat(concreteType.GetInterfaces().Except(InterfaceRegistrationBlacklist).Select(extraRegistration));
            }

            if ((alsoRegister & SuperRegistration.SUPERCLASSES) != 0) {
                IEnumerable<Type> superclasses = [];
                Type              @class       = concreteType, @object = typeof(object), valueType = typeof(ValueType);
                while (@class.BaseType is { } superclass && superclass != @object && superclass != valueType) {
                    superclasses = superclasses.Append(superclass);
                    @class       = superclass;
                }
                registrations = registrations.Concat(superclasses.Select(extraRegistration));
            }
        } catch (TargetInvocationException) {
            // if an interface's static initializer is throwing an exception, it will become obvious anyway
        }

        services.AddAll(registrations); // TryAddEnumerable fails when trying to register a class as itself, such as .AddSingleton<HttpClient>()
        return services;
    }

    private static object ConcreteClassProvider<TImpl>(IServiceProvider services) => services.GetService<TImpl>()!;

    private static object KeyedConcreteClassProvider<TImpl>(IServiceProvider services, object? serviceKey) => services.GetKeyedService<TImpl>(serviceKey)!;

    private static object HostedServiceProvider<TImpl>(IServiceProvider services) => services.GetServices<IHostedService>().OfTypeExactly<TImpl>().First()!;

}