using System.Collections.ObjectModel;

namespace Unfucked;

/// <summary>
/// Easily create singleton collections
/// </summary>
public static class Singleton {

    /// <summary>
    /// Create a read-only singleton dictionary
    /// </summary>
    /// <typeparam name="K">key type</typeparam>
    /// <typeparam name="V">value type</typeparam>
    /// <param name="key">single key</param>
    /// <param name="value">single value</param>
    /// <returns>A read-only dictionary with the one specified <paramref name="key"/> and <paramref name="value"/>.</returns>
    public static IReadOnlyDictionary<K, V> Dictionary<K, V>(K key, V value) where K: notnull => new ReadOnlyDictionary<K, V>(new Dictionary<K, V>(1) { { key, value } });

    /// <summary>
    /// Create a singleton enumerable with one key-value pair
    /// </summary>
    /// <typeparam name="K">key type</typeparam>
    /// <typeparam name="V">value type</typeparam>
    /// <param name="key">single key</param>
    /// <param name="value">single value</param>
    /// <returns>An enumerable with the one specified <paramref name="key"/> and <paramref name="value"/> pair.</returns>
    public static IEnumerable<KeyValuePair<K, V>> KeyValues<K, V>(K key, V value) => [new(key, value)];

    /// <summary>
    /// Create a singleton set
    /// </summary>
    /// <typeparam name="T">item type</typeparam>
    /// <param name="item">single item</param>
    /// <param name="comparer">optional comparer used for item equality, or <c>null</c> to use the default <see cref="EqualityComparer{T}"/> for <typeparamref name="T"/></param>
    /// <returns>A set with the one specified <paramref name="item"/></returns>
    public static
#if NET5_0_OR_GREATER
        IReadOnlySet<T>
#else
        ISet<T>
#endif
        Set<T>(T item, IEqualityComparer<T>? comparer = null) => new HashSet<T>([item], comparer);

    /// <summary>
    /// Create a singleton enumerable with one key-value pair
    /// </summary>
    /// <typeparam name="K">key type</typeparam>
    /// <typeparam name="V">value type</typeparam>
    /// <param name="keyValuePair">single key-value tuple</param>
    /// <returns>An enumerable with the one specified key-value pair</returns>
    public static IEnumerable<KeyValuePair<K, V>> KeyValues<K, V>(this (K, V) keyValuePair) => [new(keyValuePair.Item1, keyValuePair.Item2)];

}