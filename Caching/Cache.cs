namespace Unfucked.Caching;

/// <summary>Strongly-typed cache that can encapsulate automatic value loading logic into the cache itself, instead of duplicating it across every single call site. Supports expiration after read or write, and periodic refresh for expired values.</summary>
/// <remarks>Inspired by Guava Cache.</remarks>
/// <typeparam name="K">Type of the cache key.</typeparam>
/// <typeparam name="V">Type of the cached values.</typeparam>
public interface Cache<K, V>: IDisposable where K: notnull {

    /// <summary>Fired when a value is removed from the cache, including the removal reason.</summary>
    event RemovalNotification<K, V> Removal;

    /// <summary>Number of keys currently in the cache.</summary>
    long Count { get; }

    /// <summary>Retrieve a cached value by its key, automatically loading the value if it's missing from the cache. If the cache has expiration times (<see cref="CacheOptions.ExpireAfterRead"/> or <see cref="CacheOptions.ExpireAfterWrite"/>) this will be automatically refreshed so an expired value is never returned.</summary>
    /// <param name="key">The key of the value which you want to get.</param>
    /// <param name="loader">Optional callback used to generate the cache value if it's expired or not already in the cache. Overrides any loader specified on the cache itself in <see cref="InMemoryCache{K,V}(CacheOptions?,Func{K,ValueTask{V}}?)"/>. If this and the cache-wide loader are both <c>null</c>, you must first manually populate the cache with <see cref="Put"/>. This callback can throw an exception, which will not be cached.</param>
    /// <returns>The cached value for the key.</returns>
    /// <exception cref="System.Collections.Generic.KeyNotFoundException">No value loader was specified in <paramref name="loader"/> or for the <see cref="Cache{K,V}"/> instance, and <paramref name="key"/> had not already been manually added to the cache with <see cref="Put"/>.</exception>
    /// <exception cref="Exception">The loader threw an exception while loading the value for <paramref name="key"/>, and the value was not cached.</exception>
    Task<V> Get(K key, Func<K, ValueTask<V>>? loader = null);

    /// <summary>Manually and eagerly adds or replaces a value to the cache, instead of letting it get automatically and lazily loaded when the value is requested.</summary>
    /// <param name="key">The key of the value which you want to set.</param>
    /// <param name="value">Cached value to manually add.</param>
    Task Put(K key, V value);

    /// <summary>Remove all expired values from the cache. Only has an effect if either <see cref="CacheOptions.ExpireAfterRead"/> or <see cref="CacheOptions.ExpireAfterWrite"/> are greater than <see cref="TimeSpan.Zero"/>. By default, this runs automatically once per minute, which can be customized with <see cref="CacheOptions.ExpirationScanInterval"/>.</summary>
    void CleanUp();

    /// <summary>Remove keys and their values from the cache.</summary>
    /// <param name="key">Zero or more keys to remove.</param>
    void Invalidate(params IEnumerable<K> key);

    /// <summary>Remove all keys and values from the cache.</summary>
    void InvalidateAll();

}