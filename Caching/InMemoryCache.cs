using System.Collections.Concurrent;
using System.Timers;
using Timer = System.Timers.Timer;

namespace Unfucked.Caching;

public class InMemoryCache<K, V>: Cache<K, V> where K: notnull {

    public event RemovalNotification<K, V>? Removal;

    private readonly CacheOptions                           options;
    private readonly Func<K, ValueTask<V>>?                 defaultLoader;
    private readonly ConcurrentDictionary<K, CacheEntry<V>> cache;
    private readonly Timer?                                 expirationTimer;

    private volatile bool isDisposed;

    public InMemoryCache(CacheOptions? options = null, Func<K, ValueTask<V>>? loader = null) {
        this.options = (options ?? new CacheOptions()) with {
            ConcurrencyLevel = this.options.ConcurrencyLevel is > 0 and var c ? c : Environment.ProcessorCount,
            InitialCapacity = this.options.InitialCapacity is > 0 and var i ? i : 31
        };

        defaultLoader = loader;
        cache         = new ConcurrentDictionary<K, CacheEntry<V>>(this.options.ConcurrencyLevel, this.options.InitialCapacity);

        if (this.options.ExpireAfterWrite > TimeSpan.Zero || this.options.ExpireAfterRead > TimeSpan.Zero) {
            TimeSpan expirationScanInterval = this.options.ExpirationScanInterval is { Ticks: > 0 } interval ? interval : TimeSpan.FromMinutes(1);
            expirationTimer         =  new Timer(expirationScanInterval.TotalMilliseconds) { AutoReset = false };
            expirationTimer.Elapsed += ScanForExpirations;
            expirationTimer.Start();
        }
    }

    /// <inheritdoc />
    public long Count => cache.Count;

    /// <inheritdoc />
    public async Task<V> Get(K key, Func<K, ValueTask<V>>? loader = null) {
        CacheEntry<V> cacheEntry = cache.GetOrAdd(key, ValueFactory);
        if (cacheEntry.IsNew) {
            await cacheEntry.ValueLock.WaitAsync().ConfigureAwait(false);
            try {
                if (cacheEntry.IsNew) {
                    cacheEntry.IsNew = false;
                    cacheEntry.Value = await LoadValue(key, loader ?? defaultLoader).ConfigureAwait(false);
                    cacheEntry.LastWritten.Start();
                    cacheEntry.RefreshTimer?.Start();
                }
            } catch (KeyNotFoundException) {
                cache.TryRemove(key, out _);
                cacheEntry.Dispose();
                throw;
            } finally {
                if (!cacheEntry.IsDisposed) {
                    cacheEntry.ValueLock.Release();
                }
            }
        } else if (IsExpired(cacheEntry)) {
            V? oldValue = default;
            await cacheEntry.ValueLock.WaitAsync().ConfigureAwait(false);
            try {
                if (IsExpired(cacheEntry)) {
                    cacheEntry.RefreshTimer?.Stop();
                    try {
                        oldValue         = cacheEntry.Value;
                        cacheEntry.Value = await LoadValue(key, loader ?? defaultLoader).ConfigureAwait(false);
                        cacheEntry.LastWritten.Restart();
                    } finally {
                        cacheEntry.RefreshTimer?.Start();
                    }
                }
            } finally {
                cacheEntry.ValueLock.Release();
            }

            if (oldValue is not null) {
                Removal?.Invoke(this, key, oldValue, RemovalCause.EXPIRED);
            }
        }

        cacheEntry.LastRead.Restart();
        return cacheEntry.Value;
    }

    /// <inheritdoc />
    public async Task Put(K key, V value) {
        CacheEntry<V> cacheEntry = cache.GetOrAdd(key, ValueFactory);
        await cacheEntry.ValueLock.WaitAsync().ConfigureAwait(false);
        V? removedValue = default;
        try {
            cacheEntry.RefreshTimer?.Stop();
            if (cacheEntry.IsNew) {
                cacheEntry.IsNew = false;
            } else {
                removedValue = cacheEntry.Value;
            }

            cacheEntry.Value = value;
            cacheEntry.LastWritten.Restart();
            cacheEntry.RefreshTimer?.Start();
        } finally {
            cacheEntry.ValueLock.Release();
        }

        if (removedValue is not null) {
            Removal?.Invoke(this, key, removedValue, RemovalCause.REPLACED);
        }
    }

