using System.Collections.Concurrent;

namespace Unfucked;

public static partial class Enumerables {

    private const int DefaultConcurrentDictionaryCapacity = 31;

    #region Factory Methods

    // Normal values like int and object
    /// <summary>
    /// Create a <see cref="ConcurrentDictionary{TKey,TValue}"/> whose values can be mutated atomically using <see cref="Interlocked"/>, especially using the extension methods like <see cref="Exchange{TKey,TValue}"/>.
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
    /// <returns>A <see cref="ConcurrentDictionary{TKey,TValue}"/> whose values can be mutated atomically using <see cref="Exchange{TKey,TValue}"/>.</returns>
    [Pure]
    public static ConcurrentDictionary<TKey, ValueHolder<TValue>> CreateConcurrentDictionary<TKey, TValue>(
        IEnumerable<KeyValuePair<TKey, TValue>>? initialElements = null,
        int concurrency = -1,
        int? capacity = null,
        IEqualityComparer<TKey>? keyComparer = null)
        where TKey: notnull {
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
            : new ConcurrentDictionary<TKey, ValueHolder<TValue>>(concurrency, capacity ?? DefaultConcurrentDictionaryCapacity, keyComparer);
    }

    // Boolean values
    /// <summary>
    /// <para>Create a <see cref="ConcurrentDictionary{TKey,TValue}"/> whose <see cref="bool"/> values can be mutated atomically using <see cref="Interlocked"/>, especially using the extension methods like <see cref="Exchange{TKey}(ConcurrentDictionary{TKey,BooleanValueHolder},TKey,bool)"/>.</para>
    /// <para>If you get an ambiguous invocation error, try calling <see cref="CreateConcurrentBooleanDictionary{TKey}"/>.</para>
    /// </summary>
    /// <typeparam name="TKey">Type of the dictionary keys.</typeparam>
    /// <typeparam name="TValue"><see cref="bool"/> dictionary values.</typeparam>
    /// <param name="initialElements">Optional key-value pairs to initially add to the new dictionary.</param>
    /// <param name="capacity">Optional expected maximum number of keys that this dictionary will contain, useful to prevent resizing as key-value pairs are added. Will be ignored if <paramref name="initialElements"/> is not <c>null</c>.</param>
    /// <param name="keyComparer">Optional <see cref="IEqualityComparer{T}"/> to determine if two keys are the same.</param>
    /// <param name="concurrency">Optional maximum number of threads that are expected to access this dictionary concurrently, defaults to the total CPU thread count.</param>
    /// <returns>A <see cref="ConcurrentDictionary{TKey,TValue}"/> whose values can be mutated atomically using <see cref="Exchange{TKey,TValue}"/>.</returns>
    [Pure]
    public static ConcurrentDictionary<TKey, BooleanValueHolder> CreateConcurrentDictionary<TKey, TValue>(
        IEnumerable<KeyValuePair<TKey, bool>>? initialElements = null,
        int concurrency = -1,
        int? capacity = null,
        IEqualityComparer<TKey>? keyComparer = null)
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
            : new ConcurrentDictionary<TKey, BooleanValueHolder>(concurrency, capacity ?? DefaultConcurrentDictionaryCapacity, keyComparer);
    }

    // Boolean values helper for ambiguous signature error
    /// <summary>
    /// <para>Create a <see cref="ConcurrentDictionary{TKey,TValue}"/> whose <see cref="bool"/> values can be mutated atomically using <see cref="Interlocked"/>, especially using the extension methods like <see cref="Exchange{TKey}(ConcurrentDictionary{TKey,BooleanValueHolder},TKey,bool)"/>.</para>
    /// </summary>
    /// <typeparam name="TKey">Type of the dictionary keys.</typeparam>
    /// <param name="initialElements">Optional key-value pairs to initially add to the new dictionary.</param>
    /// <param name="capacity">Optional expected maximum number of keys that this dictionary will contain, useful to prevent resizing as key-value pairs are added. Will be ignored if <paramref name="initialElements"/> is not <c>null</c>.</param>
    /// <param name="keyComparer">Optional <see cref="IEqualityComparer{T}"/> to determine if two keys are the same.</param>
    /// <param name="concurrency">Optional maximum number of threads that are expected to access this dictionary concurrently, defaults to the total CPU thread count.</param>
    /// <returns>A <see cref="ConcurrentDictionary{TKey,TValue}"/> whose values can be mutated atomically using <see cref="Exchange{TKey,TValue}"/>.</returns>
    [Pure]
    public static ConcurrentDictionary<TKey, BooleanValueHolder> CreateConcurrentBooleanDictionary<TKey>(
        IEnumerable<KeyValuePair<TKey, bool>>? initialElements = null,
        int concurrency = -1,
        int? capacity = null,
        IEqualityComparer<TKey>? keyComparer = null)
        where TKey: notnull {
        return CreateConcurrentDictionary<TKey, bool>(initialElements, concurrency, capacity, keyComparer);
    }

    // Enum values
    /// <summary>
    /// Create a <see cref="ConcurrentDictionary{TKey,TValue}"/> whose <see cref="Enum"/> values can be mutated atomically using <see cref="Exchange{TKey,TIntegralValue,TValue}"/> or <see cref="Interlocked"/>.
    /// </summary>
    /// <typeparam name="TKey">Type of the dictionary keys.</typeparam>
    /// <typeparam name="TEnumValue">Type of the dictionary <see cref="Enum"/> values, like <c>MyEnum</c> (not the underlying integral type like <see cref="int"/>).</typeparam>
    /// <typeparam name="TIntegralValue">The underlying integral type of the <typeparamref name="TEnumValue"/>'s <see cref="Enum"/>, such as <see cref="int"/> or <see cref="long"/>.</typeparam>
    /// <param name="initialElements">Optional key-value pairs to initially add to the new dictionary.</param>
    /// <param name="capacity">Optional expected maximum number of keys that this dictionary will contain, useful to prevent resizing as key-value pairs are added. Will be ignored if <paramref name="initialElements"/> is not <c>null</c>.</param>
    /// <param name="keyComparer">Optional <see cref="IEqualityComparer{T}"/> to determine if two keys are the same.</param>
    /// <param name="concurrency">Optional maximum number of threads that are expected to access this dictionary concurrently, defaults to the total CPU thread count.</param>
    /// <returns>A <see cref="ConcurrentDictionary{TKey,TValue}"/> whose values can be mutated atomically using <see cref="Exchange{TKey,TValue}"/>.</returns>
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
            : new ConcurrentDictionary<TKey, EnumValueHolder<TEnumValue, TIntegralValue>>(concurrency, capacity ?? DefaultConcurrentDictionaryCapacity, keyComparer);
    }

    #endregion

    #region Exchange enums

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
    public static TValue? Exchange<TKey, TIntegralValue, TValue>(this ConcurrentDictionary<TKey, EnumValueHolder<TValue, TIntegralValue>> dictionary, TKey key, TValue newValue)
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
    /// Atomically swap a new value into a dictionary with a given key, getting the old value. If the dictionary does not already contain this key, it will be atomically inserted.
    /// </summary>
    /// <typeparam name="TKey">Type of the dictionary keys.</typeparam>
    /// <param name="dictionary">A <see cref="ConcurrentDictionary{TKey,TValue}"/> created by <see cref="CreateConcurrentBooleanDictionary{TKey}"/>.</param>
    /// <param name="key">The key whose value you want to replace.</param>
    /// <param name="newValue">The new value that you want to swap into the dictionary.</param>
    /// <returns>The old value of the <paramref name="key"/> that was swapped out of <paramref name="dictionary"/>, replaced by <paramref name="newValue"/>, or <c>null</c> if <paramref name="dictionary"/> did not already contain this <paramref name="key"/>.</returns>
    public static bool? Exchange<TKey>(this ConcurrentDictionary<TKey, BooleanValueHolder> dictionary, TKey key, bool newValue)
        where TKey: notnull {
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
    public static long? Exchange<TKey>(this ConcurrentDictionary<TKey, ValueHolder<long>> dictionary, TKey key, long newValue) where TKey: notnull {
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
    /// Atomically swap a new value into a dictionary with a given key, getting the old value. If the dictionary does not already contain this key, it will be atomically inserted.
    /// </summary>
    /// <typeparam name="TKey">Type of the dictionary keys.</typeparam>
    /// <param name="dictionary">A <see cref="ConcurrentDictionary{TKey,TValue}"/>, possibly created by <see cref="CreateConcurrentDictionary{TKey,TValue}(System.Collections.Generic.IEnumerable{System.Collections.Generic.KeyValuePair{TKey,TValue}}?,int,int?,System.Collections.Generic.IEqualityComparer{TKey}?)"/>.</param>
    /// <param name="key">The key whose value you want to replace.</param>
    /// <param name="newValue">The new value that you want to swap into the dictionary.</param>
    /// <returns>The old value of the <paramref name="key"/> that was swapped out of <paramref name="dictionary"/>, replaced by <paramref name="newValue"/>, or <c>null</c> if <paramref name="dictionary"/> did not already contain this <paramref name="key"/>.</returns>
    public static int? Exchange<TKey>(this ConcurrentDictionary<TKey, ValueHolder<int>> dictionary, TKey key, int newValue) where TKey: notnull {
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

    /// <summary>
    /// Atomically swap a new value into a dictionary with a given key, getting the old value. If the dictionary does not already contain this key, it will be atomically inserted.
    /// </summary>
    /// <typeparam name="TKey">Type of the dictionary keys.</typeparam>
    /// <param name="dictionary">A <see cref="ConcurrentDictionary{TKey,TValue}"/>, possibly created by <see cref="CreateConcurrentDictionary{TKey,TValue}(System.Collections.Generic.IEnumerable{System.Collections.Generic.KeyValuePair{TKey,TValue}}?,int,int?,System.Collections.Generic.IEqualityComparer{TKey}?)"/>.</param>
    /// <param name="key">The key whose value you want to replace.</param>
    /// <param name="newValue">The new value that you want to swap into the dictionary.</param>
    /// <returns>The old value of the <paramref name="key"/> that was swapped out of <paramref name="dictionary"/>, replaced by <paramref name="newValue"/>, or <c>null</c> if <paramref name="dictionary"/> did not already contain this <paramref name="key"/>.</returns>
    public static double? Exchange<TKey>(this ConcurrentDictionary<TKey, ValueHolder<double>> dictionary, TKey key, double newValue) where TKey: notnull {
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

#if NET6_0_OR_GREATER
    /// <summary>
    /// Atomically swap a new value into a dictionary with a given key, getting the old value. If the dictionary does not already contain this key, it will be atomically inserted.
    /// </summary>
    /// <typeparam name="TKey">Type of the dictionary keys.</typeparam>
    /// <param name="dictionary">A <see cref="ConcurrentDictionary{TKey,TValue}"/>, possibly created by <see cref="CreateConcurrentDictionary{TKey,TValue}(System.Collections.Generic.IEnumerable{System.Collections.Generic.KeyValuePair{TKey,TValue}}?,int,int?,System.Collections.Generic.IEqualityComparer{TKey}?)"/>.</param>
    /// <param name="key">The key whose value you want to replace.</param>
    /// <param name="newValue">The new value that you want to swap into the dictionary.</param>
    /// <returns>The old value of the <paramref name="key"/> that was swapped out of <paramref name="dictionary"/>, replaced by <paramref name="newValue"/>, or <c>null</c> if <paramref name="dictionary"/> did not already contain this <paramref name="key"/>.</returns>
    public static uint? Exchange<TKey>(this ConcurrentDictionary<TKey, ValueHolder<uint>> dictionary, TKey key, uint newValue) where TKey: notnull {
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

    /// <summary>
    /// Atomically swap a new value into a dictionary with a given key, getting the old value. If the dictionary does not already contain this key, it will be atomically inserted.
    /// </summary>
    /// <typeparam name="TKey">Type of the dictionary keys.</typeparam>
    /// <param name="dictionary">A <see cref="ConcurrentDictionary{TKey,TValue}"/>, possibly created by <see cref="CreateConcurrentDictionary{TKey,TValue}(System.Collections.Generic.IEnumerable{System.Collections.Generic.KeyValuePair{TKey,TValue}}?,int,int?,System.Collections.Generic.IEqualityComparer{TKey}?)"/>.</param>
    /// <param name="key">The key whose value you want to replace.</param>
    /// <param name="newValue">The new value that you want to swap into the dictionary.</param>
    /// <returns>The old value of the <paramref name="key"/> that was swapped out of <paramref name="dictionary"/>, replaced by <paramref name="newValue"/>, or <c>null</c> if <paramref name="dictionary"/> did not already contain this <paramref name="key"/>.</returns>
    public static ulong? Exchange<TKey>(this ConcurrentDictionary<TKey, ValueHolder<ulong>> dictionary, TKey key, ulong newValue) where TKey: notnull {
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
#endif

    /// <summary>
    /// Atomically swap a new value into a dictionary with a given key, getting the old value. If the dictionary does not already contain this key, it will be atomically inserted.
    /// </summary>
    /// <typeparam name="TKey">Type of the dictionary keys.</typeparam>
    /// <param name="dictionary">A <see cref="ConcurrentDictionary{TKey,TValue}"/>, possibly created by <see cref="CreateConcurrentDictionary{TKey,TValue}(System.Collections.Generic.IEnumerable{System.Collections.Generic.KeyValuePair{TKey,TValue}}?,int,int?,System.Collections.Generic.IEqualityComparer{TKey}?)"/>.</param>
    /// <param name="key">The key whose value you want to replace.</param>
    /// <param name="newValue">The new value that you want to swap into the dictionary.</param>
    /// <returns>The old value of the <paramref name="key"/> that was swapped out of <paramref name="dictionary"/>, replaced by <paramref name="newValue"/>, or <c>null</c> if <paramref name="dictionary"/> did not already contain this <paramref name="key"/>.</returns>
    public static float? Exchange<TKey>(this ConcurrentDictionary<TKey, ValueHolder<float>> dictionary, TKey key, float newValue) where TKey: notnull {
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

    /// <summary>
    /// Atomically swap a new value into a dictionary with a given key, getting the old value. If the dictionary does not already contain this key, it will be atomically inserted.
    /// </summary>
    /// <typeparam name="TKey">Type of the dictionary keys.</typeparam>
    /// <param name="dictionary">A <see cref="ConcurrentDictionary{TKey,TValue}"/>, possibly created by <see cref="CreateConcurrentDictionary{TKey,TValue}(System.Collections.Generic.IEnumerable{System.Collections.Generic.KeyValuePair{TKey,TValue}}?,int,int?,System.Collections.Generic.IEqualityComparer{TKey}?)"/>.</param>
    /// <param name="key">The key whose value you want to replace.</param>
    /// <param name="newValue">The new value that you want to swap into the dictionary.</param>
    /// <returns>The old value of the <paramref name="key"/> that was swapped out of <paramref name="dictionary"/>, replaced by <paramref name="newValue"/>, or <c>null</c> if <paramref name="dictionary"/> did not already contain this <paramref name="key"/>.</returns>
    public static IntPtr? Exchange<TKey>(this ConcurrentDictionary<TKey, ValueHolder<IntPtr>> dictionary, TKey key, IntPtr newValue) where TKey: notnull {
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

#if NET8_0_OR_GREATER
    /// <summary>
    /// Atomically swap a new value into a dictionary with a given key, getting the old value. If the dictionary does not already contain this key, it will be atomically inserted.
    /// </summary>
    /// <typeparam name="TKey">Type of the dictionary keys.</typeparam>
    /// <param name="dictionary">A <see cref="ConcurrentDictionary{TKey,TValue}"/>, possibly created by <see cref="CreateConcurrentDictionary{TKey,TValue}(System.Collections.Generic.IEnumerable{System.Collections.Generic.KeyValuePair{TKey,TValue}}?,int,int?,System.Collections.Generic.IEqualityComparer{TKey}?)"/>.</param>
    /// <param name="key">The key whose value you want to replace.</param>
    /// <param name="newValue">The new value that you want to swap into the dictionary.</param>
    /// <returns>The old value of the <paramref name="key"/> that was swapped out of <paramref name="dictionary"/>, replaced by <paramref name="newValue"/>, or <c>null</c> if <paramref name="dictionary"/> did not already contain this <paramref name="key"/>.</returns>
    public static UIntPtr? Exchange<TKey>(this ConcurrentDictionary<TKey, ValueHolder<UIntPtr>> dictionary, TKey key, UIntPtr newValue) where TKey: notnull {
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
#endif

    /// <summary>
    /// Atomically swap a new value into a dictionary with a given key, getting the old value. If the dictionary does not already contain this key, it will be atomically inserted.
    /// </summary>
    /// <typeparam name="TKey">Type of the dictionary keys.</typeparam>
    /// <typeparam name="TValue">Non-nullable reference type of the dictionary values. For nullable reference type, use <see cref="ExchangeNullable{TKey,TValue}"/>.</typeparam>
    /// <param name="dictionary">A <see cref="ConcurrentDictionary{TKey,TValue}"/>, possibly created by <see cref="CreateConcurrentDictionary{TKey,TValue}(System.Collections.Generic.IEnumerable{System.Collections.Generic.KeyValuePair{TKey,TValue}}?,int,int?,System.Collections.Generic.IEqualityComparer{TKey}?)"/>.</param>
    /// <param name="key">The key whose value you want to replace.</param>
    /// <param name="newValue">The new value that you want to swap into the dictionary.</param>
    /// <returns>The old value of the <paramref name="key"/> that was swapped out of <paramref name="dictionary"/>, replaced by <paramref name="newValue"/>, or <c>null</c> if <paramref name="dictionary"/> did not already contain this <paramref name="key"/>.</returns>
    public static TValue? Exchange<TKey, TValue>(this ConcurrentDictionary<TKey, ValueHolder<TValue>> dictionary, TKey key, TValue newValue) where TKey: notnull where TValue: class {
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
    /// Atomically swap a new value into a dictionary with a given key, getting the old value. If the dictionary does not already contain this key, it will be atomically inserted.
    /// </summary>
    /// <typeparam name="TKey">Type of the dictionary keys.</typeparam>
    /// <typeparam name="TValue">Nullable reference type of the dictionary values. For non-nullable reference type, use <see cref="Exchange{TKey,TValue}"/></typeparam>
    /// <param name="dictionary">A <see cref="ConcurrentDictionary{TKey,TValue}"/>, possibly created by <see cref="CreateConcurrentDictionary{TKey,TValue}(System.Collections.Generic.IEnumerable{System.Collections.Generic.KeyValuePair{TKey,TValue}}?,int,int?,System.Collections.Generic.IEqualityComparer{TKey}?)"/>.</param>
    /// <param name="key">The key whose value you want to replace.</param>
    /// <param name="newValue">The new value that you want to swap into the dictionary.</param>
    /// <returns>The old value of the <paramref name="key"/> that was swapped out of <paramref name="dictionary"/>, replaced by <paramref name="newValue"/>, or <c>null</c> if <paramref name="dictionary"/> did not already contain this <paramref name="key"/>.</returns>
    public static TValue? ExchangeNullable<TKey, TValue>(this ConcurrentDictionary<TKey, ValueHolder<TValue?>> dictionary, TKey key, TValue? newValue) where TKey: notnull where TValue: class? {
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

    #endregion

    /// <summary>
    /// Wrapper class used as a dictionary value in <see cref="CreateConcurrentDictionary{TKey,TValue}(IEnumerable{KeyValuePair{TKey,TValue}}?,int,int?,IEqualityComparer{TKey}?)"/> to allow the value to be swapped with <see cref="Interlocked.Exchange{T}(ref T,T)"/> or <see cref="Exchange{TKey,TValue}"/>.
    /// </summary>
    /// <typeparam name="T">Type of the dictionary value.</typeparam>
    /// <param name="value">Initial value for the dictionary key-value pair.</param>
    public class ValueHolder<T>(T value): IEquatable<ValueHolder<T>> {

        /// <summary>
        /// Actual value of the dictionary key-value pair. Can be atomically updated and the old value returned using <see cref="Exchange{TKey,TValue}"/>.
        /// </summary>
        public T Value = value;

        /// <inheritdoc />
        public bool Equals(ValueHolder<T>? other) => other is not null && (ReferenceEquals(this, other) || EqualityComparer<T>.Default.Equals(Value, other.Value));

        /// <inheritdoc />
        public override bool Equals(object? obj) => obj is not null && (ReferenceEquals(this, obj) || (obj.GetType() == typeof(ValueHolder<T>) && Equals((ValueHolder<T>) obj)));

        /// <inheritdoc />
        public override int GetHashCode() => EqualityComparer<T>.Default.GetHashCode(Value);

        public static bool operator ==(ValueHolder<T>? left, ValueHolder<T>? right) => Equals(left, right);

        public static bool operator !=(ValueHolder<T>? left, ValueHolder<T>? right) => !Equals(left, right);

    }

    /// <summary>
    /// Wrapper class for <see cref="Enum"/>s used as a dictionary value in <see cref="Enumerables.CreateConcurrentEnumDictionary{TKey,TEnumValue,TIntegralValue}"/> to allow the value to be swapped with <see cref="Enumerables.Exchange{TKey,TIntegralValue,TValue}"/>.
    /// </summary>
    /// <typeparam name="TEnum"><see cref="Enum"/> type, such as <c>MyEnum</c>, not the underlying integral type.</typeparam>
    /// <typeparam name="TUnderlying">Underlying integral type of <typeparamref name="TEnum"/>, such as <see cref="int"/> or <see cref="long"/>.</typeparam>
    /// <param name="enumValue">Initial enum value for the dictionary key-value pair.</param>
    public class EnumValueHolder<TEnum, TUnderlying>(TEnum enumValue)
        : ValueHolder<TUnderlying>((TUnderlying) Convert.ChangeType(enumValue, enumValue.GetTypeCode())) where TUnderlying: struct where TEnum: struct, Enum {

        private readonly TypeCode underlyingEnumType = enumValue.GetTypeCode();

        /// <summary>
        /// Enum value of the dictionary key-value pair, automatically converted to and from its underlying type. Can be atomically updated and the old value returned using <see cref="Enumerables.Exchange{TKey,TIntegralValue,TValue}"/>.
        /// </summary>
        public new TEnum Value {
            get => (TEnum) Enum.ToObject(typeof(TEnum), ((ValueHolder<TUnderlying>) this).Value);
            set => ((ValueHolder<TUnderlying>) this).Value = (TUnderlying) Convert.ChangeType(value, value.GetTypeCode());
        }

        /// <exception cref="ArgumentOutOfRangeException"><typeparamref name="TEnum"/>'s underlying integral type <typeparamref name="TUnderlying"/> is neither <see cref="int"/> nor <see cref="long"/> (.NET &lt; 6: nor <see cref="uint"/> nor <see cref="ulong"/>)</exception>
        internal TEnum Exchange(TEnum newEnumValue) {
#pragma warning disable CS8509 // The switch expression does not handle all possible values of its input type (it is not exhaustive). - Guarded by Convert.ChangeType calls in constructor and this method.
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
#pragma warning restore CS8509 // The switch expression does not handle all possible values of its input type (it is not exhaustive).
        }

    }

    /// <summary>
    /// Wrapper class for <see cref="bool"/>s used as a dictionary value in <see cref="Enumerables.CreateConcurrentBooleanDictionary{TKey}"/> to allow the value to be swapped with <see cref="Enumerables.Exchange{TKey}(ConcurrentDictionary{TKey,BooleanValueHolder},TKey,bool)"/>.
    /// </summary>
    /// <param name="boolValue"></param>
    public class BooleanValueHolder(bool boolValue): ValueHolder<int>(Convert.ToInt32(boolValue)) {

        /// <summary>
        /// Enum value of the dictionary key-value pair, automatically converted to and from an <see cref="int"/>. Can be atomically updated and the old value returned using <see cref="Enumerables.Exchange{TKey}(ConcurrentDictionary{TKey,BooleanValueHolder},TKey,bool)"/>.
        /// </summary>
        public new bool Value {
            get => Convert.ToBoolean(((ValueHolder<int>) this).Value);
            set => ((ValueHolder<int>) this).Value = Convert.ToInt32(value);
        }

        internal bool Exchange(bool newBoolValue) {
            return Convert.ToBoolean(Interlocked.Exchange(ref ((ValueHolder<int>) this).Value, Convert.ToInt32(newBoolValue)));
        }

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
    /// <exception cref="T:System.ArgumentNullException">
    /// <paramref name="key" /> or <paramref name="valueFactory" /> is <c>null</c>.</exception>
    /// <exception cref="T:System.OverflowException">The dictionary contains too many elements.</exception>
    /// <returns>The value for the key. This will be either the existing value for the key if the key is already in the dictionary, or the new value if the key was not in the dictionary.</returns>
    public static TValue GetOrAddWithDisposal<TKey, TValue>(this ConcurrentDictionary<TKey, TValue> dictionary, TKey key, Func<TKey, TValue> valueFactory)
        where TKey: notnull where TValue: IDisposable {

        TValue? toAdd = default;

        TValue result = GetOrAdd(dictionary, key, k => {
            toAdd = valueFactory(k);
            return toAdd;
        }, out bool added);

        if (!added) {
            toAdd?.Dispose();
        }

        return result;
    }

    /// <summary>
    /// <para>Adds a key/value pair to the <see cref="T:System.Collections.Concurrent.ConcurrentDictionary`2" /> by using the specified function and an argument if the key does not already exist, or returns the existing value if the key exists.</para>
    /// <para>This extension method will also dispose of the value created by <paramref name="valueFactory"/> if it was unused. To avoid deadlocks, <see cref="ConcurrentDictionary{TKey,TValue}"/> does not atomically create the value and add it to the dictionary, because <paramref name="valueFactory"/> is untrusted code and could deadlock. Instead, the <see cref="ConcurrentDictionary{TKey,TValue}"/> takes a three phased approach: check if the key already exists, create the value, and add the value. This means that the key could be concurrently added after the first check, which would lead to the value being created in the second step but not added in the third step. In this case, the created value is unused and will never be disposed.</para>
    /// <para>If you want values created by <paramref name="valueFactory"/> that are never added to the dictionary to be disposed, call this method.</para>
    /// </summary>
    /// <param name="dictionary">The <see cref="ConcurrentDictionary{TKey,TValue}"/> to get or add a value to.</param>
    /// <param name="key">The key of the element to add.</param>
    /// <param name="valueFactory">The function used to generate a value for the key.</param>
    /// <param name="factoryArgument">An argument value to pass into <paramref name="valueFactory" />.</param>
    /// <typeparam name="TKey">Type of keys in the dictionary.</typeparam>
    /// <typeparam name="TArg">The type of an argument to pass into <paramref name="valueFactory" />.</typeparam>
    /// <typeparam name="TValue">Type of values in the dictionary.</typeparam>
    /// <exception cref="T:System.ArgumentNullException">
    /// <paramref name="key" /> is a <see langword="null" /> reference (Nothing in Visual Basic).</exception>
    /// <exception cref="T:System.OverflowException">The dictionary contains too many elements.</exception>
    /// <returns>The value for the key. This will be either the existing value for the key if the key is already in the dictionary, or the new value if the key was not in the dictionary.</returns>
    public static TValue GetOrAddWithDisposal<TKey, TArg, TValue>(this ConcurrentDictionary<TKey, TValue> dictionary, TKey key, Func<TKey, TArg, TValue> valueFactory, TArg factoryArgument)
        where TKey: notnull where TValue: IDisposable {
        return GetOrAddWithDisposal(dictionary, key, k => valueFactory(k, factoryArgument));
    }

    public static TValue GetOrAdd<TKey, TValue>(this ConcurrentDictionary<TKey, TValue> dictionary, TKey key, TValue value, out bool added) where TKey: notnull {
        TValue result = dictionary.GetOrAdd(key, value);
        added = ReferenceEquals(result, value);
        return result;
    }

    public static TValue GetOrAdd<TKey, TValue>(this ConcurrentDictionary<TKey, TValue> dictionary, TKey key, Func<TKey, TValue> valueFactory, out bool added) where TKey: notnull {
        TValue? toAdd   = default;
        bool    created = false;
        TValue result = dictionary.GetOrAdd(key, k => {
            toAdd   = valueFactory(k);
            created = true;
            return toAdd;
        });

        added = created && ReferenceEquals(toAdd, result);
        return result;
    }

    public static TValue GetOrAdd<TKey, TArg, TValue>(this ConcurrentDictionary<TKey, TValue> dictionary, TKey key, Func<TKey, TArg, TValue> valueFactory, TArg factoryArgument, out bool added)
        where TKey: notnull {
        return GetOrAdd(dictionary, key, k => valueFactory(k, factoryArgument), out added);
    }

}