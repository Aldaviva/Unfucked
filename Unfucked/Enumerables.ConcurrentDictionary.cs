using System.Collections.Concurrent;

namespace Unfucked;

public static partial class Enumerables {

    private const int DEFAULT_CONCURRENT_DICTIONARY_CAPACITY = 31;

    #region Factory Methods

    // Normal values like int and object
    /// <summary>
    /// Create a <see cref="ConcurrentDictionary{TKey,TValue}"/> whose values can be mutated atomically using <see cref="Interlocked"/>, especially using the extension methods like <see cref="Swap{TKey,TValue}"/>.
    /// </summary>
    /// <typeparam name="TKey">Type of the dictionary keys.</typeparam>
    /// <typeparam name="TValue">
    /// <para>Type of the dictionary values. Can be <see cref="int"/>, <see cref="long"/>, <see cref="IntPtr"/>, <see cref="float"/>, <see cref="double"/>, <see cref="bool"/>¹, <see cref="Enum"/>², or a class.</para>
    /// <para>On .NET ≥ 8, can also be <see cref="uint"/>, <see cref="ulong"/>, <see cref="UIntPtr"/>.</para>
    /// <para>¹ For <see cref="bool"/> values, use <see cref="CreateConcurrentBooleanDictionary{TKey}"/>.</para>
    /// <para>² For <see cref="Enum"/> values, use <see cref="CreateConcurrentEnumDictionary{TKey,TEnumValue,TIntegralValue}"/>.</para></typeparam>
    /// <param name="initialElements">Optional key-value pairs to initially add to the new dictionary.</param>
    /// <param name="capacity">Optional expected maximum number of keys that this dictionary will contain, useful to prevent resizing as key-value pairs are added. Will be ignored if <paramref name="initialElements"/> is not <c>null</c>.</param>
    /// <param name="keyComparer">Optional <see cref="IEqualityComparer{T}"/> to determine if two keys are the same.</param>
    /// <param name="concurrency">Optional maximum number of threads that are expected to access this dictionary concurrently, defaults to the total CPU thread count.</param>
    /// <returns>A <see cref="ConcurrentDictionary{TKey,TValue}"/> whose values can be mutated atomically using <see cref="Swap{TKey,TValue}"/>.</returns>
    [Pure]
    public static ConcurrentDictionary<TKey, ValueHolder<TValue>> CreateConcurrentDictionary<TKey, TValue>(IEnumerable<KeyValuePair<TKey, TValue>>? initialElements = null, int concurrency = -1,
                                                                                                           int? capacity = null, IEqualityComparer<TKey>? keyComparer = null) where TKey: notnull {
#if NETSTANDARD
        keyComparer ??= EqualityComparer<TKey>.Default;
#endif
#if !NET8_0_OR_GREATER
        if (concurrency == -1) {
            concurrency = Environment.ProcessorCount;
        }
#endif
        return initialElements != null
            ? new ConcurrentDictionary<TKey, ValueHolder<TValue>>(concurrency,
                initialElements.Select(pair => new KeyValuePair<TKey, ValueHolder<TValue>>(pair.Key, new ValueHolder<TValue>(pair.Value))), keyComparer)
            : new ConcurrentDictionary<TKey, ValueHolder<TValue>>(concurrency, capacity ?? DEFAULT_CONCURRENT_DICTIONARY_CAPACITY, keyComparer);
    }

    // Boolean values
    /// <summary>
    /// <para>Create a <see cref="ConcurrentDictionary{TKey,TValue}"/> whose <see cref="bool"/> values can be mutated atomically using <see cref="Interlocked"/>, especially using the extension methods like <see cref="Swap{TKey}(System.Collections.Concurrent.ConcurrentDictionary{TKey,Unfucked.BooleanValueHolder},TKey,bool)"/>.</para>
    /// <para>If you get an ambiguous invocation error, try calling <see cref="CreateConcurrentBooleanDictionary{TKey}"/>.</para>
    /// </summary>
    /// <typeparam name="TKey">Type of the dictionary keys.</typeparam>
    /// <typeparam name="TValue"><see cref="bool"/> dictionary values.</typeparam>
    /// <param name="initialElements">Optional key-value pairs to initially add to the new dictionary.</param>
    /// <param name="capacity">Optional expected maximum number of keys that this dictionary will contain, useful to prevent resizing as key-value pairs are added. Will be ignored if <paramref name="initialElements"/> is not <c>null</c>.</param>
    /// <param name="keyComparer">Optional <see cref="IEqualityComparer{T}"/> to determine if two keys are the same.</param>
    /// <param name="concurrency">Optional maximum number of threads that are expected to access this dictionary concurrently, defaults to the total CPU thread count.</param>
    /// <returns>A <see cref="ConcurrentDictionary{TKey,TValue}"/> whose values can be mutated atomically using <see cref="Swap{TKey,TValue}"/>.</returns>
    [Pure]
    public static ConcurrentDictionary<TKey, BooleanValueHolder> CreateConcurrentDictionary<TKey, TValue>(IEnumerable<KeyValuePair<TKey, bool>>? initialElements = null, int concurrency = -1,
                                                                                                          int? capacity = null, IEqualityComparer<TKey>? keyComparer = null)
        where TKey: notnull where TValue: IEquatable<bool> {
#if NETSTANDARD
        keyComparer ??= EqualityComparer<TKey>.Default;
#endif
#if !NET8_0_OR_GREATER
        if (concurrency == -1) {
            concurrency = Environment.ProcessorCount;
        }
#endif
        return initialElements != null
            ? new ConcurrentDictionary<TKey, BooleanValueHolder>(concurrency,
                initialElements.Select(pair => new KeyValuePair<TKey, BooleanValueHolder>(pair.Key, new BooleanValueHolder(pair.Value))), keyComparer)
            : new ConcurrentDictionary<TKey, BooleanValueHolder>(concurrency, capacity ?? DEFAULT_CONCURRENT_DICTIONARY_CAPACITY, keyComparer);
    }

    // Boolean values helper for ambiguous signature error
    /// <summary>
    /// <para>Create a <see cref="ConcurrentDictionary{TKey,TValue}"/> whose <see cref="bool"/> values can be mutated atomically using <see cref="Interlocked"/>, especially using the extension methods like <see cref="Swap{TKey}(System.Collections.Concurrent.ConcurrentDictionary{TKey,Unfucked.BooleanValueHolder},TKey,bool)"/>.</para>
    /// </summary>
    /// <typeparam name="TKey">Type of the dictionary keys.</typeparam>
    /// <param name="initialElements">Optional key-value pairs to initially add to the new dictionary.</param>
    /// <param name="capacity">Optional expected maximum number of keys that this dictionary will contain, useful to prevent resizing as key-value pairs are added. Will be ignored if <paramref name="initialElements"/> is not <c>null</c>.</param>
    /// <param name="keyComparer">Optional <see cref="IEqualityComparer{T}"/> to determine if two keys are the same.</param>
    /// <param name="concurrency">Optional maximum number of threads that are expected to access this dictionary concurrently, defaults to the total CPU thread count.</param>
    /// <returns>A <see cref="ConcurrentDictionary{TKey,TValue}"/> whose values can be mutated atomically using <see cref="Swap{TKey,TValue}"/>.</returns>
    [Pure]
    public static ConcurrentDictionary<TKey, BooleanValueHolder> CreateConcurrentBooleanDictionary<TKey>(IEnumerable<KeyValuePair<TKey, bool>>? initialElements = null, int concurrency = -1,
                                                                                                         int? capacity = null, IEqualityComparer<TKey>? keyComparer = null) where TKey: notnull {
        return CreateConcurrentDictionary<TKey, bool>(initialElements, concurrency, capacity, keyComparer);
    }

