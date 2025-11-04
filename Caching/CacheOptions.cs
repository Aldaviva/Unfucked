namespace Unfucked.Caching;

public readonly struct CacheOptions {

    public int ConcurrencyLevel { get; init; }
    public int InitialCapacity { get; init; }
    public TimeSpan RefreshAfterWrite { get; init; }
    public TimeSpan ExpireAfterWrite { get; init; }
    public TimeSpan ExpireAfterRead { get; init; }
    public TimeSpan ExpirationScanInterval { get; init; }

    // public long         MaximumSize;
    // public bool         RecordStats;
    // public long         maximumWeight;

}