using Unfucked.Caching;

// ReSharper disable AccessToDisposedClosure

namespace Tests;

public class InMemoryCacheTest {

    private int loads;

    private event EventHandler<string>? stringLengthLoaded;

    private Task<int> StringLengthLoader(string s) {
        Interlocked.Increment(ref loads);
        int length = s.Length;
        stringLengthLoaded?.Invoke(this, s);
        return Task.FromResult(length);
    }

    [Fact]
    public async Task GetMissing() {
        using var cache = new InMemoryCache<string, int>();
        cache.Count.Should().Be(0);
        Func<Task> thrower = async () => await cache.Get("a");
        await thrower.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task PutAndGet() {
        using var cache = new InMemoryCache<string, int>();
        await cache.Put("a", 1);
        (await cache.Get("a")).Should().Be(1);
    }

    [Fact]
    public async Task LocalLoader() {
        using var cache = new InMemoryCache<string, int>();
        (await cache.Get("a", StringLengthLoader)).Should().Be(1);
    }

    [Fact]
    public async Task GlobalLoader() {
        using var cache = new InMemoryCache<string, int>(loader: StringLengthLoader);
        (await cache.Get("a")).Should().Be(1);
    }

    [Fact]
    public async Task BothLoaders() {
        using var cache = new InMemoryCache<string, int>(loader: s => Task.FromResult(s.Length + 1));
        (await cache.Get("a", StringLengthLoader)).Should().Be(1);
    }

    [Fact]
    public async Task LoadExpiredAndGet() {
        using var cache = new InMemoryCache<string, int>(new CacheOptions { ExpireAfterWrite = TimeSpan.FromMilliseconds(1) }, StringLengthLoader);
        loads.Should().Be(0);

        (await cache.Get("a")).Should().Be(1);

        await Task.Delay(50);
        loads.Should().Be(1);
        (await cache.Get("a")).Should().Be(1);
        loads.Should().Be(2);
    }

    [Fact]
    public async Task PutOverwriting() {
        using var cache = new InMemoryCache<string, int>();
        await cache.Put("a", 1);
        await cache.Put("a", 2);
        int actual = await cache.Get("a");
        actual.Should().Be(2);
    }

    [Fact]
    public async Task ExpiredAndGetWithoutLoader() {
        using var cache = new InMemoryCache<string, int>(new CacheOptions { ExpireAfterWrite = TimeSpan.FromMilliseconds(1) });
        await cache.Put("a", 1);
        await Task.Delay(50);

        Func<Task> thrower = async () => await cache.Get("a");
        await thrower.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task InvalidateOne() {
        using var cache = new InMemoryCache<string, int>();
        await cache.Put("a", 1);
        cache.Count.Should().Be(1);

        cache.Invalidate("a");
        cache.Count.Should().Be(0);
        Func<Task> thrower = async () => await cache.Get("a");
        await thrower.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task InvalidateMany() {
        using var cache = new InMemoryCache<string, int>();
        await cache.Put("a", 1);
        await cache.Put("bb", 2);
        await cache.Put("ccc", 3);
        cache.Count.Should().Be(3);

        cache.Invalidate("a", "bb");
        cache.Count.Should().Be(1);
        Func<Task> thrower = async () => await cache.Get("a");
        await thrower.Should().ThrowAsync<KeyNotFoundException>();
        thrower = async () => await cache.Get("bb");
        await thrower.Should().ThrowAsync<KeyNotFoundException>();
        (await cache.Get("ccc")).Should().Be(3);
    }

    [Fact]
    public async Task InvalidateAll() {
        using var cache = new InMemoryCache<string, int>();
        await cache.Put("a", 1);
        await cache.Put("bb", 2);
        await cache.Put("ccc", 3);
        cache.Count.Should().Be(3);

        cache.InvalidateAll();
        cache.Count.Should().Be(0);
        Func<Task> thrower = async () => await cache.Get("a");
        await thrower.Should().ThrowAsync<KeyNotFoundException>();
        thrower = async () => await cache.Get("bb");
        await thrower.Should().ThrowAsync<KeyNotFoundException>();
        thrower = async () => await cache.Get("ccc");
        await thrower.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task CleanUp() {
        using var cache = new InMemoryCache<string, int>(new CacheOptions { ExpireAfterWrite = TimeSpan.FromMilliseconds(1) });
        await cache.Put("a", 1);
        await cache.Put("bb", 2);
        cache.Count.Should().Be(2);
        await Task.Delay(50);

        cache.CleanUp();
        cache.Count.Should().Be(0);
    }

    [Fact]
    public async Task AutoExpire() {
        using var cache = new InMemoryCache<string, int>(new CacheOptions { ExpireAfterWrite = TimeSpan.FromMilliseconds(1), ExpirationScanInterval = TimeSpan.FromMilliseconds(50) });
        await cache.Put("a", 1);
        cache.Count.Should().Be(1);

        string?              removedKey   = null;
        int?                 removedValue = null;
        RemovalCause?        removalCause = null;
        TaskCompletionSource removed      = new();
        cache.Removal += (_, key, value, cause) => {
            removedKey   = key;
            removedValue = value;
            removalCause = cause;
            removed.SetResult();
        };

        await removed.Task.WaitAsync(TimeSpan.FromMilliseconds(5000));

        removedKey.Should().Be("a");
        removedValue.Should().Be(1);
        removalCause.Should().Be(RemovalCause.Expired);
        cache.Count.Should().Be(0);
    }

    [Fact]
    public async Task CleanUpNothing() {
        using var cache = new InMemoryCache<string, int>(new CacheOptions { ExpireAfterWrite = TimeSpan.FromDays(1) });
        await cache.Put("a", 1);
        cache.Count.Should().Be(1);

        cache.CleanUp();

        cache.Count.Should().Be(1);
        (await cache.Get("a")).Should().Be(1);
    }

    [Fact]
    public async Task AutoRefresh() {
        TaskCompletionSource allLoadsCompleted = new();
        stringLengthLoaded += (_, _) => {
            if (loads >= 4) {
                allLoadsCompleted.TrySetResult();
            }
        };

        using var cache = new InMemoryCache<string, int>(new CacheOptions { RefreshAfterWrite = TimeSpan.FromMilliseconds(10) }, StringLengthLoader);
        await cache.Get("a");
        loads.Should().Be(1);

        await allLoadsCompleted.Task.WaitAsync(TimeSpan.FromMilliseconds(5000));
        loads.Should().BeGreaterOrEqualTo(4);
        cache.Count.Should().Be(1);
    }

}