    // Enum values
    /// <summary>
    /// Create a <see cref="ConcurrentDictionary{TKey,TValue}"/> whose <see cref="Enum"/> values can be mutated atomically using <see cref="Swap{TKey,TIntegralValue,TValue}"/> or <see cref="Interlocked"/>.
    /// </summary>
    /// <typeparam name="TKey">Type of the dictionary keys.</typeparam>
    /// <typeparam name="TEnumValue">Type of the dictionary <see cref="Enum"/> values, like <c>MyEnum</c> (not the underlying integral type like <see cref="int"/>).</typeparam>
    /// <typeparam name="TIntegralValue">The underlying integral type of the <typeparamref name="TEnumValue"/>'s <see cref="Enum"/>, such as <see cref="int"/> or <see cref="long"/>.</typeparam>
    /// <param name="initialElements">Optional key-value pairs to initially add to the new dictionary.</param>
    /// <param name="capacity">Optional expected maximum number of keys that this dictionary will contain, useful to prevent resizing as key-value pairs are added. Will be ignored if <paramref name="initialElements"/> is not <c>null</c>.</param>
    /// <param name="keyComparer">Optional <see cref="IEqualityComparer{T}"/> to determine if two keys are the same.</param>
    /// <param name="concurrency">Optional maximum number of threads that are expected to access this dictionary concurrently, defaults to the total CPU thread count.</param>
    /// <returns>A <see cref="ConcurrentDictionary{TKey,TValue}"/> whose values can be mutated atomically using <see cref="Swap{TKey,TValue}"/>.</returns>
    /// <exception cref="InvalidCastException"><typeparamref name="TIntegralValue"/> does not match the underlying type of <typeparamref name="TEnumValue"/>.</exception>
    [Pure]
    public static ConcurrentDictionary<TKey, EnumValueHolder<TEnumValue, TIntegralValue>> CreateConcurrentEnumDictionary<TKey, TEnumValue, TIntegralValue>(
        IEnumerable<KeyValuePair<TKey, TEnumValue>>? initialElements = null,
        int concurrency = -1,
        int? capacity = null,
        IEqualityComparer<TKey>? keyComparer = null)
        where TKey: notnull where TIntegralValue: struct where TEnumValue: struct, Enum {
        if (Enum.GetUnderlyingType(typeof(TEnumValue)) is var expectedUnderlyingType && typeof(TIntegralValue) is var actualUnderlyingType && expectedUnderlyingType != actualUnderlyingType) {
            throw new InvalidCastException(
                $"The {nameof(TIntegralValue)} generic type parameter must be {expectedUnderlyingType.Name} based on the underlying type of {typeof(TEnumValue).Name}, but was {actualUnderlyingType.Name}");
        }
#if NETSTANDARD
        keyComparer ??= EqualityComparer<TKey>.Default;
#endif
#if !NET8_0_OR_GREATER
        if (concurrency == -1) {
            concurrency = Environment.ProcessorCount;
        }
#endif
        return initialElements != null
            ? new ConcurrentDictionary<TKey, EnumValueHolder<TEnumValue, TIntegralValue>>(concurrency,
                initialElements.Select(pair => new KeyValuePair<TKey, EnumValueHolder<TEnumValue, TIntegralValue>>(pair.Key, new EnumValueHolder<TEnumValue, TIntegralValue>(pair.Value))), keyComparer)
            : new ConcurrentDictionary<TKey, EnumValueHolder<TEnumValue, TIntegralValue>>(concurrency, capacity ?? DEFAULT_CONCURRENT_DICTIONARY_CAPACITY, keyComparer);
    }

    #endregion

    #region Exchange enums and booleans

    /// <summary>
    /// Atomically swap a new <see cref="Enum"/> value into a dictionary with a given key, getting the old value. If the dictionary does not already contain this key, it will be atomically inserted.
    /// </summary>
    /// <typeparam name="TKey">Type of the dictionary keys.</typeparam>
    /// <typeparam name="TValue"><see cref="Enum"/> type of the dictionary values such as <c>MyEnum</c>, rather than the underlying integral type such as <see cref="int"/>. These will be wrapped in a <see cref="EnumValueHolder{T,T}"/> to allow for mutations.</typeparam>
    /// <typeparam name="TIntegralValue">The underlying integral type of the <typeparamref name="TValue"/>'s <see cref="Enum"/>, such as <see cref="int"/> or <see cref="long"/>.</typeparam>
    /// <param name="dictionary">A <see cref="ConcurrentDictionary{TKey,TValue}"/> created by <see cref="CreateConcurrentEnumDictionary{TKey,TEnumValue,TIntegralValue}"/>.</param>
    /// <param name="key">The key whose value you want to replace.</param>
    /// <param name="newValue">The new value that you want to swap into the dictionary.</param>
    /// <returns>The old value of the <paramref name="key"/> that was swapped out of <paramref name="dictionary"/>, replaced by <paramref name="newValue"/>, or <c>null</c> if <paramref name="dictionary"/> did not already contain this <paramref name="key"/>.</returns>
    public static TValue? Swap<TKey, TIntegralValue, TValue>(this ConcurrentDictionary<TKey, EnumValueHolder<TValue, TIntegralValue>> dictionary, TKey key, TValue newValue)
        where TKey: notnull where TValue: struct, Enum where TIntegralValue: struct {
        var     newHolder = new EnumValueHolder<TValue, TIntegralValue>(newValue);
        TValue? oldValue;
        bool    inserted;
        do {
            EnumValueHolder<TValue, TIntegralValue> existingHolder = dictionary.GetOrAdd(key, newHolder);
            inserted = ReferenceEquals(newHolder, existingHolder);
            oldValue = inserted ? null : existingHolder.Exchange(newValue);
        } while (!inserted && !dictionary.ContainsKey(key));
        return oldValue;
    }

    /// <summary>
    /// Atomically compare and swap a new <see cref="Enum"/> value into a dictionary with a given key, only if the existing value equals a given value, and returning the old value. If the dictionary does not already contain this key, it will not be inserted, and <c>null</c> will be returned.
    /// </summary>
    /// <typeparam name="TKey">Type of the dictionary keys.</typeparam>
    /// <typeparam name="TValue"><see cref="Enum"/> type of the dictionary values such as <c>MyEnum</c>, rather than the underlying integral type such as <see cref="int"/>. These will be wrapped in a <see cref="EnumValueHolder{T,T}"/> to allow for mutations.</typeparam>
    /// <typeparam name="TIntegralValue">The underlying integral type of the <typeparamref name="TValue"/>'s <see cref="Enum"/>, such as <see cref="int"/> or <see cref="long"/>.</typeparam>
    /// <param name="dictionary">A <see cref="ConcurrentDictionary{TKey,TValue}"/> created by  <see cref="CreateConcurrentEnumDictionary{TKey,TEnumValue,TIntegralValue}"/>.</param>
    /// <param name="key">The key whose value you want to conditionally replace.</param>
    /// <param name="oldValue">The old value that must be equal to the existing value in the dictionary in order to swap it.</param>
    /// <param name="newValue">The new value that you want to swap into the dictionary.</param>
    /// <returns>The old value <paramref name="oldValue"/>, which is the old value of <paramref name="key"/> in <paramref name="dictionary"/>, or <c>null</c> if <paramref name="dictionary"/> did not already contain the key <paramref name="key"/> so no insertion or swapping occurred.</returns>
    public static TValue? CompareAndSwap<TKey, TIntegralValue, TValue>(this ConcurrentDictionary<TKey, EnumValueHolder<TValue, TIntegralValue>> dictionary, TKey key, TValue oldValue, TValue newValue)
        where TKey: notnull where TValue: struct, Enum where TIntegralValue: struct =>
        dictionary.TryGetValue(key, out EnumValueHolder<TValue, TIntegralValue>? existingHolder) ? existingHolder.CompareExchange(oldValue, newValue) : null;

