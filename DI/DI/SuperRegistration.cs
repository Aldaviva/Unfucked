using Microsoft.Extensions.DependencyInjection;

namespace Unfucked.DI;

/// <summary>
/// Used with <see cref="IServiceCollection"/><c>.Add*</c> methods register not only the specified type, but also optionally its interfaces and superclasses.
/// </summary>
[Flags]
public enum SuperRegistration: ushort {

    /// <summary>
    /// Don't perform any extra registrations, like the regular DI Add* methods.
    /// </summary>
    NONE = 0,

    /// <summary>
    /// Register the concrete class as its own type.
    /// </summary>
    CONCRETE_CLASS = 1,

    /// <summary>
    /// Register this concrete class as all of its implemented interfaces.
    /// </summary>
    INTERFACES = 2,

    /// <summary>
    /// Register this concrete class as all of its extended superclasses.
    /// </summary>
    SUPERCLASSES = 4

}