using Microsoft.Extensions.DependencyInjection;

namespace Unfucked.DI;

/// <summary>
/// Declarative injection of dependencies with shorter lifetimes into dependents with longer lifetimes, like <c>javax.inject.Provider&lt;T&gt;</c>, without the complication of creating scopes, so you don't have a inject an <see cref="IServiceProvider"/> and imperatively request everything, which isn't very DI-like.
/// </summary>
/// <typeparam name="T">Type to request from IoC container</typeparam>
public interface OptionalProvider<out T> {

    /// <summary>
    /// Provide an optional dependency service of the given type.
    /// </summary>
    /// <returns>A service instance from the dependency injection context, or <c>null</c> if no service with the given type <typeparamref name="T"/> was registered in the context.</returns>
    T? Get();

}

/// <inheritdoc />
public interface Provider<out T>: OptionalProvider<T> where T: notnull {

    /// <summary>
    /// Provide a dependency service of the given type.
    /// </summary>
    /// <returns>A required service instance from the dependency injection context, never <c>null</c>.</returns>
    new T Get();

}

/// <inheritdoc />
public class MicrosoftDependencyInjectionServiceProvider<T>(IServiceProvider services): Provider<T> where T: notnull {

    /// <inheritdoc />
    public T Get() => services.GetRequiredService<T>();

    T? OptionalProvider<T>.Get() => services.GetService<T>();

}