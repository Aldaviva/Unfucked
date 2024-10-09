namespace Unfucked.Caching;

public struct CacheOptions {

    public int      ConcurrencyLevel;
    public int      InitialCapacity;
    public TimeSpan RefreshAfterWrite;
    public TimeSpan ExpireAfterWrite;
    public TimeSpan ExpireAfterRead;
    public TimeSpan ExpirationScanInterval;

    // public long         MaximumSize;
    // public bool         RecordStats;
    // public long         maximumWeight;

}