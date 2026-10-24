using System.ComponentModel;

namespace Unfucked;

/// <summary>
/// Methods that make it easier to work with lazy values.
/// </summary>
public static class LazyExtensions {

    /// <summary>
    /// Easily dispose of the lazy value without all the conditional and exception handling boilerplate.
    /// </summary>
    /// <typeparam name="T">Type of value in the <see cref="Lazy{T}"/> instance</typeparam>
    /// <param name="lazy"><see cref="Lazy{T}"/> instance</param>
    /// <returns><c>true</c> if the <see cref="Lazy{T}"/> instance had a value and it was disposed, or <c>false</c> if it didn't have a value yet</returns>
    public static bool TryDisposeValue<T>(this Lazy<T> lazy) where T: IDisposable? {
        try {
            if (lazy is { IsValueCreated: true, Value: IDisposable d }) {
                d.Dispose();
                return true;
            }
        } catch (MemberAccessException) {}
        return false;
    }

    /// <inheritdoc cref="TryDisposeValue{T}" />
    public static async Task<bool> TryDisposeValueAsync<T>(this Lazy<T> lazy) where T: IAsyncDisposable? {
        try {
            if (lazy is { IsValueCreated: true, Value: IAsyncDisposable ad }) {
                await ad.DisposeAsync().ConfigureAwait(false);
                return true;
            }
        } catch (MemberAccessException) {}
        return false;
    }

    extension<T>(Lazy<T> lazy) where T: class? {

        /// <summary>Get the already-created value of the <see cref="Lazy{T}"/> instance, or <c>null</c> if no value has been created yet rather than creating the value on-demand.</summary>
        /// <returns>Value of <paramref name="lazy"/>, or <c>null</c> if it was never created.</returns>
        public T? ExistingValueOrNull => lazy.IsValueCreated ? lazy.Value : null;

    }

}

/// <inheritdoc cref="LazyExtensions" />
[EditorBrowsable(EditorBrowsableState.Advanced)]
public static class LazyExtensions2 {

    extension<T>(Lazy<T> lazy) where T: struct {

        /// <inheritdoc cref="LazyExtensions.get_ExistingValueOrNull{T}" />
        public T? ExistingValueOrNull => lazy.IsValueCreated ? lazy.Value : null;

    }

}