    private CacheEntry<V> ValueFactory(K key) {
        bool   hasLoader    = defaultLoader != null;
        Timer? refreshTimer = options.RefreshAfterWrite > TimeSpan.Zero && hasLoader ? new Timer(options.RefreshAfterWrite.TotalMilliseconds) { AutoReset = false, Enabled = false } : null;
        var    entry        = new CacheEntry<V>(refreshTimer);

        if (entry.RefreshTimer != null) {
            async void refreshEntry(object o, ElapsedEventArgs elapsedEventArgs) {
                if (!entry.IsDisposed) {
                    try {
                        V oldValue;
                        await entry.ValueLock.WaitAsync().ConfigureAwait(false);
                        try {
                            oldValue    = entry.Value;
                            entry.Value = await defaultLoader!(key).ConfigureAwait(false);
                            entry.LastWritten.Restart();
                            entry.RefreshTimer.Start();
                        } finally {
                            entry.ValueLock.Release();
                        }
                        Removal?.Invoke(this, key, oldValue, RemovalCause.REPLACED);
                    } catch (ObjectDisposedException) { }
                } else {
                    entry.RefreshTimer.Elapsed -= refreshEntry;
                }
            }

            entry.RefreshTimer.Elapsed += refreshEntry;
        }

        return entry;
    }

    /// <exception cref="System.Collections.Generic.KeyNotFoundException">a value with the key <typeparamref name="K"/> was not found, and no <paramref name="loader"/> was not provided</exception>
    private static ValueTask<V> LoadValue(K key, Func<K, ValueTask<V>>? loader) {
        if (loader != null) {
            return loader(key);
        } else {
            throw KeyNotFoundException(key);
        }
    }

    private static KeyNotFoundException KeyNotFoundException(K key) => new(
        $"Value with key {key} not found in cache, and a loader function was not provided when constructing the {nameof(InMemoryCache<K, V>)} or getting the value.");

    private bool IsExpired(CacheEntry<V> cacheEntry) =>
        (options.ExpireAfterWrite > TimeSpan.Zero && options.ExpireAfterWrite <= cacheEntry.LastWritten.Elapsed)
        || (options.ExpireAfterRead > TimeSpan.Zero && options.ExpireAfterRead <= cacheEntry.LastRead.Elapsed);

    /// <inheritdoc />
    public void CleanUp() {
        foreach (KeyValuePair<K, CacheEntry<V>> entry in cache.Where(pair => IsExpired(pair.Value))) {
            entry.Value.ValueLock.Wait();
            bool removed = false;
            if (IsExpired(entry.Value)) {
                //this will probably throw a concurrent modification exception
                removed = cache.TryRemove(entry.Key, out _);
                if (removed) {
                    entry.Value.Dispose();
                    Removal?.Invoke(this, entry.Key, entry.Value.Value, RemovalCause.EXPIRED);
                }
            }

            if (!removed) {
                /*
                 * First pass showed entry as expired, but it was concurrently loaded or refreshed before this second pass check, so don't actually remove it.
                 * Only release if we didn't remove, because if we removed the entry and its lock were already disposed.
                 */
                entry.Value.ValueLock.Release();
            }
        }
    }

    private void ScanForExpirations(object? sender = null, ElapsedEventArgs? e = null) {
        CleanUp();
        expirationTimer!.Start();
    }

    /// <inheritdoc />
    public void Invalidate(params IEnumerable<K> keys) {
        foreach (K key in keys) {
            if (cache.TryRemove(key, out CacheEntry<V>? removedEntry)) {
                Removal?.Invoke(this, key, removedEntry.Value, RemovalCause.EXPLICIT);
                removedEntry.Dispose();
            }
        }
    }

    /// <inheritdoc />
    public void InvalidateAll() {
        KeyValuePair<K, CacheEntry<V>>[] toDispose = cache.ToArray();
        cache.Clear();
        foreach (KeyValuePair<K, CacheEntry<V>> entry in toDispose) {
            if (!isDisposed) {
                Removal?.Invoke(this, entry.Key, entry.Value.Value, RemovalCause.EXPLICIT);
            }
            entry.Value.Dispose();
        }
    }

    protected virtual void Dispose(bool disposing) {
        if (disposing) {
            isDisposed = true;
            if (expirationTimer != null) {
                expirationTimer.Elapsed -= ScanForExpirations;
                expirationTimer.Dispose();
            }
            InvalidateAll();
        }
    }

    /// <inheritdoc />
    public void Dispose() {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

}