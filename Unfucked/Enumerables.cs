using System.Collections;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;

namespace Unfucked;

/// <summary>
/// Methods that make it easier to work with enumerables and arrays.
/// </summary>
public static partial class Enumerables {

    /// <summary>Remove <c>null</c> values.</summary>
    /// <param name="source">Enumerable that may contain <c>null</c> values</param>
    /// <returns>Input enumerable with <c>null</c> values removed.</returns>
    /// <remarks>Inspired by Underscore.js' <c>_.compact()</c>: <see href="https://underscorejs.org/#compact"/></remarks>
    [Pure]
    public static IEnumerable<T> Compact<T>(this IEnumerable<T?> source) where T: class => source.Where(item => item != null)!;

    // Not using <inheritdoc> for any Compact() overloads because it seems to cause ServiceHub.RoslynCodeAnalysisService.exe to crash repeatedly
    // <inheritdoc cref="Compact{T}(IEnumerable{T?})" />
    /// <summary>Remove <c>null</c> values.</summary>
    /// <param name="source">Enumerable that may contain <c>null</c> values</param>
    /// <returns>Input enumerable with <c>null</c> values removed.</returns>
    [Pure]
    public static IEnumerable<T> Compact<T>(this IEnumerable<T?> source) where T: struct => source.Where(item => item != null).Cast<T>();

    /// <summary>Remove <c>null</c> values.</summary>
    /// <param name="source">Array that may contain <c>null</c> values</param>
    /// <returns>Copy of input array with <c>null</c> values removed.</returns>
    [Pure]
    public static T[] Compact<T>(this T?[] source) where T: class => source.Where(item => item != null).ToArray()!;

    // <inheritdoc cref="Compact{T}(T?[])" />
    /// <summary>Remove <c>null</c> values.</summary>
    /// <param name="source">Array that may contain <c>null</c> values</param>
    /// <returns>Copy of input array with <c>null</c> values removed.</returns>
    [Pure]
    public static T[] Compact<T>(this T?[] source) where T: struct => source.Where(item => item != null).Cast<T>().ToArray();

    /// <summary>
    /// Remove <c>null</c> values from a dictionary.
    /// </summary>
    /// <typeparam name="TKey">Key type of the dictionary</typeparam>
    /// <typeparam name="TValue">Value type of the dictionary</typeparam>
    /// <param name="source">Dictionary with possible <c>null</c> values. This dictionary and its values are not modified by this method.</param>
    /// <returns>A copy of <paramref name="source"/> where all the <c>null</c> values have been removed.</returns>
    [Pure]
    public static IDictionary<TKey, TValue> Compact<TKey, TValue>(this IDictionary<TKey, TValue?> source) where TKey: notnull where TValue: class =>
        source.Where(entry => entry.Value != null).ToDictionary(entry => entry.Key, entry => entry.Value!);

    // <inheritdoc cref="Compact{TKey,TValue}(IDictionary{TKey, TValue?})" />
    /// <summary>
    /// Remove <c>null</c> values from a dictionary.
    /// </summary>
    /// <typeparam name="TKey">Key type of the dictionary</typeparam>
    /// <typeparam name="TValue">Value type of the dictionary</typeparam>
    /// <param name="source">Dictionary with possible <c>null</c> values. This dictionary and its values are not modified by this method.</param>
    /// <returns>A copy of <paramref name="source"/> where all the <c>null</c> values have been removed.</returns>
    [Pure]
    public static IDictionary<TKey, TValue> Compact<TKey, TValue>(this IDictionary<TKey, TValue?> source) where TKey: notnull where TValue: struct =>
        source.Where(entry => entry.Value != null).ToDictionary(entry => entry.Key, pair => pair.Value!.Value);

    /// <summary>
    /// Remove <c>null</c> values from a dictionary.
    /// </summary>
    /// <typeparam name="TKey">Key type of the dictionary</typeparam>
    /// <typeparam name="TValue">Value type of the dictionary</typeparam>
    /// <param name="source">Dictionary with possible <c>null</c> values. This dictionary and its values are not modified by this method.</param>
    /// <returns>A copy of <paramref name="source"/> where all the <c>null</c> values have been removed.</returns>
    [Pure]
    public static IEnumerable<KeyValuePair<TKey, TValue>> Compact<TKey, TValue>(this IEnumerable<KeyValuePair<TKey, TValue?>> source) where TKey: notnull where TValue: struct =>
        from pair in source
        where pair.Value.HasValue
        select new KeyValuePair<TKey, TValue>(pair.Key, pair.Value!.Value);

