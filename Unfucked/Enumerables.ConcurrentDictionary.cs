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
        int                                      concurrency     = -1,
        int?                                     capacity        = null,
        IEqualityComparer<TKey>?                 keyComparer     = null)
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
        int                                    concurrency     = -1,
        int?                                   capacity        = null,
        IEqualityComparer<TKey>?               keyComparer     = null)
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
        int                                    concurrency     = -1,
        int?                                   capacity        = null,
        IEqualityComparer<TKey>?               keyComparer     = null)
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
        int                                          concurrency     = -1,
        int?                                         capacity        = null,
        IEqualityComparer<TKey>?                     keyComparer     = null)
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
    public class ValueHolder<T>(T value) {

        /// <summary>
        /// Actual value of the dictionary key-value pair. Can be atomically updated and the old value returned using <see cref="Exchange{TKey,TValue}"/>.
        /// </summary>
        public T Value = value;

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

}