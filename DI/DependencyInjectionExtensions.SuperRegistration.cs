using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Reflection;
using System.Runtime.Serialization;
using Unfucked.DI;

namespace Unfucked;

public static partial class DependencyInjectionExtensions {

    private static readonly IEnumerable<Type> INTERFACE_REGISTRATION_BLACKLIST = [
        typeof(IDisposable),
        typeof(IAsyncDisposable),
        typeof(ICloneable),
        typeof(ISerializable),
        typeof(IHostedService) // if you want to register a class as a hosted service and also its own interfaces, call Services.AddHostedService<MyHostedService>(SuperRegistration.INTERFACES)
    ];

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
    public static IServiceCollection AddHostedService<TImpl>(this IServiceCollection services, SuperRegistration alsoRegister) where TImpl: class, IHostedService {
        if ((alsoRegister & SuperRegistration.CONCRETE_CLASS) != 0) {
            return Add<TImpl>(services, alsoRegister & ~SuperRegistration.CONCRETE_CLASS, () => [
                new ServiceDescriptor(typeof(TImpl), typeof(TImpl), ServiceLifetime.Singleton),
                new ServiceDescriptor(typeof(IHostedService), ServiceProvider<TImpl>, ServiceLifetime.Singleton)
            ], extra => new ServiceDescriptor(extra, ServiceProvider<TImpl>, ServiceLifetime.Singleton));
        } else {
            Guid concreteClassKey = Guid.NewGuid();
            return Add<TImpl>(services, alsoRegister, () => [
                new ServiceDescriptor(typeof(TImpl), concreteClassKey, typeof(TImpl), ServiceLifetime.Singleton),
                new ServiceDescriptor(typeof(IHostedService), provider => KeyedServiceProvider<TImpl>(provider, concreteClassKey), ServiceLifetime.Singleton)
            ], extra => new ServiceDescriptor(extra, provider => KeyedServiceProvider<TImpl>(provider, concreteClassKey), ServiceLifetime.Singleton));
        }
    }

    /// <inheritdoc cref="AddSingleton{TImpl}(Microsoft.Extensions.DependencyInjection.IServiceCollection,SuperRegistration)" />
    /// <param name="factory">Function that creates instances of <typeparamref name="TImpl"/></param>
    public static IServiceCollection AddHostedService<TImpl>(this IServiceCollection services, Func<IServiceProvider, TImpl> factory, SuperRegistration alsoRegister)
        where TImpl: class, IHostedService {
        if ((alsoRegister & SuperRegistration.CONCRETE_CLASS) != 0) {
            return Add<TImpl>(services, alsoRegister & ~SuperRegistration.CONCRETE_CLASS, () => [
                new ServiceDescriptor(typeof(TImpl), factory, ServiceLifetime.Singleton),
                new ServiceDescriptor(typeof(IHostedService), ServiceProvider<TImpl>, ServiceLifetime.Singleton)
            ], extra => new ServiceDescriptor(extra, ServiceProvider<TImpl>, ServiceLifetime.Singleton));
        } else {
            Guid concreteClassKey = Guid.NewGuid();
            return Add<TImpl>(services, alsoRegister, () => [
                new ServiceDescriptor(typeof(TImpl), concreteClassKey, (provider, _) => factory(provider), ServiceLifetime.Singleton),
                new ServiceDescriptor(typeof(IHostedService), provider => KeyedServiceProvider<TImpl>(provider, concreteClassKey), ServiceLifetime.Singleton)
            ], extra => new ServiceDescriptor(extra, provider => KeyedServiceProvider<TImpl>(provider, concreteClassKey), ServiceLifetime.Singleton));
        }
    }

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
        where TImpl: class => Add(services, ServiceLifetime.Scoped, alsoRegister, factory, serviceKey);

    #endregion

    private static IServiceCollection Add<TImpl>(IServiceCollection services, ServiceLifetime scope, SuperRegistration alsoRegister) where TImpl: class =>
        Add<TImpl>(services, alsoRegister, () => [new ServiceDescriptor(typeof(TImpl), typeof(TImpl), scope)],
            extra => scope == ServiceLifetime.Singleton ? new ServiceDescriptor(extra, ServiceProvider<TImpl>, scope) : new ServiceDescriptor(extra, typeof(TImpl), scope));

