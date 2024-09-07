using System.Runtime.Caching;

// ReSharper disable InconsistentNaming

namespace Unfucked.Caching;

/// <summary>
/// Like <see cref="MemoryCache"/>, but strongly typed
/// </summary>
/// <typeparam name="T">The type of the value to cache</typeparam>
public interface IMemoryCache<T>: IDisposable where T: notnull {

    #region New Methods

    /// <summary>
    /// Lazy/deferred synchronous upsert with custom cache item policy
    /// </summary>
    /// <param name="key">Cache key</param>
    /// <param name="valueCreator">Function that creates the value, will be called (possibly multiple times) if the key is not present in the cache, but 0 times if it is present</param>
    /// <param name="policy">custom cache item policy</param>
    /// <returns>The old cached value if it already existed, or the new value returned and cached from <paramref name="valueCreator"/> otherwise</returns>
    T GetOrAdd(string key, Func<T> valueCreator, CacheItemPolicy? policy = default);

    /// <summary>
    /// Lazy/deferred synchronous upsert with custom cache item policy
    /// </summary>
    /// <param name="key">Cache key</param>
    /// <param name="valueCreator">Function that creates the value, will be called (possibly multiple times) if the key is not present in the cache, but 0 times if it is present</param>
    /// <param name="evictAfterCreation">cache item lifetime after write</param>
    /// <returns>The old cached value if it already existed, or the new value returned and cached from <paramref name="valueCreator"/> otherwise</returns>
    T GetOrAdd(string key, Func<T> valueCreator, TimeSpan evictAfterCreation);

    /// <summary>
    /// Lazy/deferred asynchronous upsert with custom cache item policy
    /// </summary>
    /// <param name="key">Cache key</param>
    /// <param name="valueCreator">Function that asynchronously creates the value, will be called (possibly multiple times) if the key is not present in the cache, but 0 times if it is present</param>
    /// <param name="policy">custom cache item policy</param>
    /// <returns>The old cached value if it already existed, or the new value returned and cached from <paramref name="valueCreator"/> otherwise</returns>
    Task<T> GetOrAdd(string key, Func<Task<T>> valueCreator, CacheItemPolicy? policy = null);

    /// <summary>
    /// Lazy/deferred asynchronous upsert with custom cache item policy
    /// </summary>
    /// <param name="key">Cache key</param>
    /// <param name="valueCreator">Function that asynchronously creates the value, will be called (possibly multiple times) if the key is not present in the cache, but 0 times if it is present</param>
    /// <param name="evictAfterCreation">cache item lifetime after write</param>
    /// <returns>The old cached value if it already existed, or the new value returned and cached from <paramref name="valueCreator"/> otherwise</returns>
    Task<T> GetOrAdd(string key, Func<Task<T>> valueCreator, TimeSpan evictAfterCreation);

    /// <summary>
    /// Remove all entries from the cache
    /// </summary>
    void Clear();

    #endregion

    #region Delegated

    /// <inheritdoc cref="MemoryCache.GetEnumerator()" />
    IEnumerator<T> GetEnumerator();

    /// <inheritdoc cref="ObjectCache.Add(string,object,System.DateTimeOffset,string)" />
    bool Add(string key, T value, DateTimeOffset absoluteExpiration);

    /// <inheritdoc cref="ObjectCache.Add(string,object,CacheItemPolicy,string?)" />
    bool Add(string key, T value, CacheItemPolicy policy);

    /// <inheritdoc cref="MemoryCache.Add(CacheItem,CacheItemPolicy)" />
    bool Add(CacheItem item, CacheItemPolicy policy);

    /// <inheritdoc cref="MemoryCache.GetValues(IEnumerable{string},string)" />
    IDictionary<string, T>? GetValues(params string[] keys);

    /// <inheritdoc cref="MemoryCache.GetValues(IEnumerable{string},string)" />
    IDictionary<string, T> GetValues(IEnumerable<string> keys);

    /// <inheritdoc cref="MemoryCache.CreateCacheEntryChangeMonitor" />
    CacheEntryChangeMonitor CreateCacheEntryChangeMonitor(IEnumerable<string> keys);

    /// <inheritdoc cref="MemoryCache.Trim" />
    long Trim(int percent);

    /// <inheritdoc cref="MemoryCache.Contains" />
    bool Contains(string key);

    /// <inheritdoc cref="MemoryCache.AddOrGetExisting(string,object,DateTimeOffset,string)" />
    T AddOrGetExisting(string key, T value, DateTimeOffset absoluteExpiration);

    /// <inheritdoc cref="MemoryCache.AddOrGetExisting(CacheItem,CacheItemPolicy)" />
    CacheItem AddOrGetExisting(CacheItem item, CacheItemPolicy policy);

    /// <inheritdoc cref="MemoryCache.AddOrGetExisting(string,object,CacheItemPolicy,string)" />
    T AddOrGetExisting(string key, T value, CacheItemPolicy policy);

    /// <summary>When overridden in a derived class, gets the specified cache entry from the cache as an object.</summary>
    /// <param name="key">A unique identifier for the cache entry to get.</param>
    /// <returns>The cache entry that is identified by <paramref name="key" />.</returns>
    T? Get(string key);

    /// <inheritdoc cref="MemoryCache.GetCacheItem" />
    CacheItem? GetCacheItem(string key);

    /// <inheritdoc cref="MemoryCache.Set(string,object,DateTimeOffset,string)" />
    void Set(string key, T value, DateTimeOffset absoluteExpiration);

    /// <inheritdoc cref="MemoryCache.Set(CacheItem,CacheItemPolicy)" />
    void Set(CacheItem item, CacheItemPolicy policy);

    /// <inheritdoc cref="MemoryCache.Set(string,object,CacheItemPolicy,string?)" />
    void Set(string key, T value, CacheItemPolicy policy);

    /// <inheritdoc cref="MemoryCache.Remove(string,string)" />
    T? Remove(string key);

    /// <inheritdoc cref="MemoryCache.Remove(string,CacheEntryRemovedReason,string?)" />
    T? Remove(string key, CacheEntryRemovedReason reason);

    /// <inheritdoc cref="MemoryCache.GetCount" />
    long GetCount(string? regionName = null);

    /// <inheritdoc cref="MemoryCache.GetLastSize" />
    long GetLastSize(string? regionName = null);

    /// <inheritdoc cref="MemoryCache.CacheMemoryLimit" />
    long CacheMemoryLimit { get; }

    /// <inheritdoc cref="MemoryCache{T}.DefaultCacheCapabilities" />
    DefaultCacheCapabilities DefaultCacheCapabilities { get; }

    /// <inheritdoc cref="MemoryCache.Name" />
    string Name { get; }

    /// <inheritdoc cref="MemoryCache.PhysicalMemoryLimit" />
    long PhysicalMemoryLimit { get; }

    /// <inheritdoc cref="MemoryCache.PollingInterval" />
    TimeSpan PollingInterval { get; }

    /// <inheritdoc cref="MemoryCache.this" />
    T? this[string key] { get; set; }

    #endregion

}