namespace Unfucked.Caching;

public interface Cache<K, V>: IDisposable where K: notnull {

    event RemovalNotification<K, V> Removal;

    long Count { get; }

    /// <exception cref="System.Collections.Generic.KeyNotFoundException"></exception>
    Task<V> Get(K key, Func<K, Task<V>>? loader = null);

    Task Put(K key, V value);

    void CleanUp();
    void Invalidate(params K[] key);
    void InvalidateAll();

}