using System.Collections;

namespace Unfucked;

/// <summary>
/// Easily create singleton collections
/// </summary>
public static class Singletons {

    /// <typeparam name="K">key type</typeparam>
    /// <typeparam name="V">value type</typeparam>
    extension<K, V>(IReadOnlyDictionary<K, V>) where K: notnull {

        /// <summary>
        /// Create a read-only singleton dictionary
        /// </summary>
        /// <param name="key">single key</param>
        /// <param name="value">single value</param>
        /// <returns>A read-only dictionary with the one specified <paramref name="key"/> and <paramref name="value"/>.</returns>
        public static IReadOnlyDictionary<K, V> Singleton(K key, V value, IEqualityComparer<K>? keyComparer = null) => new ReadOnlySingletonDictionary<K, V>(key, value, keyComparer);

    }

    /// <typeparam name="T">item type</typeparam>
    extension<T>(ISet<T>) {

        /// <summary>
        /// Create a singleton set
        /// </summary>
        /// <param name="item">single item</param>
        /// <param name="comparer">optional comparer used for item equality, or <c>null</c> to use the default <see cref="EqualityComparer{T}"/> for <typeparamref name="T"/></param>
        /// <returns>A set with the one specified <paramref name="item"/></returns>
        public static IReadOnlySingletonSet<T> Singleton(T item, IEqualityComparer<T>? comparer = null) => new ReadOnlySingletonSet<T>(item, comparer);

    }

    /// <summary>
    /// Create a singleton enumerable with one key-value pair
    /// </summary>
    /// <typeparam name="K">key type</typeparam>
    /// <typeparam name="V">value type</typeparam>
    /// <param name="keyValuePair">single key-value tuple</param>
    /// <returns>An enumerable with the one specified key-value pair</returns>
    public static IEnumerable<KeyValuePair<K, V>> KeyValues<K, V>(this (K key, V value) keyValuePair) => [new(keyValuePair.key, keyValuePair.value)];

    /// <summary>
    /// A set that contains exactly one value and cannot be modified.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    // ReSharper disable once PossibleInterfaceMemberAmbiguity
    public interface IReadOnlySingletonSet<T>:
#if NET5_0_OR_GREATER
        IReadOnlySet<T>,
#endif
        ISet<T>;

    private sealed class ReadOnlySingletonSet<T>(T singleton, IEqualityComparer<T>? comparer): IReadOnlySingletonSet<T> {

        private readonly IEqualityComparer<T> comparer = comparer ?? EqualityComparer<T>.Default;

        public int Count => 1;
        public bool IsReadOnly => true;

        public IEnumerator<T> GetEnumerator() => new SingletonEnumerator<T>(singleton);
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public bool IsProperSupersetOf(IEnumerable<T> other) => !other.Any();
        public bool IsSubsetOf(IEnumerable<T> other) => other.Contains(singleton, comparer);
        public bool IsSupersetOf(IEnumerable<T> other) => other.All(item => comparer.Equals(singleton, item));
        public bool Overlaps(IEnumerable<T> other) => IsSubsetOf(other);

        public bool IsProperSubsetOf(IEnumerable<T> other) {
            bool foundItem = false, foundDifferentItem = false;
            foreach (T otherItem in other) {
                bool isItem = comparer.Equals(singleton, otherItem);
                foundItem          |= isItem;
                foundDifferentItem |= !isItem;
                if (foundItem && foundDifferentItem) {
                    return true;
                }
            }
            return false;
        }

        public bool SetEquals(IEnumerable<T> other) {
            bool empty = true;
            foreach (T otherItem in other) {
                empty = false;
                if (!comparer.Equals(singleton, otherItem)) {
                    return false;
                }
            }
            return !empty;
        }

        public bool Contains(T item) => comparer.Equals(singleton, item);
        public void CopyTo(T[] array, int arrayIndex) => array[arrayIndex] = singleton;

        void ICollection<T>.Add(T item) => throw new NotSupportedException();
        public void ExceptWith(IEnumerable<T> other) => throw new NotSupportedException();
        public void IntersectWith(IEnumerable<T> other) => throw new NotSupportedException();
        public void SymmetricExceptWith(IEnumerable<T> other) => throw new NotSupportedException();
        public void UnionWith(IEnumerable<T> other) => throw new NotSupportedException();
        bool ISet<T>.Add(T item) => throw new NotSupportedException();
        public void Clear() => throw new NotSupportedException();
        public bool Remove(T item) => throw new NotSupportedException();

    }

