using System.Collections;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;

namespace Unfucked;

public static class Collections {

    /// <summary>Remove null values.</summary>
    /// <param name="source">enumerable that may contain null values</param>
    /// <returns>Input enumerable with null values removed.</returns>
    [Pure]
    public static IEnumerable<T> Compact<T>(this IEnumerable<T?> source) where T: class => source.Where(item => item != null)!;

    // Not using <inheritdoc> for any Compact() overloads because it seems to cause ServiceHub.RoslynCodeAnalysisService.exe to crash repeatedly
    // <inheritdoc cref="Compact{T}(IEnumerable{T?})" />
    /// <summary>Remove null values.</summary>
    /// <param name="source">enumerable that may contain null values</param>
    /// <returns>Input enumerable with null values removed.</returns>
    [Pure]
    public static IEnumerable<T> Compact<T>(this IEnumerable<T?> source) where T: struct => source.Where(item => item != null).Cast<T>();

    /// <summary>Remove null values.</summary>
    /// <param name="source">array that may contain null values</param>
    /// <returns>Copy of input array with null values removed.</returns>
    [Pure]
    public static T[] Compact<T>(this T?[] source) where T: class => source.Where(item => item != null).ToArray()!;

    // <inheritdoc cref="Compact{T}(T?[])" />
    /// <summary>Remove null values.</summary>
    /// <param name="source">array that may contain null values</param>
    /// <returns>Copy of input array with null values removed.</returns>
    [Pure]
    public static T[] Compact<T>(this T?[] source) where T: struct => source.Where(item => item != null).Cast<T>().ToArray();

    /// <summary>
    /// Remove <c>null</c> values from a dictionary.
    /// </summary>
    /// <typeparam name="TKey">key type of the dictionary</typeparam>
    /// <typeparam name="TValue">value type of the dictionary</typeparam>
    /// <param name="source">Dictionary with possible <c>null</c> values. This dictionary and its values are not modified by this method.</param>
    /// <returns>A copy of <paramref name="source"/> where all the <c>null</c> values have been removed.</returns>
    [Pure]
    public static IDictionary<TKey, TValue> Compact<TKey, TValue>(this IDictionary<TKey, TValue?> source) where TKey: notnull where TValue: class =>
        source.Where(entry => entry.Value != null).ToDictionary(entry => entry.Key, entry => entry.Value!);

    // <inheritdoc cref="Compact{TKey,TValue}(IDictionary{TKey, TValue?})" />
    /// <summary>
    /// Remove <c>null</c> values from a dictionary.
    /// </summary>
    /// <typeparam name="TKey">key type of the dictionary</typeparam>
    /// <typeparam name="TValue">value type of the dictionary</typeparam>
    /// <param name="source">Dictionary with possible <c>null</c> values. This dictionary and its values are not modified by this method.</param>
    /// <returns>A copy of <paramref name="source"/> where all the <c>null</c> values have been removed.</returns>
    [Pure]
    public static IDictionary<TKey, TValue> Compact<TKey, TValue>(this IDictionary<TKey, TValue?> source) where TKey: notnull where TValue: struct =>
        source.Where(entry => entry.Value != null).ToDictionary(entry => entry.Key, pair => pair.Value!.Value);

    public static IEnumerable<KeyValuePair<TKey, TValue>> Compact<TKey, TValue>(this IEnumerable<KeyValuePair<TKey, TValue?>> source) where TKey: notnull where TValue: struct =>
        from pair in source
        where pair.Value.HasValue
        select new KeyValuePair<TKey, TValue>(pair.Key, pair.Value!.Value);

    public static IEnumerable<KeyValuePair<TKey, TValue>> Compact<TKey, TValue>(this IEnumerable<KeyValuePair<TKey, TValue?>> source) where TKey: notnull where TValue: class =>
        from pair in source
        where pair.Value != null
        select new KeyValuePair<TKey, TValue>(pair.Key, pair.Value);

    public static void AddAll<T>(this ICollection<T> destination, IEnumerable<T> source) {
        foreach (T item in source) {
            destination.Add(item);
        }
    }

