#if NETSTANDARD2_1_OR_GREATER || NETFRAMEWORK || NETCOREAPP2_0_OR_GREATER
using System.Reflection;
using System.Reflection.Emit;
#endif

namespace Unfucked;

/// <summary>Proxy callback for an event handler delegate. This allows the subscriber to be garbage collected while the publisher still exists, without the need to manually convert a callback lambda to a method or local function and then manually unsubscribe in and call <c>Dispose</c>, thus avoiding a memory leak.</summary>
public static class WeakSubscriber {

    /// <summary>
    /// <para>Create a proxy callback for an event handler delegate. This allows the subscriber to be garbage collected while the publisher still exists, without the need to manually convert a callback lambda to a method or local function and then manually unsubscribe in and call <c>Dispose</c>, thus avoiding a memory leak.</para>
    /// <code>
    /// publisher.EventHappened += WeakSubscriber.Subscribe&lt;EventHandler&gt;((object? sender, EventArgs args) => Console.WriteLine("Event happened"));
    /// </code>
    /// </summary>
    /// <typeparam name="TDelegate">Event handler delegate type, such as <see cref="EventHandler"/> or <see cref="EventHandler{TEventArgs}"/></typeparam>
    /// <param name="subscriber">A callback to trigger when the event is fired.</param>
    /// <returns>A weak reference to <paramref name="subscriber"/>, which can be registered as an event callback.</returns>
    /// <exception cref="PlatformNotSupportedException">not available on .NET Standard 2.0, requires .NET Standard ≥ 2.1, .NET ≥ 5.0, .NET Core ≥ 2.0, or .NET Framework</exception>
    public static TDelegate Subscribe<TDelegate>(TDelegate subscriber) where TDelegate: Delegate =>
#if NETSTANDARD2_1_OR_GREATER || NETFRAMEWORK || NETCOREAPP2_0_OR_GREATER
        new WeakSubscriber<TDelegate>(subscriber).StrongOuterHandler;
#else
        throw new PlatformNotSupportedException(
            $"{nameof(WeakSubscriber)} is not available on .NET Standard 2.0. It requires .NET Standard ≥ 2.1, .NET ≥ 5.0, .NET Core ≥ 2.0, or .NET Framework.");
#endif

}

#if NETSTANDARD2_1_OR_GREATER || NETFRAMEWORK || NETCOREAPP2_0_OR_GREATER
internal sealed class WeakSubscriber<TDelegate> where TDelegate: Delegate {

    // ReSharper disable once StaticMemberInGenericType - it's a cache per generic type
    private static readonly DynamicMethod OUTER_HANDLER_METHOD = BuildOuterHandler();

    private readonly WeakReference<TDelegate> weakInnerHandler;

    public TDelegate StrongOuterHandler { get; }

    public WeakSubscriber(TDelegate innerHandler) {
        weakInnerHandler   = new WeakReference<TDelegate>(innerHandler);
        StrongOuterHandler = (TDelegate) OUTER_HANDLER_METHOD.CreateDelegate(typeof(TDelegate), this);
    }

    /*
     * https://learn.microsoft.com/en-us/dotnet/fundamentals/reflection/how-to-hook-up-a-delegate-using-reflection#generate-an-event-handler-at-runtime-by-using-a-dynamic-method
     */
    // ExceptionAdjustment: M:System.Type.GetMethod(System.String) -T:System.Reflection.AmbiguousMatchException
    // ExceptionAdjustment: M:System.Type.GetMethod(System.String,System.Reflection.BindingFlags,System.Reflection.Binder,System.Type[],System.Reflection.ParameterModifier[]) -T:System.Reflection.AmbiguousMatchException
    private static DynamicMethod BuildOuterHandler() {
        Type          subscriberClass = typeof(WeakSubscriber<TDelegate>);
        Type          delegateType    = typeof(TDelegate);
        MethodInfo    invokeMethod    = delegateType.GetMethod("Invoke")!;
        Type          returnType      = invokeMethod.ReturnType;
        bool          isVoid          = returnType == typeof(void);
        Type[]        parameterTypes  = ((IEnumerable<Type>) [subscriberClass]).Concat(invokeMethod.GetParameters().Select(static param => param.ParameterType)).ToArray();
        DynamicMethod outerMethod     = new("OuterHandler", returnType, parameterTypes, subscriberClass);
        ILGenerator   bytecode        = outerMethod.GetILGenerator();

        outerMethod.InitLocals = true;
        LocalBuilder  strongDelegateInstance    = bytecode.DeclareLocal(delegateType);
        LocalBuilder  hasStrongDelegateInstance = bytecode.DeclareLocal(typeof(bool));
        LocalBuilder? returnValue               = isVoid ? null : bytecode.DeclareLocal(returnType);

        Label  returnNow          = bytecode.DefineLabel();
        Label? returnDefaultValue = isVoid ? null : bytecode.DefineLabel();

        bytecode.Emit(OpCodes.Ldarg_0); // this
        bytecode.Emit(OpCodes.Ldfld, subscriberClass.GetField(nameof(weakInnerHandler), BindingFlags.NonPublic | BindingFlags.Instance)
            ?? throw new NullReferenceException($"No {nameof(weakInnerHandler)} field found"));
        bytecode.Emit(OpCodes.Ldloca_S, strongDelegateInstance);
        bytecode.Emit(OpCodes.Callvirt, typeof(WeakReference<TDelegate>).GetMethod(nameof(WeakReference<>.TryGetTarget))
            ?? throw new NullReferenceException($"No {nameof(WeakReference<>.TryGetTarget)} method found"));
        bytecode.Emit(OpCodes.Stloc, hasStrongDelegateInstance);

        bytecode.Emit(OpCodes.Ldloc, hasStrongDelegateInstance);
        bytecode.Emit(OpCodes.Brfalse_S, returnDefaultValue ?? returnNow);

        bytecode.Emit(OpCodes.Ldloc_0); // target
        for (byte i = 1; i < parameterTypes.Length; i++) {
            switch (i) {
                case 1:
                    bytecode.Emit(OpCodes.Ldarg_1);
                    break;
                case 2:
                    bytecode.Emit(OpCodes.Ldarg_2);
                    break;
                case 3:
                    bytecode.Emit(OpCodes.Ldarg_3);
                    break;
                default:
                    bytecode.Emit(OpCodes.Ldarg_S, i);
                    break;
            }
        }
        bytecode.Emit(OpCodes.Callvirt, invokeMethod);
        if (returnDefaultValue is not null && returnValue is not null) {
            bytecode.Emit(OpCodes.Stloc, returnValue);
            bytecode.Emit(OpCodes.Br_S, returnNow);

            bytecode.MarkLabel(returnDefaultValue.Value);
            bytecode.Emit(OpCodes.Call, subscriberClass.GetMethod(nameof(GetDeadReturnValue), BindingFlags.NonPublic | BindingFlags.Static, null, Type.EmptyTypes, null)?.MakeGenericMethod(returnType)
                ?? throw new NullReferenceException($"No {nameof(GetDeadReturnValue)} method found"));
            bytecode.Emit(OpCodes.Stloc, returnValue);
            bytecode.Emit(OpCodes.Br_S, returnNow);
        }

        bytecode.MarkLabel(returnNow);
        if (returnValue is not null) {
            bytecode.Emit(OpCodes.Ldloc, returnValue);
        }
        bytecode.Emit(OpCodes.Ret);

        return outerMethod;
    }

    private static T? GetDeadReturnValue<T>() => default;

}

#endif