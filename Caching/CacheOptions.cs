namespace Unfucked.Caching;

/// <summary>Options that let you customize the behavior of a <see cref="Cache{K,V}"/>.</summary>
public readonly struct CacheOptions {

    /// <summary>Maximum number of threads that can be expected the access the cache concurrently.</summary>
    public int ConcurrencyLevel { get; init; }

    /// <summary>Number of keys that the cache can store at first, which can be automatically expanded later as more keys are added.</summary>
    public int InitialCapacity { get; init; }

    /// <summary>How long after a value is cached to wait before automatically loading a new value for its key in the background and caching it again, to ensure that the value always has some amount of freshness without having to call <see cref="Cache{K,V}.Get"/> and wait for a loader to run. By default, this is <see cref="TimeSpan.Zero"/>, which disables eager cache refreshing.</summary>
    public TimeSpan RefreshAfterWrite { get; init; }

    /// <summary>How long after caching a value to automatically remove it from the cache, so that its next read will regenerate it using a loader. By default, this is <see cref="TimeSpan.Zero"/>, which disables automatic expiration after writes. The frequency of expiration checks is set by <see cref="ExpirationScanInterval"/>.</summary>
    public TimeSpan ExpireAfterWrite { get; init; }

    /// <summary>How long after getting a cached value to automatically remove it from the cache, so that its next read will regenerate it using a loader. By default, this is <see cref="TimeSpan.Zero"/>, which disables automatic expiration after reads. The frequency of expiration checks is set by <see cref="ExpirationScanInterval"/>.</summary>
    public TimeSpan ExpireAfterRead { get; init; }

    /// <summary>Frequency of checks for cached values that are expired to be removed. By default, this is 1 minute, so the expiration durations are a lower bound, with a range of this interval. Has no effect if both <see cref="ExpireAfterRead"/> and <see cref="ExpireAfterWrite"/> are <see cref="TimeSpan.Zero"/>.</summary>
    public TimeSpan ExpirationScanInterval { get; init; }

    // public long         MaximumSize;
    // public bool         RecordStats;
    // public long         maximumWeight;

}