    /// <summary>
    /// Remove <c>null</c> values from a dictionary.
    /// </summary>
    /// <typeparam name="TKey">Key type of the dictionary</typeparam>
    /// <typeparam name="TValue">Value type of the dictionary</typeparam>
    /// <param name="source">Dictionary with possible <c>null</c> values. This dictionary and its values are not modified by this method.</param>
    /// <returns>A copy of <paramref name="source"/> where all the <c>null</c> values have been removed.</returns>
    [Pure]
    public static IEnumerable<KeyValuePair<TKey, TValue>> Compact<TKey, TValue>(this IEnumerable<KeyValuePair<TKey, TValue?>> source) where TKey: notnull where TValue: class =>
        from pair in source
        where pair.Value != null
        select new KeyValuePair<TKey, TValue>(pair.Key, pair.Value);

    /// <summary>
    /// Copy each of the items in <paramref name="source"/> to this collection.
    /// </summary>
    /// <param name="destination">Items will be copied into here.</param>
    /// <param name="source">Items will be copied from here.</param>
    /// <typeparam name="T">Type of items in both enumerables.</typeparam>
    public static void AddAll<T>(this ICollection<T> destination, params T[]? source) {
        if (source is not null) {
            AddAll(destination, (IEnumerable<T>) source);
        }
    }

    /// <inheritdoc cref="AddAll{T}(System.Collections.Generic.ICollection{T},T[])" />
    public static void AddAll<T>(this ICollection<T> destination, params IEnumerable<T> source) {
        foreach (T item in source) {
            destination.Add(item);
        }
    }

