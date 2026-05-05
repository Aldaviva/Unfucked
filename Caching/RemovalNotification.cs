namespace Unfucked.Caching;

/// <summary>Payload of event fired when a value is removed from a <see cref="Cache{K,V}"/> due to automatic expiration, manual replacement, or manual invalidation.</summary>
/// <typeparam name="K">Type of the cache key.</typeparam>
/// <typeparam name="V">Type of the cached values.</typeparam>
/// <param name="sender"><see cref="Cache{K,V}"/> instance.</param>
/// <param name="key">Cache key of the value that was removed.</param>
/// <param name="value">Cached value that was removed.</param>
/// <param name="cause">The reason why the value was removed from the cache.</param>
public delegate void RemovalNotification<in K, in V>(object sender, K key, V value, RemovalCause cause);

/// <summary>The reason why a value was removed from a cache.</summary>
public enum RemovalCause {

    /// <summary>Manual call to <see cref="Cache{K,V}.Invalidate"/>.</summary>
    Explicit,

    /// <summary>Manual call to <see cref="Cache{K,V}.Put"/>.</summary>
    Replaced,

    // Collected,
    /// <summary>Automatic cleanup, or manual call to <see cref="Cache{K,V}.CleanUp"/>, when <see cref="CacheOptions.ExpireAfterRead"/> or <see cref="CacheOptions.ExpireAfterWrite"/> are greater than <see cref="TimeSpan.Zero"/>.</summary>
    Expired,
    // Size

}

/// <summary>Extension members for <c>Unfucked.Caching</c>, such as for <see cref="RemovalCause"/>.</summary>
public static class Extensions {

    extension(RemovalCause cause) {

        /// <summary>Was the value automatically removed from the cache because it was stale?</summary>
        public bool WasEvicted => cause is /*RemovalCause.Collected or*/ RemovalCause.Expired /*or RemovalCause.Size*/;

    }

}