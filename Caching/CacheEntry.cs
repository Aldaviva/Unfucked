using System.Diagnostics;
using Timer = System.Timers.Timer;

namespace Unfucked.Caching;

internal class CacheEntry<V>: IDisposable {

    public readonly SemaphoreSlim ValueLock   = new(1);
    public readonly Stopwatch     LastRead    = new();
    public readonly Stopwatch     LastWritten = new();
    public readonly Timer?        RefreshTimer;

    public V    Value = default!;
    public bool IsNew = true;

    public CacheEntry(bool initiallyLocked, Timer? refreshTimer) {
        RefreshTimer = refreshTimer;
        if (initiallyLocked) {
            ValueLock.Wait();
        }
    }

    /// <inheritdoc />
    public void Dispose() {
        RefreshTimer?.Dispose();
        ValueLock.Dispose();
    }

}