    private sealed class SingletonEnumerator<T>(T singleton): IEnumerator<T> {

        private int position;

        public T Current => position == 1 ? singleton : default!;
        object? IEnumerator.Current => Current;

        public bool MoveNext() => 0 == Interlocked.CompareExchange(ref position, 1, 0);

        public void Reset() => throw new NotSupportedException();
        public void Dispose() {}

    }

    internal sealed class ReadOnlySingletonDictionary<K, V>(K singletonKey, V singletonValue, IEqualityComparer<K>? keyComparer): IReadOnlyDictionary<K, V>, IDictionary<K, V> where K: notnull {

        private readonly KeyValuePair<K, V>   pair        = new(singletonKey, singletonValue);
        private readonly IEqualityComparer<K> keyComparer = keyComparer ?? EqualityComparer<K>.Default;

        public int Count => 1;
        public bool IsReadOnly => true;

        /// <inheritdoc cref="IReadOnlyDictionary{TKey,TValue}.this" />
        public V this[K key] {
            get => keyComparer.Equals(singletonKey, key) ? singletonValue : throw new KeyNotFoundException();
            set => throw new NotSupportedException();
        }

        public IEnumerator<KeyValuePair<K, V>> GetEnumerator() => new SingletonEnumerator<KeyValuePair<K, V>>(pair);
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public ICollection<K> Keys => [singletonKey];
        public ICollection<V> Values => [singletonValue];
        IEnumerable<K> IReadOnlyDictionary<K, V>.Keys => Keys;
        IEnumerable<V> IReadOnlyDictionary<K, V>.Values => Values;

        public bool Contains(KeyValuePair<K, V> item) => ContainsKey(item.Key) && singletonValue is not null ? singletonValue.Equals(item.Value) : item.Value is null;
        public bool ContainsKey(K key) => keyComparer.Equals(singletonKey, key);
        public void CopyTo(KeyValuePair<K, V>[] array, int arrayIndex) => array[arrayIndex] = pair;

        public bool TryGetValue(K key,
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
                                [MaybeNullWhen(false)]
#endif
                                out V value) {
            bool isFound = keyComparer.Equals(key);
            value = isFound ? singletonValue : default!;
            return isFound;
        }

        public void Add(K key, V value) => throw new NotSupportedException();
        public void Add(KeyValuePair<K, V> item) => throw new NotSupportedException();
        public void Clear() => throw new NotSupportedException();
        public bool Remove(KeyValuePair<K, V> item) => throw new NotSupportedException();
        public bool Remove(K key) => throw new NotSupportedException();

    }

    /// <typeparam name="K">key type</typeparam>
    /// <typeparam name="V">value type</typeparam>
    extension<K, V>(IEnumerable<KeyValuePair<K, V>>) {

        /// <summary>
        /// Create a singleton enumerable with one key-value pair
        /// </summary>
        /// <param name="key">single key</param>
        /// <param name="value">single value</param>
        /// <returns>An enumerable with the one specified <paramref name="key"/> and <paramref name="value"/> pair.</returns>
        public static IEnumerable<KeyValuePair<K, V>> Singleton(K key, V value) => [new(key, value)];

    }

}

/// <inheritdoc cref="Singletons" />
public static class Singletons2 {

    extension(IDictionary) {

        /// <summary>
        /// Create a read-only singleton dictionary
        /// </summary>
        /// <typeparam name="K">key type</typeparam>
        /// <typeparam name="V">value type</typeparam>
        /// <param name="key">single key</param>
        /// <param name="value">single value</param>
        /// <returns>A read-only dictionary with the one specified <paramref name="key"/> and <paramref name="value"/>.</returns>
        public static IReadOnlyDictionary<K, V> Singleton<K, V>(K key, V value, IEqualityComparer<K>? keyComparer = null) where K: notnull =>
            new Singletons.ReadOnlySingletonDictionary<K, V>(key, value, keyComparer);

    }

}