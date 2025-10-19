using Microsoft.Extensions.DependencyInjection;

namespace Unfucked.DI;

/// <summary>
/// Used with <see cref="IServiceCollection"/><c>.Add*</c> methods register not only the specified type, but also optionally its interfaces and superclasses.
/// </summary>
public enum SuperRegistration {

    /// <summary>
    /// Don't perform any extra registrations, just register the concrete class as its own type and nothing else, like the regular DI Add* methods.
    /// </summary>
    THIS_CLASS_ONLY,

    /// <summary>
    /// Register this concrete class as its own type, and also register it as all of its implemented interfaces.
    /// </summary>
    INTERFACES,

    /// <summary>
    /// Register this concrete class as its own type, and also register it as all of its extended superclasses.
    /// </summary>
    SUPERCLASSES,

    /// <summary>
    /// Register this concrete class as its own type, and also register it as all of its extended superclasses and implemented interfaces.
    /// </summary>
    SUPERCLASSES_AND_INTERFACES

}