    /// <summary>
    /// <para>Returns an existing value from a dictionary, or if it wasn't already present, add it.</para>
    /// <para>Not atomic.</para>
    /// </summary>
    /// <typeparam name="TKey">type of <paramref name="dictionary"/> keys</typeparam>
    /// <typeparam name="TValue">type of <paramref name="dictionary"/> values</typeparam>
    /// <param name="dictionary">writable dictionary that may contain <paramref name="key"/></param>
    /// <param name="key">key to look up or insert into the <paramref name="dictionary"/></param>
    /// <param name="newValue">the new value to insert if <paramref name="key"/> is not found</param>
    /// <param name="added">after this method returns, this will be set to <c>true</c> if the new value was added to <paramref name="dictionary"/>, or <c>false</c> if a value with key <paramref name="key"/> was already present</param>
    /// <returns>the existing value with key <paramref name="key"/>, or the new value if it was not already present</returns>
    public static TValue GetOrAdd<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, TValue newValue, out bool added) {
        added = !dictionary.TryGetValue(key, out TValue? oldValue);
        if (added) {
            dictionary.Add(key, newValue);
            return newValue;
        } else {
            return oldValue;
        }
    }

    /// <summary>
    /// <para>Returns an existing value from a dictionary, or if it wasn't already present, add it.</para>
    /// <para>Not atomic.</para>
    /// </summary>
    /// <typeparam name="TKey">type of <paramref name="dictionary"/> keys</typeparam>
    /// <typeparam name="TValue">type of <paramref name="dictionary"/> values</typeparam>
    /// <param name="dictionary">writable dictionary that may contain <paramref name="key"/></param>
    /// <param name="key">key to look up or insert into the <paramref name="dictionary"/></param>
    /// <param name="newValueFactory">Function that lazily returns the new value to add if <paramref name="key"/> is not already present</param>
    /// <param name="added">after this method returns, this will be set to <c>true</c> if the new value was added to <paramref name="dictionary"/>, or <c>false</c> if a value with key <paramref name="key"/> was already present</param>
    /// <returns>the existing value with key <paramref name="key"/>, or the new value if it was not already present</returns>
    public static TValue GetOrAdd<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, Func<TValue> newValueFactory, out bool added) {
        added = !dictionary.TryGetValue(key, out TValue? oldValue);
        if (added) {
            TValue newValue = newValueFactory();
            dictionary.Add(key, newValue);
            return newValue;
        } else {
            return oldValue;
        }
    }

    /// <summary>
    /// <para>Returns an existing value from a dictionary, or if it wasn't already present, add it.</para>
    /// <para>Not atomic.</para>
    /// </summary>
    /// <typeparam name="TKey">type of <paramref name="dictionary"/> keys</typeparam>
    /// <typeparam name="TValue">type of <paramref name="dictionary"/> values</typeparam>
    /// <param name="dictionary">writable dictionary that may contain <paramref name="key"/></param>
    /// <param name="key">key to look up or insert into the <paramref name="dictionary"/></param>
    /// <param name="newValueFactory">Function that lazily returns the new value to add if <paramref name="key"/> is not already present</param>
    /// <returns>a tuple that contains the existing value with key <paramref name="key"/>, or the new value if it was not already present, as well as a boolean that shows whether the value was added to the dictionary in this method invocation</returns>
    public static async Task<(TValue value, bool added)> GetOrAdd<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, Func<Task<TValue>> newValueFactory) {
        if (!dictionary.TryGetValue(key, out TValue? oldValue)) {
            TValue newValue = await newValueFactory().ConfigureAwait(false);
            dictionary.Add(key, newValue);
            return (value: newValue, added: true);
        } else {
            return (value: oldValue, added: false);
        }
    }

    /// <summary>
    /// Set difference
    /// </summary>
    /// <typeparam name="T">type of items</typeparam>
    /// <param name="a">set or other enumerable from which you want to subtract <paramref name="b"/></param>
    /// <param name="b">set or other enumerable which you want to subtract from <paramref name="a"/></param>
    /// <returns>a new set containing the result of <c>a−b</c></returns>
    [Pure]
    public static ISet<T> Minus<T>(this IEnumerable<T> a, IEnumerable<T> b) {
        var difference = new HashSet<T>(a);
        difference.ExceptWith(b);
        return difference;
    }

    [Pure]
    public static IEnumerable<T> DistinctConsecutive<T>(this IEnumerable<T> source, IEqualityComparer<T>? comparer = null) {
        comparer ??= EqualityComparer<T>.Default;
        T?   previousItem = default;
        bool isFirstItem  = true;

        foreach (T item in source) {
            if (isFirstItem || !comparer.Equals(previousItem, item)) {
                yield return item;
            }

            previousItem = item;
            isFirstItem  = false;
        }
    }

    [Pure]
    public static async Task<IReadOnlyCollection<T>> ToList<T>(this IAsyncEnumerator<T> source) {
        if (!await source.MoveNextAsync().ConfigureAwait(false)) {
            return [];
        }

        IList<T> result = [source.Current];
        while (await source.MoveNextAsync().ConfigureAwait(false)) {
            result.Add(source.Current);
        }

        return new ReadOnlyCollection<T>(result);
    }

    /// <summary>
    /// Like <see cref="Enumerable.FirstOrDefault{TSource}(System.Collections.Generic.IEnumerable{TSource})"/>, but returns <c>null</c> for value types instead of <c>default</c> when <paramref name="source"/> is empty, because nullable chaining and coalescing is easier than ambiguous defaults that can't be chained.
    /// </summary>
    /// <typeparam name="TSource">type of item in <paramref name="source"/></typeparam>
    /// <param name="source">sequence of items</param>
    /// <returns>the first item in <paramref name="source"/>, or <c>null</c> if it it empty</returns>
    [Pure]
    public static TSource? FirstOrNull<TSource>(this IEnumerable<TSource> source) where TSource: struct {
        if (source is IList<TSource> list) {
            if (list.Count > 0) {
                return list[0];
            }
        } else {
            using IEnumerator<TSource> enumerator = source.GetEnumerator();
            if (enumerator.MoveNext()) {
                return enumerator.Current;
            }
        }

        return null;
    }

    /// <summary>
    /// Like <see cref="Enumerable.FirstOrDefault{TSource}(System.Collections.Generic.IEnumerable{TSource},System.Func{TSource,bool})"/>, but returns <c>null</c> for value types instead of <c>default</c> when <paramref name="source"/> has no items that match <paramref name="predicate"/>, because nullable chaining and coalescing is easier than ambiguous defaults that can't be chained.
    /// </summary>
    /// <typeparam name="TSource">type of item in <paramref name="source"/></typeparam>
    /// <param name="source">sequence of items</param>
    /// <param name="predicate">function that should return <c>true</c> when the passed in item from <paramref name="source"/> should be returned, or <c>false</c> to not return it</param>
    /// <returns>the first item in <paramref name="source"/> that causes <paramref name="predicate"/> to return <c>true</c>, or <c>null</c> if it it empty or every item causes <paramref name="predicate"/> to return <c>false</c></returns>
    [Pure]
    public static TSource? FirstOrNull<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate) where TSource: struct {
        foreach (TSource element in source) {
            if (predicate(element)) {
                return element;
            }
        }

        return null;
    }

    public static (T? head, IEnumerable<T> tail) HeadAndTail<T>(this IEnumerable<T> source) where T: class? {
        using IEnumerator<T> enumerator = source.GetEnumerator();
        return (head: enumerator.MoveNext() ? enumerator.Current : null, tail: new Enumerable<T>(enumerator));
    }

    public static (T? head, IEnumerable<T> tail) HeadAndTailStruct<T>(this IEnumerable<T> source) where T: struct {
        using IEnumerator<T> enumerator = source.GetEnumerator();
        return (head: enumerator.MoveNext() ? enumerator.Current : null, tail: new Enumerable<T>(enumerator));
    }

    public static (T? head, IEnumerable<T?> tail) HeadAndTailStruct<T>(this IEnumerable<T?> source) where T: struct {
        using IEnumerator<T?> enumerator = source.GetEnumerator();
        return (head: enumerator.MoveNext() ? enumerator.Current : null, tail: new Enumerable<T?>(enumerator));
    }

    private class Enumerable<T>(IEnumerator<T> enumerator): IEnumerable<T> {

        public IEnumerator<T> GetEnumerator() => enumerator;

        IEnumerator IEnumerable.GetEnumerator() => enumerator;

    }

    [Pure]
    public static ConcurrentDictionary<TKey, ValueHolder<TValue>> CreateConcurrentDictionary<TKey, TValue>() where TKey: notnull {
        return new ConcurrentDictionary<TKey, ValueHolder<TValue>>();
    }

    public static TValue ExchangeEnum<TKey, TValue>(this ConcurrentDictionary<TKey, ValueHolder<int>> dictionary, TKey key, TValue newValue) where TValue: Enum where TKey: notnull {
        int newValueInt = (int) Convert.ChangeType(newValue, newValue.GetTypeCode());
        return (TValue) Enum.ToObject(typeof(TValue), Interlocked.Exchange(ref dictionary.GetOrAdd(key, new ValueHolder<int>(newValueInt)).Value, newValueInt));
    }

    public static TValue Exchange<TKey, TValue>(this ConcurrentDictionary<TKey, ValueHolder<TValue>> dictionary, TKey key, TValue newValue) where TValue: class where TKey: notnull {
        return Interlocked.Exchange(ref dictionary.GetOrAdd(key, new ValueHolder<TValue>(newValue)).Value, newValue);
    }

    public static long Exchange<TKey>(this ConcurrentDictionary<TKey, ValueHolder<long>> dictionary, TKey key, long newValue) where TKey: notnull {
        return Interlocked.Exchange(ref dictionary.GetOrAdd(key, new ValueHolder<long>(newValue)).Value, newValue);
    }

    public static int Exchange<TKey>(this ConcurrentDictionary<TKey, ValueHolder<int>> dictionary, TKey key, int newValue) where TKey: notnull {
        return Interlocked.Exchange(ref dictionary.GetOrAdd(key, new ValueHolder<int>(newValue)).Value, newValue);
    }

    public static double Exchange<TKey>(this ConcurrentDictionary<TKey, ValueHolder<double>> dictionary, TKey key, double newValue) where TKey: notnull {
        return Interlocked.Exchange(ref dictionary.GetOrAdd(key, new ValueHolder<double>(newValue)).Value, newValue);
    }

    public class ValueHolder<T>(T value) {

        public T Value = value;

    }

    /// <summary>
    /// <para>Diff two lists, producing a delta of their contents.</para>
    /// <para>The list on which you call this extension method is considered to be the old or existing list, and the other list you pass as a parameter is the new or updated state of the list.</para>
    /// </summary>
    /// <typeparam name="T">the type of items</typeparam>
    /// <param name="oldList">the previous state of the list</param>
    /// <param name="newList">the new state of the list</param>
    /// <param name="isEqual">equality comparer for an item pair</param>
    /// <returns>a tuple of items that were <c>created</c> (in <paramref name="newList"/> but not in <paramref name="oldList"/>), <c>updated</c> (in both <paramref name="newList"/> and <paramref name="oldList"/> but not equal because at least one property changed), <c>deleted</c> (in <paramref name="oldList"/> but not in <paramref name="newList"/>), and <c>unmodified</c> (in both <paramref name="newList"/> and <paramref name="oldList"/>, and equal because no properties changed). Sorting of input items is preserved in outputs, except <c>deleted</c>, which is in an undefined order.</returns>
    [Pure]
    public static (IEnumerable<T> created, IEnumerable<T> updated, IEnumerable<T> deleted, IEnumerable<T> unmodified) DeltaWith<T>(
        this IEnumerable<T> oldList, IEnumerable<T> newList, IEqualityComparer<T>? isEqual = null) where T: notnull => oldList.DeltaWith(newList, item => item, isEqual);

    /// <summary>
    /// <para>Diff two lists, producing a delta of their contents.</para>
    /// <para>The list on which you call this extension method is considered to be the old or existing list, and the other list you pass as a parameter is the new or updated state of the list.</para>
    /// </summary>
    /// <typeparam name="T">the type of items</typeparam>
    /// <typeparam name="TId">the type of an identifier used to determine which items in <paramref name="oldList"/> and <paramref name="newList"/> are potentially different versions of the same item, even if their properties have changed</typeparam>
    /// <param name="oldList">the previous state of the list</param>
    /// <param name="newList">the new state of the list</param>
    /// <param name="idSelector">get the <typeparamref name="TId"/> from items in <paramref name="oldList"/> and <paramref name="newList"/></param>
    /// <param name="isEqual">equality comparer for an item pair</param>
    /// <returns>a tuple of items that were <c>created</c> (in <paramref name="newList"/> but not in <paramref name="oldList"/>), <c>updated</c> (in both <paramref name="newList"/> and <paramref name="oldList"/> but not equal because at least one property changed), <c>deleted</c> (in <paramref name="oldList"/> but not in <paramref name="newList"/>), and <c>unmodified</c> (in both <paramref name="newList"/> and <paramref name="oldList"/>, and equal because no properties changed). Sorting of input items is preserved in outputs, except <c>deleted</c>, which is in an undefined order.</returns>
    [Pure]
    public static (IEnumerable<T> created, IEnumerable<T> updated, IEnumerable<T> deleted, IEnumerable<T> unmodified) DeltaWith<T, TId>(
        this IEnumerable<T> oldList, IEnumerable<T> newList, Func<T, TId> idSelector, IEqualityComparer<T>? isEqual = null) where T: notnull where TId: notnull =>
        oldList.DeltaWith(newList, idSelector, idSelector, (isEqual ?? EqualityComparer<T>.Default).Equals);

    /// <summary>
    /// <para>Diff two lists, producing a delta of their contents.</para>
    /// <para>The list on which you call this extension method is considered to be the old or existing list, and the other list you pass as a parameter is the new or updated state of the list.</para>
    /// </summary>
    /// <typeparam name="TOld">the type of existing items</typeparam>
    /// <typeparam name="TNew">the type of new items, may be the same as <typeparamref name="TOld"/></typeparam>
    /// <typeparam name="TId">the type of an identifier used to determine which items in <paramref name="oldList"/> and <paramref name="newList"/> are potentially different versions of the same item, even if their properties have changed</typeparam>
    /// <param name="oldList">the previous state of the list</param>
    /// <param name="newList">the new state of the list</param>
    /// <param name="oldIdSelector">get the <typeparamref name="TOld"/> ID from items in <paramref name="oldList"/></param>
    /// <param name="newIdSelector">get the <typeparamref name="TNew"/> ID from items in <paramref name="newList"/></param>
    /// <param name="isEqual">equality comparer for an <typeparamref name="TOld"/> and <typeparamref name="TNew"/> item pair</param>
    /// <returns>a tuple of items that were <c>created</c> (in <paramref name="newList"/> but not in <paramref name="oldList"/>), <c>updated</c> (in both <paramref name="newList"/> and <paramref name="oldList"/> but not equal because at least one property changed), <c>deleted</c> (in <paramref name="oldList"/> but not in <paramref name="newList"/>), and <c>unmodified</c> (in both <paramref name="newList"/> and <paramref name="oldList"/>, and equal because no properties changed). Sorting of input items is preserved in outputs, except <c>deleted</c>, which is in an undefined order.</returns>
    [Pure]
    public static (IEnumerable<TNew> created, IEnumerable<TNew> updated, IEnumerable<TOld> deleted, IEnumerable<TOld> unmodified) DeltaWith<TOld, TNew, TId>(
        this IEnumerable<TOld> oldList, IEnumerable<TNew> newList, Func<TOld, TId> oldIdSelector, Func<TNew, TId> newIdSelector, Func<TOld, TNew, bool>? isEqual = null)
        where TOld: notnull where TNew: notnull where TId: notnull {

        isEqual ??= (a, b) => Equals(a, b);
        List<TNew>             created      = [];
        List<TNew>             updated      = [];
        var                    deleted      = new HashSet<TOld>(oldList);
        List<TOld>             unmodified   = [];
        IDictionary<TId, TOld> oldItemsById = deleted.ToDictionary(oldIdSelector);

        foreach (TNew newItem in newList) {
            TId id = newIdSelector(newItem);
            if (oldItemsById.TryGetValue(id, out TOld? oldItem)) {
                if (isEqual(oldItem, newItem)) {
                    unmodified.Add(oldItem);
                } else {
                    updated.Add(newItem);
                }
                deleted.Remove(oldItem);
            } else {
                created.Add(newItem);
            }
        }

        return (created, updated, deleted, unmodified);
    }

}