    /// <summary>
    /// Atomically swap a new boolean value into a dictionary with a given key, getting the old value. If the dictionary does not already contain this key, it will be atomically inserted.
    /// </summary>
    /// <typeparam name="TKey">Type of the dictionary keys.</typeparam>
    /// <param name="dictionary">A <see cref="ConcurrentDictionary{TKey,TValue}"/> created by <see cref="CreateConcurrentBooleanDictionary{TKey}"/>.</param>
    /// <param name="key">The key whose value you want to replace.</param>
    /// <param name="newValue">The new value that you want to swap into the dictionary.</param>
    /// <returns>The old value of the <paramref name="key"/> that was swapped out of <paramref name="dictionary"/>, replaced by <paramref name="newValue"/>, or <c>null</c> if <paramref name="dictionary"/> did not already contain this <paramref name="key"/>.</returns>
    public static bool? Swap<TKey>(this ConcurrentDictionary<TKey, BooleanValueHolder> dictionary, TKey key, bool newValue) where TKey: notnull {
        BooleanValueHolder newHolder = new(newValue);
        bool?              oldValue;
        bool               inserted;
        do {
            BooleanValueHolder existingHolder = dictionary.GetOrAdd(key, newHolder);
            inserted = ReferenceEquals(newHolder, existingHolder);
            oldValue = inserted ? null : existingHolder.Exchange(newValue);
        } while (!inserted && !dictionary.ContainsKey(key));
        return oldValue;
    }

    /// <summary>
    /// Atomically compare and swap a new boolean value into a dictionary with a given key, only if the existing value equals a given value, and returning the old value. If the dictionary does not already contain this key, it will not be inserted, and <c>null</c> will be returned.
    /// </summary>
    /// <typeparam name="TKey">Type of the dictionary keys.</typeparam>
    /// <param name="dictionary">A <see cref="ConcurrentDictionary{TKey,TValue}"/> created by <see cref="CreateConcurrentBooleanDictionary{TKey}"/>.</param>
    /// <param name="key">The key whose value you want to conditionally replace.</param>
    /// <param name="oldValue">The old value that must be equal to the existing value in the dictionary in order to swap it.</param>
    /// <param name="newValue">The new value that you want to swap into the dictionary.</param>
    /// <returns>The old value <paramref name="oldValue"/>, which is the old value of <paramref name="key"/> in <paramref name="dictionary"/>, or <c>null</c> if <paramref name="dictionary"/> did not already contain the key <paramref name="key"/> so no insertion or swapping occurred.</returns>
    public static bool? CompareAndSwap<TKey>(this ConcurrentDictionary<TKey, BooleanValueHolder> dictionary, TKey key, bool oldValue, bool newValue) where TKey: notnull =>
        dictionary.TryGetValue(key, out BooleanValueHolder? existingHolder) ? existingHolder.CompareExchange(oldValue, newValue) : null;

    #endregion

    #region Exchange numbers and objects

    /// <summary>
    /// Atomically swap a new value into a dictionary with a given key, getting the old value. If the dictionary does not already contain this key, it will be atomically inserted.
    /// </summary>
    /// <typeparam name="TKey">Type of the dictionary keys.</typeparam>
    /// <param name="dictionary">A <see cref="ConcurrentDictionary{TKey,TValue}"/>, possibly created by <see cref="CreateConcurrentDictionary{TKey,TValue}(System.Collections.Generic.IEnumerable{System.Collections.Generic.KeyValuePair{TKey,TValue}}?,int,int?,System.Collections.Generic.IEqualityComparer{TKey}?)"/>.</param>
    /// <param name="key">The key whose value you want to replace.</param>
    /// <param name="newValue">The new value that you want to swap into the dictionary.</param>
    /// <returns>The old value of the <paramref name="key"/> that was swapped out of <paramref name="dictionary"/>, replaced by <paramref name="newValue"/>, or <c>null</c> if <paramref name="dictionary"/> did not already contain this <paramref name="key"/>.</returns>
    public static long? Swap<TKey>(this ConcurrentDictionary<TKey, ValueHolder<long>> dictionary, TKey key, long newValue) where TKey: notnull {
        var   newHolder = new ValueHolder<long>(newValue);
        long? oldValue;
        bool  inserted;
        do {
            ValueHolder<long> existingHolder = dictionary.GetOrAdd(key, newHolder);
            inserted = ReferenceEquals(newHolder, existingHolder);
            oldValue = inserted ? null : Interlocked.Exchange(ref existingHolder.Value, newValue);
        } while (!inserted && !dictionary.ContainsKey(key));
        return oldValue;
    }

    /// <summary>
    /// Atomically compare and swap a new value into a dictionary with a given key, only if the existing value equals a given value, and returning the old value. If the dictionary does not already contain this key, it will not be inserted, and <c>null</c> will be returned.
    /// </summary>
    /// <typeparam name="TKey">Type of the dictionary keys.</typeparam>
    /// <param name="dictionary">A <see cref="ConcurrentDictionary{TKey,TValue}"/>, possibly created by <see cref="CreateConcurrentDictionary{TKey,TValue}(System.Collections.Generic.IEnumerable{System.Collections.Generic.KeyValuePair{TKey,TValue}}?,int,int?,System.Collections.Generic.IEqualityComparer{TKey}?)"/>.</param>
    /// <param name="key">The key whose value you want to conditionally replace.</param>
    /// <param name="oldValue">The old value that must be equal to the existing value in the dictionary in order to swap it.</param>
    /// <param name="newValue">The new value that you want to swap into the dictionary.</param>
    /// <returns>The old value <paramref name="oldValue"/>, which is the old value of <paramref name="key"/> in <paramref name="dictionary"/>, or <c>null</c> if <paramref name="dictionary"/> did not already contain the key <paramref name="key"/> so no insertion or swapping occurred.</returns>
    public static long? CompareAndSwap<TKey>(this ConcurrentDictionary<TKey, ValueHolder<long>> dictionary, TKey key, long oldValue, long newValue) where TKey: notnull =>
        dictionary.TryGetValue(key, out ValueHolder<long>? existingHolder) ? Interlocked.CompareExchange(ref existingHolder.Value, newValue, oldValue) : null;

    /// <summary>
    /// Atomically swap a new value into a dictionary with a given key, getting the old value. If the dictionary does not already contain this key, it will be atomically inserted.
    /// </summary>
    /// <typeparam name="TKey">Type of the dictionary keys.</typeparam>
    /// <param name="dictionary">A <see cref="ConcurrentDictionary{TKey,TValue}"/>, possibly created by <see cref="CreateConcurrentDictionary{TKey,TValue}(System.Collections.Generic.IEnumerable{System.Collections.Generic.KeyValuePair{TKey,TValue}}?,int,int?,System.Collections.Generic.IEqualityComparer{TKey}?)"/>.</param>
    /// <param name="key">The key whose value you want to replace.</param>
    /// <param name="newValue">The new value that you want to swap into the dictionary.</param>
    /// <returns>The old value of the <paramref name="key"/> that was swapped out of <paramref name="dictionary"/>, replaced by <paramref name="newValue"/>, or <c>null</c> if <paramref name="dictionary"/> did not already contain this <paramref name="key"/>.</returns>
    public static int? Swap<TKey>(this ConcurrentDictionary<TKey, ValueHolder<int>> dictionary, TKey key, int newValue) where TKey: notnull {
        var  newHolder = new ValueHolder<int>(newValue);
        int? oldValue;
        bool inserted;
        do {
            ValueHolder<int> existingHolder = dictionary.GetOrAdd(key, newHolder);
            inserted = ReferenceEquals(newHolder, existingHolder);
            oldValue = inserted ? null : Interlocked.Exchange(ref existingHolder.Value, newValue);
        } while (!inserted && !dictionary.ContainsKey(key));
        return oldValue;
    }

