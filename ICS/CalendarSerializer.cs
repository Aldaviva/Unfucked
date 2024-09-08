using Ical.Net.Serialization;
using System.Reflection;
using System.Text;

namespace Unfucked;

/// <summary>
/// Methods that make it easier to work with Ical.Net's <see cref="Ical.Net.Serialization.CalendarSerializer"/> class.
/// </summary>
public static class CalendarSerializer {

    private const int BufferSize = 1024;

    // ExceptionAdjustment: M:System.Reflection.Assembly.GetType(System.String) -T:System.BadImageFormatException
    private static readonly Type EncodingStackType = typeof(SerializerBase).Assembly.GetType("Ical.Net.Serialization.EncodingStack")!;

    // ExceptionAdjustment: M:System.Type.GetMethod(System.String,System.Type[]) -T:System.Reflection.AmbiguousMatchException
    private static readonly MethodInfo EncodingStackPush = EncodingStackType.GetMethod("Push", [typeof(Encoding)])!;

    // ExceptionAdjustment: M:System.Type.GetMethod(System.String) -T:System.Reflection.AmbiguousMatchException
    private static readonly MethodInfo EncodingStackPop = EncodingStackType.GetMethod("Pop")!;

    /// <summary>
    /// Without this, you would have to manually configure your web server (Kestrel and IIS) to allow synchronous writes:
    /// <code>
    /// webappBuilder.WebHost.ConfigureKestrel(options =&gt; options.AllowSynchronousIO = true);
    /// webappBuilder.Services.Configure&lt;IISServerOptions&gt;(options =&gt; options.AllowSynchronousIO = true);
    /// </code>
    /// </summary>
    // ExceptionAdjustment: M:System.Reflection.MethodBase.Invoke(System.Object,System.Object[]) -T:System.Reflection.TargetException
    // ExceptionAdjustment: M:System.Reflection.MethodBase.Invoke(System.Object,System.Object[]) -T:System.Reflection.TargetInvocationException
    // ExceptionAdjustment: M:System.Reflection.MethodBase.Invoke(System.Object,System.Object[]) -T:System.Reflection.TargetParameterCountException
    // ExceptionAdjustment: M:System.Reflection.MethodBase.Invoke(System.Object,System.Object[]) -T:System.MethodAccessException
    public static async Task SerializeAsync(this SerializerBase serializer, object dataToSerialize, Stream destinationStream, Encoding streamEncoding) {
#if NETSTANDARD2_1_OR_GREATER
        await using StreamWriter streamWriter = new(destinationStream, streamEncoding, BufferSize, true);
#else
        using StreamWriter streamWriter = new(destinationStream, streamEncoding, BufferSize, true);
#endif

        serializer.SerializationContext.Push(dataToSerialize);
        object encodingStack = serializer.GetService(EncodingStackType);
        EncodingStackPush.Invoke(encodingStack, [streamEncoding]);

        await streamWriter.WriteAsync(serializer.SerializeToString(dataToSerialize)).ConfigureAwait(false);

        EncodingStackPop.Invoke(encodingStack, []);
        serializer.SerializationContext.Pop();
    }

}