    /// <summary>
    /// <para>Returns an existing value from a dictionary, or if it wasn't already present, add it.</para>
    /// <para>Not atomic. If this data needs to be accessed concurrently, use <see cref="ConcurrentDictionary{TKey,TValue}.GetOrAdd(TKey,TValue)"/> instead.</para>
    /// </summary>
    /// <typeparam name="TKey">type of <paramref name="dictionary"/> keys</typeparam>
    /// <typeparam name="TValue">type of <paramref name="dictionary"/> values</typeparam>
    /// <param name="dictionary">writable dictionary that may contain <paramref name="key"/></param>
    /// <param name="key">key to look up or insert into the <paramref name="dictionary"/></param>
    /// <param name="newValue">the new value to insert if <paramref name="key"/> is not found</param>
    /// <param name="added">after this method returns, this will be set to <c>true</c> if the new value was added to <paramref name="dictionary"/>, or <c>false</c> if a value with key <paramref name="key"/> was already present</param>
    /// <returns>the existing value with key <paramref name="key"/>, or the new value if it was not already present</returns>
    /// <exception cref="NotSupportedException"><paramref name="dictionary"/> is read-only</exception>
    public static TValue GetOrAdd<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, TValue newValue, out bool added) {
        added = !dictionary.TryGetValue(key, out TValue? oldValue);
        if (added) {
            dictionary.Add(key, newValue);
            return newValue;
        } else {
            return oldValue!;
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
    /// <exception cref="NotSupportedException"><paramref name="dictionary"/> is read-only</exception>
    public static TValue GetOrAdd<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, Func<TValue> newValueFactory, out bool added) {
        added = !dictionary.TryGetValue(key, out TValue? oldValue);
        if (added) {
            TValue newValue = newValueFactory();
            dictionary.Add(key, newValue);
            return newValue;
        } else {
            return oldValue!;
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
    /// <exception cref="NotSupportedException"><paramref name="dictionary"/> is read-only</exception>
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

    /// <summary>
    /// <para>Return a copy of an enumerable with all runs of consecutive duplicate items replaced with only one instance of that item.</para>
    /// <para>For example, given the input <c>[1, 1, 1, 2, 3, 1, 1]</c>, the distinct consecutive version would be <c>[1, 2, 3, 1]</c>.</para>
    /// </summary>
    /// <typeparam name="T">Type of items in <paramref name="source"/>.</typeparam>
    /// <param name="source">Input enumeration of items that may have runs of consecutive duplicate items.</param>
    /// <param name="comparer">Used to compare whether two items are equal, or the default <see cref="EqualityComparer{T}"/> if <c>null</c>.</param>
    /// <returns>An enumerable containing the items from <paramref name="source"/> in order, but with consecutive duplicate items excluded, or an empty enumerable if <paramref name="source"/> is empty.</returns>
    [Pure]
    public static IEnumerable<T> DistinctConsecutive<T>(this IEnumerable<T> source, IEqualityComparer<T>? comparer = null) {
        comparer ??= EqualityComparer<T>.Default;
        T?   previousItem = default;
        bool isFirstItem  = true;

        foreach (T item in source) {
            if (isFirstItem || !comparer.Equals(previousItem!, item)) {
                isFirstItem = false;
                yield return item;
            }

            previousItem = item;
        }
    }

    /// <summary>
    /// <para>Return a copy of an enumerable with all runs of consecutive duplicate items replaced with only one instance of that item.</para>
    /// <para>For example, given the input <c>[1, 1, 1, 2, 3, 1, 1]</c>, the distinct consecutive version would be <c>[1, 2, 3, 1]</c>.</para>
    /// </summary>
    /// <typeparam name="T">Type of items in <paramref name="source"/>.</typeparam>
    /// <typeparam name="TKey">Type of the derived value to compare uniqueness on.</typeparam>
    /// <param name="source">Input enumeration of items that may have runs of consecutive duplicate items.</param>
    /// <param name="keySelector">Compare uniqueness not on the items of <paramref name="source"/>, but on the output of this function that takes each item as input.</param>
    /// <param name="comparer">Used to compare whether two items are equal, or the default <see cref="EqualityComparer{T}"/> if <c>null</c>.</param>
    /// <returns>An enumerable containing the items from <paramref name="source"/> in order, but with consecutive duplicate items excluded, or an empty enumerable if <paramref name="source"/> is empty.</returns>
    [Pure]
    public static IEnumerable<T> DistinctConsecutive<T, TKey>(this IEnumerable<T> source, Func<T, TKey> keySelector, IEqualityComparer<TKey>? comparer = null) {
        comparer ??= EqualityComparer<TKey>.Default;
        TKey? previousKey = default;
        bool  isFirstItem = true;

        foreach (T item in source) {
            TKey key = keySelector(item);
            if (isFirstItem || !comparer.Equals(previousKey!, key)) {
                isFirstItem = false;
                yield return item;
            }

            previousKey = key;
        }
    }

    /// <summary>
    /// Convert an <see cref="IAsyncEnumerator{T}"/> to a list by fully enumerating all of its items.
    /// </summary>
    /// <param name="source">An <see cref="IAsyncEnumerator{T}"/> of finite size.</param>
    /// <typeparam name="T">Type of items in <paramref name="source"/>.</typeparam>
    /// <returns>A read-only list containing all of the items from <paramref name="source"/> in order, or an empty list if <paramref name="source"/> returned no items.</returns>
    [Pure]
    public static async Task<IReadOnlyList<T>> ToList<T>(this IAsyncEnumerator<T> source) {
        if (!await source.MoveNextAsync().ConfigureAwait(false)) {
            return [];
        }

        IList<T> result = new List<T> { source.Current };
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
    /// <returns>the first item in <paramref name="source"/>, or <c>null</c> if it is empty</returns>
    [Pure]
    public static TSource? FirstOrNull<TSource>(this IEnumerable<TSource> source) where TSource: struct {
        try {
            return source.First();
        } catch (InvalidOperationException) {
            return null;
        }
    }

    /// <summary>
    /// Like <see cref="Enumerable.FirstOrDefault{TSource}(System.Collections.Generic.IEnumerable{TSource},System.Func{TSource,bool})"/>, but returns <c>null</c> for value types instead of <c>default</c> when <paramref name="source"/> has no items that match <paramref name="predicate"/>, because nullable chaining and coalescing is easier than ambiguous defaults that can't be chained.
    /// </summary>
    /// <typeparam name="TSource">type of item in <paramref name="source"/></typeparam>
    /// <param name="source">sequence of items</param>
    /// <param name="predicate">function that should return <c>true</c> when the passed in item from <paramref name="source"/> should be returned, or <c>false</c> to not return it</param>
    /// <returns>the first item in <paramref name="source"/> that causes <paramref name="predicate"/> to return <c>true</c>, or <c>null</c> if it is empty or every item causes <paramref name="predicate"/> to return <c>false</c></returns>
    [Pure]
    public static TSource? FirstOrNull<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate) where TSource: struct {
        try {
            return source.First(predicate);
        } catch (InvalidOperationException) {
            return null;
        }
    }

    /// <summary>
    /// Like <see cref="Enumerable.LastOrDefault{TSource}(System.Collections.Generic.IEnumerable{TSource})"/>, but returns <c>null</c> for value types instead of <c>default</c> when <paramref name="source"/> is empty, because nullable chaining and coalescing is easier than ambiguous defaults that can't be chained.
    /// </summary>
    /// <typeparam name="TSource">type of item in <paramref name="source"/></typeparam>
    /// <param name="source">sequence of items</param>
    /// <returns>the last item in <paramref name="source"/>, or <c>null</c> if it is empty</returns>
    [Pure]
    public static TSource? LastOrNull<TSource>(this IEnumerable<TSource> source) where TSource: struct {
        try {
            return source.Last();
        } catch (InvalidOperationException) {
            return null;
        }
    }

    /// <summary>
    /// Like <see cref="Enumerable.LastOrDefault{TSource}(System.Collections.Generic.IEnumerable{TSource},System.Func{TSource,bool})"/>, but returns <c>null</c> for value types instead of <c>default</c> when <paramref name="source"/> has no items that match <paramref name="predicate"/>, because nullable chaining and coalescing is easier than ambiguous defaults that can't be chained.
    /// </summary>
    /// <typeparam name="TSource">type of item in <paramref name="source"/></typeparam>
    /// <param name="source">sequence of items</param>
    /// <param name="predicate">function that should return <c>true</c> when the passed in item from <paramref name="source"/> should be returned, or <c>false</c> to not return it</param>
    /// <returns>the last item in <paramref name="source"/> that causes <paramref name="predicate"/> to return <c>true</c>, or <c>null</c> if it is empty or every item causes <paramref name="predicate"/> to return <c>false</c></returns>
    [Pure]
    public static TSource? LastOrNull<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate) where TSource: struct {
        try {
            return source.Last(predicate);
        } catch (InvalidOperationException) {
            return null;
        }
    }

    /// <summary>
    /// Like <see cref="Enumerable.SingleOrDefault{TSource}(System.Collections.Generic.IEnumerable{TSource})"/>, but returns <c>null</c> for value types instead of <c>default</c> when <paramref name="source"/> is empty or contains two or more items, because nullable chaining and coalescing is easier than ambiguous defaults that can't be chained.
    /// </summary>
    /// <typeparam name="TSource">type of item in <paramref name="source"/></typeparam>
    /// <param name="source">sequence of items</param>
    /// <returns>the only item in <paramref name="source"/>, or <c>null</c> if it is empty or contains more than one item</returns>
    [Pure]
    public static TSource? SingleOrNull<TSource>(this IEnumerable<TSource> source) where TSource: struct {
        try {
            return source.Single();
        } catch (InvalidOperationException) {
            return null;
        }
    }

    /// <summary>
    /// Like <see cref="Enumerable.SingleOrDefault{TSource}(System.Collections.Generic.IEnumerable{TSource},System.Func{TSource,bool})"/>, but returns <c>null</c> for value types instead of <c>default</c> when <paramref name="source"/> has no items or multiple that match <paramref name="predicate"/>, because nullable chaining and coalescing is easier than ambiguous defaults that can't be chained.
    /// </summary>
    /// <typeparam name="TSource">type of item in <paramref name="source"/></typeparam>
    /// <param name="source">sequence of items</param>
    /// <param name="predicate">function that should return <c>true</c> when the passed in item from <paramref name="source"/> should be returned, or <c>false</c> to not return it</param>
    /// <returns>the only item in <paramref name="source"/> that causes <paramref name="predicate"/> to return <c>true</c>; or <c>null</c> if either <paramref name="source"/> is empty, every item causes <paramref name="predicate"/> to return <c>false</c>, or more than one item causes <paramref name="predicate"/> to return <c>true</c></returns>
    [Pure]
    public static TSource? SingleOrNull<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate) where TSource: struct {
        try {
            return source.Single(predicate);
        } catch (InvalidOperationException) {
            return null;
        }
    }

    /// <summary>
    /// Like <see cref="Enumerable.ElementAtOrDefault{TSource}(System.Collections.Generic.IEnumerable{TSource},int)"/>, but returns <c>null</c> for value types instead of <c>default</c> when <paramref name="source"/> is too short to have the requested <paramref name="index"/>, because nullable chaining and coalescing is easier than ambiguous defaults that can't be chained.
    /// </summary>
    /// <typeparam name="TSource">Type of item in <paramref name="source"/></typeparam>
    /// <param name="source">Sequence of items</param>
    /// <param name="index">The 0-indexed position of the item in <paramref name="source"/> to return</param>
    /// <returns>The item at position <paramref name="index"/> in <paramref name="source"/>, or <c>null</c> if <paramref name="index"/> is an invalid index in <paramref name="source"/>, either because the length of <paramref name="source"/> is less than or equal to <paramref name="index"/>, or because <paramref name="index"/> is negative</returns>
    [Pure]
    public static TSource? ElementAtOrNull<TSource>(this IEnumerable<TSource> source, int index) where TSource: struct {
        try {
            return source.ElementAt(index);
        } catch (ArgumentOutOfRangeException) {
            return null;
        }
    }

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
    /// <inheritdoc cref="ElementAtOrNull{TSource}(System.Collections.Generic.IEnumerable{TSource},int)" />
    [Pure]
    public static TSource? ElementAtOrNull<TSource>(this IEnumerable<TSource> source, Index index) where TSource: struct {
        try {
            return source.ElementAt(index);
        } catch (ArgumentOutOfRangeException) {
            return null;
        }
    }
#endif

    /// <summary>
    /// Split an enumerable into the first element and the rest of the elements.
    /// </summary>
    /// <typeparam name="T">Type of elements in the enumerable</typeparam>
    /// <param name="source">Enumerable with zero or more elements.</param>
    /// <returns>A tuple with two items. The first item, <c>head</c>, is the first element of <paramref name="source"/>, or <c>null</c> if <paramref name="source"/> is empty. The second item, <c>tail</c>, is an enumerable of the remaining elements of <paramref name="source"/>, or an empty enumerable if <paramref name="source"/> has fewer than 2 elements.</returns>
    [Pure]
    public static (T? head, IEnumerable<T> tail) HeadAndTail<T>(this IEnumerable<T> source) where T: class? {
        using IEnumerator<T> enumerator = source.GetEnumerator();
        return (head: enumerator.MoveNext() ? enumerator.Current : null, tail: new Enumerable<T>(enumerator));
    }

    /// <summary>
    /// Split an enumerable into the first element and the rest of the elements.
    /// </summary>
    /// <typeparam name="T">Type of elements in the enumerable</typeparam>
    /// <param name="source">Enumerable with zero or more elements.</param>
    /// <returns>A tuple with two items. The first item, <c>head</c>, is the first element of <paramref name="source"/>, or <c>null</c> if <paramref name="source"/> is empty. The second item, <c>tail</c>, is an enumerable of the remaining elements of <paramref name="source"/>, or an empty enumerable if <paramref name="source"/> has fewer than 2 elements.</returns>
    [Pure]
    public static (T? head, IEnumerable<T> tail) HeadAndTailValueType<T>(this IEnumerable<T> source) where T: struct {
        using IEnumerator<T> enumerator = source.GetEnumerator();
        return (head: enumerator.MoveNext() ? enumerator.Current : null, tail: new Enumerable<T>(enumerator));
    }

    /// <summary>
    /// Split an enumerable into the first element and the rest of the elements.
    /// </summary>
    /// <typeparam name="T">Type of elements in the enumerable</typeparam>
    /// <param name="source">Enumerable with zero or more elements.</param>
    /// <returns>A tuple with two items. The first item, <c>head</c>, is the first element of <paramref name="source"/>, or <c>null</c> if <paramref name="source"/> is empty. The second item, <c>tail</c>, is an enumerable of the remaining elements of <paramref name="source"/>, or an empty enumerable if <paramref name="source"/> has fewer than 2 elements.</returns>
    [Pure]
    public static (T? head, IEnumerable<T?> tail) HeadAndTailValueType<T>(this IEnumerable<T?> source) where T: struct {
        using IEnumerator<T?> enumerator = source.GetEnumerator();
        return (head: enumerator.MoveNext() ? enumerator.Current : null, tail: new Enumerable<T?>(enumerator));
    }

    private class Enumerable<T>(IEnumerator<T> enumerator): IEnumerable<T> {

        public IEnumerator<T> GetEnumerator() => enumerator;

        IEnumerator IEnumerable.GetEnumerator() => enumerator;

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

    [Pure]
    public static IReadOnlyList<T> AsReadOnly<T>(this IList<T> writableList) =>
#if NET8_0_OR_GREATER
        CollectionExtensions.AsReadOnly(writableList);
#else
        new ReadOnlyCollection<T>(writableList);
#endif

}