    /// <inheritdoc cref="CompareAndSwap{TKey}(System.Collections.Concurrent.ConcurrentDictionary{TKey,Unfucked.ValueHolder{long}},TKey,long,long)" />
    public static int? CompareAndSwap<TKey>(this ConcurrentDictionary<TKey, ValueHolder<int>> dictionary, TKey key, int oldValue, int newValue) where TKey: notnull =>
        dictionary.TryGetValue(key, out ValueHolder<int>? existingHolder) ? Interlocked.CompareExchange(ref existingHolder.Value, newValue, oldValue) : null;

    /// <summary>
    /// Atomically swap a new value into a dictionary with a given key, getting the old value. If the dictionary does not already contain this key, it will be atomically inserted.
    /// </summary>
    /// <typeparam name="TKey">Type of the dictionary keys.</typeparam>
    /// <param name="dictionary">A <see cref="ConcurrentDictionary{TKey,TValue}"/>, possibly created by <see cref="CreateConcurrentDictionary{TKey,TValue}(System.Collections.Generic.IEnumerable{System.Collections.Generic.KeyValuePair{TKey,TValue}}?,int,int?,System.Collections.Generic.IEqualityComparer{TKey}?)"/>.</param>
    /// <param name="key">The key whose value you want to replace.</param>
    /// <param name="newValue">The new value that you want to swap into the dictionary.</param>
    /// <returns>The old value of the <paramref name="key"/> that was swapped out of <paramref name="dictionary"/>, replaced by <paramref name="newValue"/>, or <c>null</c> if <paramref name="dictionary"/> did not already contain this <paramref name="key"/>.</returns>
    public static double? Swap<TKey>(this ConcurrentDictionary<TKey, ValueHolder<double>> dictionary, TKey key, double newValue) where TKey: notnull {
        var     newHolder = new ValueHolder<double>(newValue);
        double? oldValue;
        bool    inserted;
        do {
            ValueHolder<double> existingHolder = dictionary.GetOrAdd(key, newHolder);
            inserted = ReferenceEquals(newHolder, existingHolder);
            oldValue = inserted ? null : Interlocked.Exchange(ref existingHolder.Value, newValue);
        } while (!inserted && !dictionary.ContainsKey(key));
        return oldValue;
    }

    /// <inheritdoc cref="CompareAndSwap{TKey}(System.Collections.Concurrent.ConcurrentDictionary{TKey,Unfucked.ValueHolder{long}},TKey,long,long)" />
    public static double? CompareAndSwap<TKey>(this ConcurrentDictionary<TKey, ValueHolder<double>> dictionary, TKey key, double oldValue, double newValue) where TKey: notnull =>
        dictionary.TryGetValue(key, out ValueHolder<double>? existingHolder) ? Interlocked.CompareExchange(ref existingHolder.Value, newValue, oldValue) : null;

#if NET6_0_OR_GREATER
    /// <summary>
    /// Atomically swap a new value into a dictionary with a given key, getting the old value. If the dictionary does not already contain this key, it will be atomically inserted.
    /// </summary>
    /// <typeparam name="TKey">Type of the dictionary keys.</typeparam>
    /// <param name="dictionary">A <see cref="ConcurrentDictionary{TKey,TValue}"/>, possibly created by <see cref="CreateConcurrentDictionary{TKey,TValue}(System.Collections.Generic.IEnumerable{System.Collections.Generic.KeyValuePair{TKey,TValue}}?,int,int?,System.Collections.Generic.IEqualityComparer{TKey}?)"/>.</param>
    /// <param name="key">The key whose value you want to replace.</param>
    /// <param name="newValue">The new value that you want to swap into the dictionary.</param>
    /// <returns>The old value of the <paramref name="key"/> that was swapped out of <paramref name="dictionary"/>, replaced by <paramref name="newValue"/>, or <c>null</c> if <paramref name="dictionary"/> did not already contain this <paramref name="key"/>.</returns>
    public static uint? Swap<TKey>(this ConcurrentDictionary<TKey, ValueHolder<uint>> dictionary, TKey key, uint newValue) where TKey: notnull {
        var   newHolder = new ValueHolder<uint>(newValue);
        uint? oldValue;
        bool  inserted;
        do {
            ValueHolder<uint> existingHolder = dictionary.GetOrAdd(key, newHolder);
            inserted = ReferenceEquals(newHolder, existingHolder);
            oldValue = inserted ? null : Interlocked.Exchange(ref existingHolder.Value, newValue);
        } while (!inserted && !dictionary.ContainsKey(key));
        return oldValue;
    }

    /// <inheritdoc cref="CompareAndSwap{TKey}(System.Collections.Concurrent.ConcurrentDictionary{TKey,Unfucked.ValueHolder{long}},TKey,long,long)" />
    public static uint? CompareAndSwap<TKey>(this ConcurrentDictionary<TKey, ValueHolder<uint>> dictionary, TKey key, uint oldValue, uint newValue) where TKey: notnull =>
        dictionary.TryGetValue(key, out ValueHolder<uint>? existingHolder) ? Interlocked.CompareExchange(ref existingHolder.Value, newValue, oldValue) : null;

    /// <summary>
    /// Atomically swap a new value into a dictionary with a given key, getting the old value. If the dictionary does not already contain this key, it will be atomically inserted.
    /// </summary>
    /// <typeparam name="TKey">Type of the dictionary keys.</typeparam>
    /// <param name="dictionary">A <see cref="ConcurrentDictionary{TKey,TValue}"/>, possibly created by <see cref="CreateConcurrentDictionary{TKey,TValue}(System.Collections.Generic.IEnumerable{System.Collections.Generic.KeyValuePair{TKey,TValue}}?,int,int?,System.Collections.Generic.IEqualityComparer{TKey}?)"/>.</param>
    /// <param name="key">The key whose value you want to replace.</param>
    /// <param name="newValue">The new value that you want to swap into the dictionary.</param>
    /// <returns>The old value of the <paramref name="key"/> that was swapped out of <paramref name="dictionary"/>, replaced by <paramref name="newValue"/>, or <c>null</c> if <paramref name="dictionary"/> did not already contain this <paramref name="key"/>.</returns>
    public static ulong? Swap<TKey>(this ConcurrentDictionary<TKey, ValueHolder<ulong>> dictionary, TKey key, ulong newValue) where TKey: notnull {
        var    newHolder = new ValueHolder<ulong>(newValue);
        ulong? oldValue;
        bool   inserted;
        do {
            ValueHolder<ulong> existingHolder = dictionary.GetOrAdd(key, newHolder);
            inserted = ReferenceEquals(newHolder, existingHolder);
            oldValue = inserted ? null : Interlocked.Exchange(ref existingHolder.Value, newValue);
        } while (!inserted && !dictionary.ContainsKey(key));
        return oldValue;
    }

