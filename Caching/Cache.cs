namespace Unfucked.Caching;

public interface Cache<K, V>: IDisposable where K: notnull {

    event RemovalNotification<K, V> Removal;

    long Count { get; }

    // V this[K key] { get; set; }
    /// <exception cref="System.Collections.Generic.KeyNotFoundException"></exception>
    Task<V> Get(K key, Func<K, Task<V>>? loader = null);
    // IDictionary<K, V> GetAllPresent(IEnumerable<K> keys);

    Task Put(K key, V value);
    // void PutAll(IEnumerable<KeyValuePair<K, V>> entries);

    void CleanUp();
    void Invalidate(params K[] key);
    void InvalidateAll();

}