    private static IServiceCollection Add<TImpl>(IServiceCollection services, SuperRegistration alsoRegister, TImpl instance) where TImpl: class =>
        Add<TImpl>(services, alsoRegister, () => [new ServiceDescriptor(typeof(TImpl), instance, ServiceLifetime.Singleton)],
            extra => new ServiceDescriptor(extra, _ => instance, ServiceLifetime.Singleton));

    private static IServiceCollection Add<TImpl>(IServiceCollection services, ServiceLifetime scope, SuperRegistration alsoRegister, Func<IServiceProvider, TImpl> factory) where TImpl: class =>
        Add<TImpl>(services, alsoRegister, () => [new ServiceDescriptor(typeof(TImpl), factory, scope)], extra => new ServiceDescriptor(extra, ServiceProvider<TImpl>, scope));

    private static IServiceCollection Add<TImpl>(IServiceCollection services, ServiceLifetime scope, SuperRegistration alsoRegister, object? serviceKey) where TImpl: class =>
        Add<TImpl>(services, alsoRegister, () => [new ServiceDescriptor(typeof(TImpl), serviceKey, typeof(TImpl), scope)],
            extra => scope == ServiceLifetime.Singleton ? new ServiceDescriptor(extra, serviceKey, KeyedServiceProvider<TImpl>, scope)
                : new ServiceDescriptor(extra, serviceKey, typeof(TImpl), scope));

    private static IServiceCollection Add<TImpl>(IServiceCollection services, SuperRegistration alsoRegister, TImpl instance, object? serviceKey) where TImpl: class =>
        Add<TImpl>(services, alsoRegister, () => [new ServiceDescriptor(typeof(TImpl), serviceKey, instance)],
            extra => new ServiceDescriptor(extra, serviceKey, (_, _) => instance, ServiceLifetime.Singleton));

    private static IServiceCollection Add<TImpl>(IServiceCollection services, ServiceLifetime scope, SuperRegistration alsoRegister, Func<IServiceProvider, object?, TImpl> factory, object? serviceKey)
        where TImpl: class => Add<TImpl>(services, alsoRegister, () => [new ServiceDescriptor(typeof(TImpl), serviceKey, factory, scope)],
        extra => new ServiceDescriptor(extra, serviceKey, KeyedServiceProvider<TImpl>, scope));

    private static IServiceCollection Add<TImpl>(IServiceCollection services, SuperRegistration alsoRegister, Func<IEnumerable<ServiceDescriptor>> defaultRegistrations,
                                                 Func<Type, ServiceDescriptor> extraRegistration) where TImpl: class {
        List<ServiceDescriptor> registrations = [..defaultRegistrations()];

        try {
            if ((alsoRegister & SuperRegistration.CONCRETE_CLASS) != 0) {
                registrations.Add(extraRegistration(typeof(TImpl)));
            }

            if ((alsoRegister & SuperRegistration.INTERFACES) != 0) {
                registrations.AddRange(typeof(TImpl).GetInterfaces().Except(INTERFACE_REGISTRATION_BLACKLIST).Select(extraRegistration));
            }

            if ((alsoRegister & SuperRegistration.SUPERCLASSES) != 0) {
                Type @class = typeof(TImpl);
                while (@class.BaseType is {} superclass && superclass != typeof(object) && superclass != typeof(ValueType)) {
                    registrations.Add(extraRegistration(superclass));
                    @class = superclass;
                }
            }
        } catch (TargetInvocationException) {
            // if an interface's static initializer is throwing an exception, it will become obvious anyway
        }

        services.AddAll(registrations); // TryAddEnumerable fails when trying to register a class as itself, such as .AddSingleton<HttpClient>()
        return services;
    }

    private static object ServiceProvider<TImpl>(IServiceProvider services) => services.GetService<TImpl>()!;

    private static object KeyedServiceProvider<TImpl>(IServiceProvider services, object? serviceKey) => services.GetKeyedService<TImpl>(serviceKey)!;

}