    /// <inheritdoc cref="CompareAndSwap{TKey}(System.Collections.Concurrent.ConcurrentDictionary{TKey,Unfucked.ValueHolder{long}},TKey,long,long)" />
    public static ulong? CompareAndSwap<TKey>(this ConcurrentDictionary<TKey, ValueHolder<ulong>> dictionary, TKey key, ulong oldValue, ulong newValue) where TKey: notnull =>
        dictionary.TryGetValue(key, out ValueHolder<ulong>? existingHolder) ? Interlocked.CompareExchange(ref existingHolder.Value, newValue, oldValue) : null;
#endif

    /// <summary>
    /// Atomically swap a new value into a dictionary with a given key, getting the old value. If the dictionary does not already contain this key, it will be atomically inserted.
    /// </summary>
    /// <typeparam name="TKey">Type of the dictionary keys.</typeparam>
    /// <param name="dictionary">A <see cref="ConcurrentDictionary{TKey,TValue}"/>, possibly created by <see cref="CreateConcurrentDictionary{TKey,TValue}(System.Collections.Generic.IEnumerable{System.Collections.Generic.KeyValuePair{TKey,TValue}}?,int,int?,System.Collections.Generic.IEqualityComparer{TKey}?)"/>.</param>
    /// <param name="key">The key whose value you want to replace.</param>
    /// <param name="newValue">The new value that you want to swap into the dictionary.</param>
    /// <returns>The old value of the <paramref name="key"/> that was swapped out of <paramref name="dictionary"/>, replaced by <paramref name="newValue"/>, or <c>null</c> if <paramref name="dictionary"/> did not already contain this <paramref name="key"/>.</returns>
    public static float? Swap<TKey>(this ConcurrentDictionary<TKey, ValueHolder<float>> dictionary, TKey key, float newValue) where TKey: notnull {
        var    newHolder = new ValueHolder<float>(newValue);
        float? oldValue;
        bool   inserted;
        do {
            ValueHolder<float> existingHolder = dictionary.GetOrAdd(key, newHolder);
            inserted = ReferenceEquals(newHolder, existingHolder);
            oldValue = inserted ? null : Interlocked.Exchange(ref existingHolder.Value, newValue);
        } while (!inserted && !dictionary.ContainsKey(key));
        return oldValue;
    }

    /// <inheritdoc cref="CompareAndSwap{TKey}(System.Collections.Concurrent.ConcurrentDictionary{TKey,Unfucked.ValueHolder{long}},TKey,long,long)" />
    public static float? CompareAndSwap<TKey>(this ConcurrentDictionary<TKey, ValueHolder<float>> dictionary, TKey key, float oldValue, float newValue) where TKey: notnull =>
        dictionary.TryGetValue(key, out ValueHolder<float>? existingHolder) ? Interlocked.CompareExchange(ref existingHolder.Value, newValue, oldValue) : null;

    /// <summary>
    /// Atomically swap a new value into a dictionary with a given key, getting the old value. If the dictionary does not already contain this key, it will be atomically inserted.
    /// </summary>
    /// <typeparam name="TKey">Type of the dictionary keys.</typeparam>
    /// <param name="dictionary">A <see cref="ConcurrentDictionary{TKey,TValue}"/>, possibly created by <see cref="CreateConcurrentDictionary{TKey,TValue}(System.Collections.Generic.IEnumerable{System.Collections.Generic.KeyValuePair{TKey,TValue}}?,int,int?,System.Collections.Generic.IEqualityComparer{TKey}?)"/>.</param>
    /// <param name="key">The key whose value you want to replace.</param>
    /// <param name="newValue">The new value that you want to swap into the dictionary.</param>
    /// <returns>The old value of the <paramref name="key"/> that was swapped out of <paramref name="dictionary"/>, replaced by <paramref name="newValue"/>, or <c>null</c> if <paramref name="dictionary"/> did not already contain this <paramref name="key"/>.</returns>
    public static IntPtr? Swap<TKey>(this ConcurrentDictionary<TKey, ValueHolder<IntPtr>> dictionary, TKey key, IntPtr newValue) where TKey: notnull {
        var     newHolder = new ValueHolder<IntPtr>(newValue);
        IntPtr? oldValue;
        bool    inserted;
        do {
            ValueHolder<IntPtr> existingHolder = dictionary.GetOrAdd(key, newHolder);
            inserted = ReferenceEquals(newHolder, existingHolder);
            oldValue = inserted ? null : Interlocked.Exchange(ref existingHolder.Value, newValue);
        } while (!inserted && !dictionary.ContainsKey(key));
        return oldValue;
    }

    /// <inheritdoc cref="CompareAndSwap{TKey}(System.Collections.Concurrent.ConcurrentDictionary{TKey,Unfucked.ValueHolder{long}},TKey,long,long)" />
    public static IntPtr? CompareAndSwap<TKey>(this ConcurrentDictionary<TKey, ValueHolder<IntPtr>> dictionary, TKey key, IntPtr oldValue, IntPtr newValue) where TKey: notnull =>
        dictionary.TryGetValue(key, out ValueHolder<IntPtr>? existingHolder) ? Interlocked.CompareExchange(ref existingHolder.Value, newValue, oldValue) : null;

#if NET8_0_OR_GREATER
    /// <summary>
    /// Atomically swap a new value into a dictionary with a given key, getting the old value. If the dictionary does not already contain this key, it will be atomically inserted.
    /// </summary>
    /// <typeparam name="TKey">Type of the dictionary keys.</typeparam>
    /// <param name="dictionary">A <see cref="ConcurrentDictionary{TKey,TValue}"/>, possibly created by <see cref="CreateConcurrentDictionary{TKey,TValue}(System.Collections.Generic.IEnumerable{System.Collections.Generic.KeyValuePair{TKey,TValue}}?,int,int?,System.Collections.Generic.IEqualityComparer{TKey}?)"/>.</param>
    /// <param name="key">The key whose value you want to replace.</param>
    /// <param name="newValue">The new value that you want to swap into the dictionary.</param>
    /// <returns>The old value of the <paramref name="key"/> that was swapped out of <paramref name="dictionary"/>, replaced by <paramref name="newValue"/>, or <c>null</c> if <paramref name="dictionary"/> did not already contain this <paramref name="key"/>.</returns>
    public static UIntPtr? Swap<TKey>(this ConcurrentDictionary<TKey, ValueHolder<UIntPtr>> dictionary, TKey key, UIntPtr newValue) where TKey: notnull {
        var      newHolder = new ValueHolder<UIntPtr>(newValue);
        UIntPtr? oldValue;
        bool     inserted;
        do {
            ValueHolder<UIntPtr> existingHolder = dictionary.GetOrAdd(key, newHolder);
            inserted = ReferenceEquals(newHolder, existingHolder);
            oldValue = inserted ? null : Interlocked.Exchange(ref existingHolder.Value, newValue);
        } while (!inserted && !dictionary.ContainsKey(key));
        return oldValue;
    }

    /// <inheritdoc cref="CompareAndSwap{TKey}(System.Collections.Concurrent.ConcurrentDictionary{TKey,Unfucked.ValueHolder{long}},TKey,long,long)" />
    public static UIntPtr? CompareAndSwap<TKey>(this ConcurrentDictionary<TKey, ValueHolder<UIntPtr>> dictionary, TKey key, UIntPtr oldValue, UIntPtr newValue) where TKey: notnull =>
        dictionary.TryGetValue(key, out ValueHolder<UIntPtr>? existingHolder) ? Interlocked.CompareExchange(ref existingHolder.Value, newValue, oldValue) : null;
#endif

