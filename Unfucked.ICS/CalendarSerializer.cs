using Ical.Net.Serialization;
using System.Reflection;
using System.Text;

namespace Unfucked;

public static class CalendarSerializer {

    private static readonly Type       EncodingStackType = typeof(SerializerBase).Assembly.GetType("Ical.Net.Serialization.EncodingStack")!;
    private static readonly MethodInfo EncodingStackPush = EncodingStackType.GetMethod("Push", [typeof(Encoding)])!;
    private static readonly MethodInfo EncodingStackPop  = EncodingStackType.GetMethod("Pop")!;

    /// <summary>
    /// Without this, you would have to manually configure your web server (Kestrel and IIS) to allow synchronous writes:
    /// <code>
    /// webappBuilder.WebHost.ConfigureKestrel(options =&gt; options.AllowSynchronousIO = true);
    /// webappBuilder.Services.Configure&lt;IISServerOptions&gt;(options =&gt; options.AllowSynchronousIO = true);
    /// </code>
    /// </summary>
    public static async Task SerializeAsync(this SerializerBase serializer, object dataToSerialize, Stream destinationStream, Encoding streamEncoding) {
#if NETSTANDARD2_1_OR_GREATER
        await using StreamWriter streamWriter = new(destinationStream, streamEncoding, 1024, true);
#else
        using StreamWriter streamWriter = new(destinationStream, streamEncoding, 1024, true);
#endif

        serializer.SerializationContext.Push(dataToSerialize);
        object encodingStack = serializer.GetService(EncodingStackType);
        EncodingStackPush.Invoke(encodingStack, [streamEncoding]);

        await streamWriter.WriteAsync(serializer.SerializeToString(dataToSerialize)).ConfigureAwait(false);

        EncodingStackPop.Invoke(encodingStack, []);
        serializer.SerializationContext.Pop();
    }

}