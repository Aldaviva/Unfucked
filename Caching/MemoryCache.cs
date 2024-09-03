using System.Collections.Specialized;
using System.Runtime.Caching;
using MemoryCache = System.Runtime.Caching.MemoryCache;

// ReSharper disable InconsistentNaming

namespace Unfucked;

/// <inheritdoc cref="IMemoryCache{T}" />
public class MemoryCache<T>(string name = "", NameValueCollection? config = null, bool ignoreConfigSection = false): IMemoryCache<T> where T: notnull {

    private readonly MemoryCache cache = new(name, config, ignoreConfigSection);

    #region New Methods

    /// <inheritdoc />
    public T GetOrAdd(string key, Func<T> valueCreator, CacheItemPolicy? policy = null) {
        return Get(key) ?? AddOrGetExisting(key, valueCreator(), policy ?? new CacheItemPolicy());
    }

    /// <inheritdoc />
    public T GetOrAdd(string key, Func<T> valueCreator, TimeSpan evictAfterCreation) {
        return GetOrAdd(key, valueCreator, new CacheItemPolicy { AbsoluteExpiration = DateTimeOffset.Now + evictAfterCreation });
    }

    /// <inheritdoc />
    public async Task<T> GetOrAdd(string key, Func<Task<T>> valueCreator, CacheItemPolicy? policy = null) {
        return Get(key) ?? AddOrGetExisting(key, await valueCreator().ConfigureAwait(false), policy ?? new CacheItemPolicy());
    }

    /// <inheritdoc />
    public async Task<T> GetOrAdd(string key, Func<Task<T>> valueCreator, TimeSpan evictAfterCreation) {
        return await GetOrAdd(key, valueCreator, new CacheItemPolicy { AbsoluteExpiration = DateTimeOffset.Now + evictAfterCreation }).ConfigureAwait(false);
    }

    public void Clear() {
        Trim(100);
    }

    #endregion

    #region Delegated

    /// <inheritdoc />
    public IEnumerator<T> GetEnumerator() {
        return ((IEnumerable<T>) cache).GetEnumerator();
    }

    /// <inheritdoc />
    public bool Add(string key, T value, DateTimeOffset absoluteExpiration) {
        return cache.Add(key, value, absoluteExpiration);
    }

    /// <inheritdoc />
    public bool Add(string key, T value, CacheItemPolicy policy) {
        return cache.Add(key, value, policy);
    }

    /// <inheritdoc />
    public IDictionary<string, T>? GetValues(params string[] keys) {
        return cache.GetValues(null, keys) as IDictionary<string, T>;
    }

    /// <inheritdoc />
    public CacheEntryChangeMonitor CreateCacheEntryChangeMonitor(IEnumerable<string> keys) {
        return cache.CreateCacheEntryChangeMonitor(keys);
    }

    /// <inheritdoc cref="MemoryCache.Dispose" />
    public void Dispose() {
        cache.Dispose();
        GC.SuppressFinalize(this);
    }

    /// <inheritdoc />
    public long Trim(int percent) {
        return cache.Trim(percent);
    }

    /// <inheritdoc />
    public bool Contains(string key) {
        return cache.Contains(key);
    }

    /// <inheritdoc />
    public bool Add(CacheItem item, CacheItemPolicy policy) {
        return cache.Add(item, policy);
    }

    /// <inheritdoc />
    public T AddOrGetExisting(string key, T value, DateTimeOffset absoluteExpiration) {
        return (T) cache.AddOrGetExisting(key, value, absoluteExpiration) ?? value;
    }

    /// <inheritdoc />
    public CacheItem AddOrGetExisting(CacheItem item, CacheItemPolicy policy) {
        return cache.AddOrGetExisting(item, policy);
    }

    /// <inheritdoc />
    public T AddOrGetExisting(string key, T value, CacheItemPolicy policy) {
        return (T) cache.AddOrGetExisting(key, value, policy) ?? value;
    }

    /// <inheritdoc />
    // ExceptionAdjustment: M:System.Runtime.Caching.MemoryCache.Get(System.String,System.String) -T:System.NotSupportedException
    public T? Get(string key) {
        return (T?) cache.Get(key);
    }

    /// <inheritdoc />
    public CacheItem? GetCacheItem(string key) {
        return cache.GetCacheItem(key);
    }

    /// <inheritdoc />
    public void Set(string key, T value, DateTimeOffset absoluteExpiration) {
        cache.Set(key, value, absoluteExpiration);
    }

    /// <inheritdoc />
    public void Set(CacheItem item, CacheItemPolicy policy) {
        cache.Set(item, policy);
    }

    /// <inheritdoc />
    public void Set(string key, T value, CacheItemPolicy policy) {
        cache.Set(key, value, policy);
    }

    /// <inheritdoc />
    public T? Remove(string key) {
        return (T?) cache.Remove(key);
    }

    /// <inheritdoc />
    public T? Remove(string key, CacheEntryRemovedReason reason) {
        return (T?) cache.Remove(key, reason);
    }

    /// <inheritdoc />
    public long GetCount(string? regionName = null) {
        return cache.GetCount(regionName);
    }

    /// <inheritdoc />
    public long GetLastSize(string? regionName = null) {
        return cache.GetLastSize(regionName);
    }

    /// <inheritdoc />
    public IDictionary<string, T> GetValues(IEnumerable<string> keys) {
        return (IDictionary<string, T>) cache.GetValues(keys);
    }

    /// <inheritdoc />
    public long CacheMemoryLimit => cache.CacheMemoryLimit;

    /// <inheritdoc />
    public DefaultCacheCapabilities DefaultCacheCapabilities => cache.DefaultCacheCapabilities;

    /// <inheritdoc />
    public string Name => cache.Name;

    /// <inheritdoc />
    public long PhysicalMemoryLimit => cache.PhysicalMemoryLimit;

    /// <inheritdoc />
    public TimeSpan PollingInterval => cache.PollingInterval;

    /// <inheritdoc />
    public T? this[string key] {
        get => (T?) cache[key];
        set => cache[key] = value;
    }

    #endregion

}