    /// <summary>
    /// Atomically swap a new value into a dictionary with a given key, getting the old value. If the dictionary does not already contain this key, it will be atomically inserted.
    /// </summary>
    /// <typeparam name="TKey">Type of the dictionary keys.</typeparam>
    /// <typeparam name="TValue">Non-nullable reference type of the dictionary values. For nullable reference type, use <see cref="SwapNullable{TKey,TValue}"/>.</typeparam>
    /// <param name="dictionary">A <see cref="ConcurrentDictionary{TKey,TValue}"/>, possibly created by <see cref="CreateConcurrentDictionary{TKey,TValue}(System.Collections.Generic.IEnumerable{System.Collections.Generic.KeyValuePair{TKey,TValue}}?,int,int?,System.Collections.Generic.IEqualityComparer{TKey}?)"/>.</param>
    /// <param name="key">The key whose value you want to replace.</param>
    /// <param name="newValue">The new value that you want to swap into the dictionary.</param>
    /// <returns>The old value of the <paramref name="key"/> that was swapped out of <paramref name="dictionary"/>, replaced by <paramref name="newValue"/>, or <c>null</c> if <paramref name="dictionary"/> did not already contain this <paramref name="key"/>.</returns>
    public static TValue? Swap<TKey, TValue>(this ConcurrentDictionary<TKey, ValueHolder<TValue>> dictionary, TKey key, TValue newValue) where TKey: notnull where TValue: class {
        var     newHolder = new ValueHolder<TValue>(newValue);
        TValue? oldValue;
        bool    inserted;
        do {
            ValueHolder<TValue> existingHolder = dictionary.GetOrAdd(key, newHolder);
            inserted = ReferenceEquals(newHolder, existingHolder);
            oldValue = inserted ? null : Interlocked.Exchange(ref existingHolder.Value, newValue);
        } while (!inserted && !dictionary.ContainsKey(key));
        return oldValue;
    }

    /// <summary>
    /// Atomically compare and swap a new value into a dictionary with a given key, only if the existing value equals a given value, and returning the old value. If the dictionary does not already contain this key, it will not be inserted, and <c>null</c> will be returned.
    /// </summary>
    /// <typeparam name="TKey">Type of the dictionary keys.</typeparam>
    /// <typeparam name="TValue">Non-nullable reference type of the dictionary values. For nullable reference type, use <see cref="CompareAndSwapNullable{TKey,TValue}"/>.</typeparam>
    /// <param name="dictionary">A <see cref="ConcurrentDictionary{TKey,TValue}"/>, possibly created by <see cref="CreateConcurrentDictionary{TKey,TValue}(System.Collections.Generic.IEnumerable{System.Collections.Generic.KeyValuePair{TKey,TValue}}?,int,int?,System.Collections.Generic.IEqualityComparer{TKey}?)"/>.</param>
    /// <param name="key">The key whose value you want to conditionally replace.</param>
    /// <param name="oldValue">The old value that must be equal to the existing value in the dictionary in order to swap it.</param>
    /// <param name="newValue">The new value that you want to swap into the dictionary.</param>
    /// <returns>The old value <paramref name="oldValue"/>, which is the old value of <paramref name="key"/> in <paramref name="dictionary"/>, or <c>null</c> if <paramref name="dictionary"/> did not already contain the key <paramref name="key"/> so no insertion or swapping occurred.</returns>
    public static TValue? CompareAndSwap<TKey, TValue>(this ConcurrentDictionary<TKey, ValueHolder<TValue>> dictionary, TKey key, TValue oldValue, TValue newValue)
        where TKey: notnull where TValue: class =>
        dictionary.TryGetValue(key, out ValueHolder<TValue>? existingHolder) ? Interlocked.CompareExchange(ref existingHolder.Value, newValue, oldValue) : null;

    /// <summary>
    /// Atomically swap a new value into a dictionary with a given key, getting the old value. If the dictionary does not already contain this key, it will be atomically inserted.
    /// </summary>
    /// <typeparam name="TKey">Type of the dictionary keys.</typeparam>
    /// <typeparam name="TValue">Nullable reference type of the dictionary values. For non-nullable reference type, use <see cref="Swap{TKey,TValue}"/>.</typeparam>
    /// <param name="dictionary">A <see cref="ConcurrentDictionary{TKey,TValue}"/>, possibly created by <see cref="CreateConcurrentDictionary{TKey,TValue}(System.Collections.Generic.IEnumerable{System.Collections.Generic.KeyValuePair{TKey,TValue}}?,int,int?,System.Collections.Generic.IEqualityComparer{TKey}?)"/>.</param>
    /// <param name="key">The key whose value you want to replace.</param>
    /// <param name="newValue">The new value that you want to swap into the dictionary.</param>
    /// <returns>The old value of the <paramref name="key"/> that was swapped out of <paramref name="dictionary"/>, replaced by <paramref name="newValue"/>, or <c>null</c> if <paramref name="dictionary"/> did not already contain this <paramref name="key"/>.</returns>
    public static TValue? SwapNullable<TKey, TValue>(this ConcurrentDictionary<TKey, ValueHolder<TValue?>> dictionary, TKey key, TValue? newValue) where TKey: notnull where TValue: class? {
        var     newHolder = new ValueHolder<TValue?>(newValue);
        TValue? oldValue;
        bool    inserted;
        do {
            ValueHolder<TValue?> existingHolder = dictionary.GetOrAdd(key, newHolder);
            inserted = ReferenceEquals(newHolder, existingHolder);
            oldValue = inserted ? null : Interlocked.Exchange(ref existingHolder.Value, newValue);
        } while (!inserted && !dictionary.ContainsKey(key));
        return oldValue;
    }

    /// <summary>
    /// Atomically compare and swap a new value into a dictionary with a given key, only if the existing value equals a given value, and returning the old value. If the dictionary does not already contain this key, it will not be inserted, and <c>null</c> will be returned.
    /// </summary>
    /// <typeparam name="TKey">Type of the dictionary keys.</typeparam>
    /// <typeparam name="TValue">Nullable reference type of the dictionary values. For non-nullable reference type, use <see cref="Swap{TKey,TValue}"/>.</typeparam>
    /// <param name="dictionary">A <see cref="ConcurrentDictionary{TKey,TValue}"/>, possibly created by <see cref="CreateConcurrentDictionary{TKey,TValue}(System.Collections.Generic.IEnumerable{System.Collections.Generic.KeyValuePair{TKey,TValue}}?,int,int?,System.Collections.Generic.IEqualityComparer{TKey}?)"/>.</param>
    /// <param name="key">The key whose value you want to conditionally replace.</param>
    /// <param name="oldValue">The old value that must be equal to the existing value in the dictionary in order to swap it.</param>
    /// <param name="newValue">The new value that you want to swap into the dictionary.</param>
    /// <returns>The old value <paramref name="oldValue"/>, which is the old value of <paramref name="key"/> in <paramref name="dictionary"/>, or <c>null</c> if <paramref name="dictionary"/> did not already contain the key <paramref name="key"/> so no insertion or swapping occurred.</returns>
    public static TValue? CompareAndSwapNullable<TKey, TValue>(this ConcurrentDictionary<TKey, ValueHolder<TValue?>> dictionary, TKey key, TValue? oldValue, TValue newValue)
        where TKey: notnull where TValue: class? =>
        dictionary.TryGetValue(key, out ValueHolder<TValue?>? existingHolder) ? Interlocked.CompareExchange(ref existingHolder.Value, newValue, oldValue) : null;

    #endregion

    #region Upsert with disposal

