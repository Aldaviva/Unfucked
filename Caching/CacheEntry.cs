using System.Diagnostics;
using Timer = System.Timers.Timer;

namespace Unfucked.Caching;

internal class CacheEntry<V>(Timer? refreshTimer): IDisposable {

    public readonly SemaphoreSlim ValueLock    = new(1);
    public readonly Stopwatch     LastRead     = new();
    public readonly Stopwatch     LastWritten  = new();
    public readonly Timer?        RefreshTimer = refreshTimer;

    public V    Value = default!;
    public bool IsNew = true;

    public volatile bool IsDisposed;

    /// <inheritdoc />
    public void Dispose() {
        IsDisposed = true;
        RefreshTimer?.Dispose();
        ValueLock.Dispose();
    }

}