    /// <summary>
    /// <para>Adds a key/value pair to the <see cref="T:System.Collections.Concurrent.ConcurrentDictionary`2" /> by using the specified function if the key does not already exist. Returns the new value, or the existing value if the key exists.</para>
    /// <para>This extension method will also dispose of the value created by <paramref name="valueFactory"/> if it was unused. To avoid deadlocks, <see cref="ConcurrentDictionary{TKey,TValue}"/> does not atomically create the value and add it to the dictionary, because <paramref name="valueFactory"/> is untrusted code and could deadlock. Instead, the <see cref="ConcurrentDictionary{TKey,TValue}"/> takes a three phased approach: check if the key already exists, create the value, and add the value. This means that the key could be concurrently added after the first check, which would lead to the value being created in the second step but not added in the third step. In this case, the created value is unused and will never be disposed.</para>
    /// <para>If you want values created by <paramref name="valueFactory"/> that are never added to the dictionary to be disposed, call this method.</para>
    /// </summary>
    /// <typeparam name="TKey">Type of keys in the dictionary.</typeparam>
    /// <typeparam name="TValue">Type of values in the dictionary.</typeparam>
    /// <param name="dictionary">The <see cref="ConcurrentDictionary{TKey,TValue}"/> to get or add a value to.</param>
    /// <param name="key">The key of the element to add.</param>
    /// <param name="valueFactory">The function used to generate a value for the key.</param>
    /// <param name="added">Will be <c>true</c> if the new value from <paramref name="valueFactory"/> was inserted into the <paramref name="dictionary"/>, or <c>false</c> if <paramref name="dictionary"/> already contained a value with the key <paramref name="key"/> which was not replaced.</param>
    /// <exception cref="T:System.ArgumentNullException"><paramref name="key" /> or <paramref name="valueFactory" /> is <c>null</c>.</exception>
    /// <exception cref="T:System.OverflowException">The dictionary contains too many elements.</exception>
    /// <returns>The value for the key. This will be either the existing value for the key if the key is already in the dictionary, or the new value if the key was not in the dictionary.</returns>
    public static TValue GetOrAddWithDisposal<TKey, TValue>(this ConcurrentDictionary<TKey, TValue> dictionary, TKey key, Func<TKey, TValue> valueFactory, out bool added)
        where TKey: notnull where TValue: IDisposable {

        TValue? toAdd = default;

        TValue result = GetOrAdd(dictionary, key, k => {
            toAdd = valueFactory(k);
            return toAdd;
        }, out bool innerAdded);

        if (!innerAdded) {
            toAdd?.Dispose();
        }

        added = innerAdded;
        return result;
    }

    /// <summary>
    /// <para>Adds a key/value pair to the <see cref="T:System.Collections.Concurrent.ConcurrentDictionary`2" /> by using the specified function if the key does not already exist. Returns the new value, or the existing value if the key exists.</para>
    /// <para>This extension method will also dispose of the value created by <paramref name="valueFactory"/> if it was unused. To avoid deadlocks, <see cref="ConcurrentDictionary{TKey,TValue}"/> does not atomically create the value and add it to the dictionary, because <paramref name="valueFactory"/> is untrusted code and could deadlock. Instead, the <see cref="ConcurrentDictionary{TKey,TValue}"/> takes a three phased approach: check if the key already exists, create the value, and add the value. This means that the key could be concurrently added after the first check, which would lead to the value being created in the second step but not added in the third step. In this case, the created value is unused and will never be disposed.</para>
    /// <para>If you want values created by <paramref name="valueFactory"/> that are never added to the dictionary to be disposed, call this method.</para>
    /// </summary>
    /// <typeparam name="TKey">Type of keys in the dictionary.</typeparam>
    /// <typeparam name="TValue">Type of values in the dictionary.</typeparam>
    /// <param name="dictionary">The <see cref="ConcurrentDictionary{TKey,TValue}"/> to get or add a value to.</param>
    /// <param name="key">The key of the element to add.</param>
    /// <param name="valueFactory">The function used to generate a value for the key.</param>
    /// <exception cref="T:System.ArgumentNullException"><paramref name="key" /> or <paramref name="valueFactory" /> is <c>null</c>.</exception>
    /// <exception cref="T:System.OverflowException">The dictionary contains too many elements.</exception>
    /// <returns>A tuple containing the eventual value for the key, as well as whether the eventual value was generated by <paramref name="valueFactory"/> and inserted. The eventual value will be either the existing value for the key if the key is already in the dictionary, or the new value if the key was not in the dictionary.</returns>
    public static async Task<(TValue actualValue, bool added)> GetOrAddWithAsyncDisposal<TKey, TValue>(this ConcurrentDictionary<TKey, TValue> dictionary, TKey key, Func<TKey, TValue> valueFactory)
        where TKey: notnull where TValue: IAsyncDisposable {

        TValue? toAdd = default;

        TValue result = GetOrAdd(dictionary, key, k => {
            toAdd = valueFactory(k);
            return toAdd;
        }, out bool innerAdded);

        if (!innerAdded && toAdd is not null) {
            await toAdd.DisposeAsync().ConfigureAwait(false);
        }

        return (result, innerAdded);
    }

    #endregion

    #region Upsert with result

    /// <summary>
    /// Atomically read or insert value into <see cref="ConcurrentDictionary{TKey,TValue}"/>, returning the eventual value (regardless of whether it was inserted or already existed), and a flag indicating whether it was either inserted or already existed.
    /// </summary>
    /// <typeparam name="TKey">Type of keys in the dictionary.</typeparam>
    /// <typeparam name="TValue">Type of values in the dictionary.</typeparam>
    /// <param name="dictionary">The <see cref="ConcurrentDictionary{TKey,TValue}"/> to get or add a value to.</param>
    /// <param name="key">The key of the element to add.</param>
    /// <param name="newValue">The new value that you want to insert into the dictionary.</param>
    /// <param name="added">Will be <c>true</c> if <paramref name="newValue"/> was inserted into the <paramref name="dictionary"/>, or <c>false</c> if <paramref name="dictionary"/> already contained a value with the key <paramref name="key"/> which was not replaced.</param>
    /// <returns></returns>
    public static TValue GetOrAdd<TKey, TValue>(this ConcurrentDictionary<TKey, TValue> dictionary, TKey key, TValue newValue, out bool added) where TKey: notnull {
        TValue actual = dictionary.GetOrAdd(key, newValue);
        added = IsAdded(newValue, actual);
        return actual;
    }

    public static TValue GetOrAdd<TKey, TValue>(this ConcurrentDictionary<TKey, TValue> dictionary, TKey key, Func<TKey, TValue> valueFactory, out bool added) where TKey: notnull {
        TValue? toAdd   = default;
        bool    created = false;
        TValue actual = dictionary.GetOrAdd(key, k => {
            toAdd   = valueFactory(k);
            created = true;
            return toAdd;
        });

        added = created && IsAdded(toAdd, actual);
        return actual;
    }

    private static bool IsAdded<TValue>(TValue supplied, TValue actual) {
        if (typeof(TValue).IsValueType) {
            if (supplied is IEquatable<TValue> equatableValue) {
                return equatableValue.Equals(actual);
            } else if (supplied is not null) {
                return supplied.Equals(actual);
            } else {
                return actual is null;
            }
        } else {
            return ReferenceEquals(supplied, actual);
        }
    }

    #endregion

}

#region Containers

/// <summary>
/// Wrapper class used as a dictionary value in <see cref="Enumerables.CreateConcurrentDictionary{TKey,TValue}(IEnumerable{KeyValuePair{TKey,TValue}}?,int,int?,IEqualityComparer{TKey}?)"/> to allow the value to be swapped with <see cref="Interlocked.Exchange{T}(ref T,T)"/> or <see cref="Enumerables.Swap{TKey,TValue}"/>.
/// </summary>
/// <typeparam name="T">Type of the dictionary value.</typeparam>
/// <param name="value">Initial value for the dictionary key-value pair.</param>
public class ValueHolder<T>(T value): IEquatable<ValueHolder<T>> {

    /// <summary>
    /// Actual value of the dictionary key-value pair. Can be atomically updated and the old value returned using <see cref="Enumerables.Swap{TKey,TValue}"/>.
    /// </summary>
    public T Value = value;

    /// <inheritdoc />
    public bool Equals(ValueHolder<T>? other) => other is not null && (ReferenceEquals(this, other) || EqualityComparer<T>.Default.Equals(Value, other.Value));

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is not null && (ReferenceEquals(this, obj) || (obj.GetType() == typeof(ValueHolder<T>) && Equals((ValueHolder<T>) obj)));

    /// <inheritdoc />
    public override int GetHashCode() => EqualityComparer<T>.Default.GetHashCode(Value!);

    public static bool operator ==(ValueHolder<T>? left, ValueHolder<T>? right) => Equals(left, right);

    public static bool operator !=(ValueHolder<T>? left, ValueHolder<T>? right) => !Equals(left, right);

}

/// <summary>
/// Wrapper class for <see cref="Enum"/>s used as a dictionary value in <see cref="Enumerables.CreateConcurrentEnumDictionary{TKey,TEnumValue,TIntegralValue}"/> to allow the value to be swapped with <see cref="Enumerables.Swap{TKey,TIntegralValue,TValue}"/>.
/// </summary>
/// <typeparam name="TEnum"><see cref="Enum"/> type, such as <c>MyEnum</c>, not the underlying integral type.</typeparam>
/// <typeparam name="TUnderlying">Underlying integral type of <typeparamref name="TEnum"/>, such as <see cref="int"/> or <see cref="long"/>.</typeparam>
/// <param name="enumValue">Initial enum value for the dictionary key-value pair.</param>
public class EnumValueHolder<TEnum, TUnderlying>(TEnum enumValue)
    : ValueHolder<TUnderlying>((TUnderlying) Convert.ChangeType(enumValue, enumValue.GetTypeCode())) where TUnderlying: struct where TEnum: struct, Enum {

    private readonly TypeCode underlyingEnumType = enumValue.GetTypeCode();

    /// <summary>
    /// Enum value of the dictionary key-value pair, automatically converted to and from its underlying type. Can be atomically updated and the old value returned using <see cref="Enumerables.Swap{TKey,TIntegralValue,TValue}"/>.
    /// </summary>
    public new TEnum Value {
        get => (TEnum) Enum.ToObject(typeof(TEnum), ((ValueHolder<TUnderlying>) this).Value);
        set => ((ValueHolder<TUnderlying>) this).Value = (TUnderlying) Convert.ChangeType(value, value.GetTypeCode());
    }

#pragma warning disable CS8509 // The switch expression does not handle all possible values of its input type (it is not exhaustive). - Guarded by Convert.ChangeType calls in constructor and this method.
    /// <exception cref="ArgumentOutOfRangeException"><typeparamref name="TEnum"/>'s underlying integral type <typeparamref name="TUnderlying"/> is neither <see cref="int"/> nor <see cref="long"/> (.NET &lt; 6: nor <see cref="uint"/> nor <see cref="ulong"/>)</exception>
    internal TEnum Exchange(TEnum newEnumValue) {
        object newIntegralValue = Convert.ChangeType(newEnumValue, underlyingEnumType);
        object oldIntegralValue = underlyingEnumType switch {
            TypeCode.Int32 => Interlocked.Exchange(ref ((ValueHolder<int>) (object) this).Value, (int) newIntegralValue),
            TypeCode.Int64 => Interlocked.Exchange(ref ((ValueHolder<long>) (object) this).Value, (long) newIntegralValue),
#if NET6_0_OR_GREATER
            TypeCode.UInt32 => Interlocked.Exchange(ref ((ValueHolder<uint>) (object) this).Value, (uint) newIntegralValue),
            TypeCode.UInt64 => Interlocked.Exchange(ref ((ValueHolder<ulong>) (object) this).Value, (ulong) newIntegralValue),
#endif
            /*_ => throw new ArgumentOutOfRangeException(nameof(newEnumValue), newEnumValue,
                $"Enum values with underlying type {typeof(TEnum)} cannot be exchanged due to limitations on System.Threading.Interlocked. Only int and long are supported (as well as uint and ulong on .NET ≥ 6).")*/
        };
        return (TEnum) Enum.ToObject(typeof(TEnum), oldIntegralValue);
    }

    /// <exception cref="ArgumentOutOfRangeException"><typeparamref name="TEnum"/>'s underlying integral type <typeparamref name="TUnderlying"/> is neither <see cref="int"/> nor <see cref="long"/> (.NET &lt; 6: nor <see cref="uint"/> nor <see cref="ulong"/>)</exception>
    internal TEnum CompareExchange(TEnum oldEnumValue, TEnum newEnumValue) {
        object newIntegralValue         = Convert.ChangeType(newEnumValue, underlyingEnumType);
        object expectedOldIntegralValue = Convert.ChangeType(oldEnumValue, underlyingEnumType);
        object actualOldIntegralValue = underlyingEnumType switch {
            TypeCode.Int32 => Interlocked.CompareExchange(ref ((ValueHolder<int>) (object) this).Value, (int) newIntegralValue, (int) expectedOldIntegralValue),
            TypeCode.Int64 => Interlocked.CompareExchange(ref ((ValueHolder<long>) (object) this).Value, (long) newIntegralValue, (long) expectedOldIntegralValue),
#if NET6_0_OR_GREATER
            TypeCode.UInt32 => Interlocked.CompareExchange(ref ((ValueHolder<uint>) (object) this).Value, (uint) newIntegralValue, (uint) expectedOldIntegralValue),
            TypeCode.UInt64 => Interlocked.CompareExchange(ref ((ValueHolder<ulong>) (object) this).Value, (ulong) newIntegralValue, (ulong) expectedOldIntegralValue),
#endif
        };
        return (TEnum) Enum.ToObject(typeof(TEnum), actualOldIntegralValue);
    }
#pragma warning restore CS8509 // The switch expression does not handle all possible values of its input type (it is not exhaustive).

}

/// <summary>
/// Wrapper class for <see cref="bool"/>s used as a dictionary value in <see cref="Enumerables.CreateConcurrentBooleanDictionary{TKey}"/> to allow the value to be swapped with <see cref="Enumerables.Swap{TKey}(System.Collections.Concurrent.ConcurrentDictionary{TKey,Unfucked.BooleanValueHolder},TKey,bool)"/>.
/// </summary>
/// <param name="boolValue"></param>
public class BooleanValueHolder(bool boolValue): ValueHolder<int>(Convert.ToInt32(boolValue)) {

    /// <summary>
    /// Enum value of the dictionary key-value pair, automatically converted to and from an <see cref="int"/>. Can be atomically updated and the old value returned using <see cref="Enumerables.Swap{TKey}(System.Collections.Concurrent.ConcurrentDictionary{TKey,Unfucked.BooleanValueHolder},TKey,bool)"/>.
    /// </summary>
    public new bool Value {
        get => Convert.ToBoolean(((ValueHolder<int>) this).Value);
        set => ((ValueHolder<int>) this).Value = Convert.ToInt32(value);
    }

    internal bool Exchange(bool newBoolValue) =>
        Convert.ToBoolean(Interlocked.Exchange(ref ((ValueHolder<int>) this).Value, Convert.ToInt32(newBoolValue)));

    internal bool CompareExchange(bool oldValue, bool newValue) =>
        Convert.ToBoolean(Interlocked.CompareExchange(ref ((ValueHolder<int>) this).Value, Convert.ToInt32(newValue), Convert.ToInt32(oldValue)));

}

#endregion