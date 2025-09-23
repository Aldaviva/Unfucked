using System.Text;
using Unfucked.HTTP.Config;
#if NET5_0_OR_GREATER
using System.Reflection;
#endif

namespace Unfucked.HTTP.Filters;

public class WireLoggingFeature(WireLoggingFeature.Config config): Feature {

    // internal static readonly string StreamCorrelationOption = $"{typeof(WireLoggingFeature).Namespace!}.{nameof(WireLoggingFeature)}.{nameof(StreamCorrelationOption)}";

    public record Config {

        public delegate void LogWriter(string message, ulong requestId);

        public Func<bool> IsLogEnabled { get; init; } = () => true;
        public required LogWriter LogRequestTransmitted { get; init; }
        public required LogWriter LogResponseReceived { get; init; }

    }

    // internal interface IWireLoggingStream {

    // void OnResponseFinished();

    // }

#if NET5_0_OR_GREATER
    private static readonly PropertyKey<bool> Activated      = new($"{typeof(WireLoggingFeature).Namespace!}.{nameof(WireLoggingFeature)}.{nameof(Activated)}");
    private static readonly object            ActivationLock = new();

    private static readonly Lazy<FieldInfo?> SocketsHttpHandlerField = new(() => typeof(HttpClientHandler).GetFields(BindingFlags.NonPublic | BindingFlags.Instance)
        .FirstOrDefault(field => field.FieldType == typeof(SocketsHttpHandler)), LazyThreadSafetyMode.PublicationOnly);

    public async ValueTask OnBeforeRequest(IUnfuckedHttpHandler handler) {
        lock (ActivationLock) {
            if (!handler.Property(Activated, out bool isActivated) || !isActivated) {
                if (FindDescendantSocketsHandler(handler as UnfuckedHttpHandler) is { } socketsHttpHandler) {
                    socketsHttpHandler.PlaintextStreamFilter = socketsHttpHandler.PlaintextStreamFilter is { } existingStreamProvider
                        ? async (ctx, ct) => new WireLoggingStream(await existingStreamProvider(ctx, ct).ConfigureAwait(false), config, ctx.InitialRequestMessage)
                        : (ctx, _) => ValueTask.FromResult<Stream>(new WireLoggingStream(ctx.PlaintextStream, config, ctx.InitialRequestMessage));

                    // close streams immediately after finishing receiving the request, instead of leaving it open to send a future request, so that this feature can log the response after all of its chunks have been received, which is not otherwise detectable without race conditions
                    socketsHttpHandler.PooledConnectionLifetime = TimeSpan.Zero;
                    handler.Property(Activated, true);
                } else {
                    handler.Property(Activated, false);
                }
            }
        }
    }

    private static SocketsHttpHandler? FindDescendantSocketsHandler(HttpMessageHandler? parent) => parent switch {
        SocketsHttpHandler s => s,
        DelegatingHandler d  => FindDescendantSocketsHandler(d.InnerHandler),
        HttpClientHandler h  => SocketsHttpHandlerField.Value?.GetValue(h) as SocketsHttpHandler,
        _                    => null
    };

    internal class WireLoggingStream(Stream httpStream, Config config, HttpRequestMessage firstRequest): Stream {

        private static readonly Encoding Encoding = Encoding.UTF8;

        private static ulong _mostRecentRequestId;

        private readonly Stream requestBuffer = new MemoryStream(), responseBuffer = new MemoryStream();
        // private readonly SemaphoreSlim logWritten    = new(1);

        private ulong requestId;
        private bool  isNewRequest = true;

        /**
         * Sending a request
         */
        public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default) {
            // await logWritten.WaitAsync(cancellationToken).ConfigureAwait(false);
            // try {
            if (isNewRequest) {
                isNewRequest = false;

                if (responseBuffer.Length != 0) { // log previously buffered response from a different request
                    if (config.IsLogEnabled()) {
                        TrimTrailingLineEndings(responseBuffer);
                        using StreamReader reader = new(responseBuffer, Encoding, leaveOpen: true);
                        config.LogResponseReceived(reader.ReadToEnd(), requestId);
                    }
                    responseBuffer.SetLength(0);
                }

                requestId = Interlocked.Increment(ref _mostRecentRequestId);
            }

            requestBuffer.Write(buffer.Span);

            await httpStream.WriteAsync(buffer, cancellationToken);
            // } finally {
            // logWritten.Release();
            // }
        }

        /**
         * Receiving a response
         */
        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) {
            // await logWritten.WaitAsync(0, cancellationToken).ConfigureAwait(false);
            if (requestBuffer.Length != 0) { // log previous buffered request for this response
                if (config.IsLogEnabled()) {
                    TrimTrailingLineEndings(requestBuffer);
                    using StreamReader reader = new(requestBuffer, Encoding, leaveOpen: true);
                    config.LogRequestTransmitted(reader.ReadToEnd(), requestId);
                }
                requestBuffer.SetLength(0);
            }

            int bytesRead = await httpStream.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
            isNewRequest = true;
            responseBuffer.Write(buffer.Span[..bytesRead]);

            // if (bytesRead < buffer.Length && responseBuffer.Length != 0) { // log response because this is the last chunk
            //     if (config.IsLogEnabled()) {
            //         TrimTrailingLineEndings(responseBuffer);
            //         using StreamReader reader = new(responseBuffer, Encoding, leaveOpen: true);
            //         config.LogResponseReceived(reader.ReadToEnd(), requestId);
            //     }
            //     responseBuffer.SetLength(0);
            // }

            return bytesRead;
        }

        public override void Close() {
            httpStream.Close();
            OnResponseFinished();
        }

        public void OnResponseFinished() {
            if (responseBuffer.Length != 0) { // log response because this is the last chunk
                if (config.IsLogEnabled()) {
                    TrimTrailingLineEndings(responseBuffer);
                    using StreamReader reader = new(responseBuffer, Encoding, leaveOpen: true);
                    config.LogResponseReceived(reader.ReadToEnd(), requestId);
                }
                responseBuffer.SetLength(0);
            }
            // logWritten.Release();
        }

        internal static void TrimTrailingLineEndings(Stream stream) {
            stream.Position = Math.Max(0, stream.Length - 1);
            long newLength = stream.Length;
            while (stream.ReadByte() is '\r' or '\n') {
                newLength--;
                if (stream.Position < 2) {
                    break;
                }
                stream.Position -= 2;
            }
            stream.SetLength(Math.Max(newLength, 0));
            stream.Position = 0;
        }

        #region Delegated

        public override void Flush() => httpStream.Flush();
        public override int Read(byte[] buffer, int offset, int count) => httpStream.Read(buffer, offset, count);
        public override long Seek(long offset, SeekOrigin origin) => httpStream.Seek(offset, origin);
        public override void SetLength(long value) => httpStream.SetLength(value);
        public override void Write(byte[] buffer, int offset, int count) => httpStream.Write(buffer, offset, count);

        public override bool CanRead => httpStream.CanRead;
        public override bool CanSeek => httpStream.CanSeek;
        public override bool CanTimeout => httpStream.CanTimeout;
        public override bool CanWrite => httpStream.CanWrite;
        public override long Length => httpStream.Length;
        public override long Position {
            get => httpStream.Position;
            set => httpStream.Position = value;
        }
        public override int ReadTimeout {
            get => httpStream.ReadTimeout;
            set => httpStream.ReadTimeout = value;
        }
        public override int WriteTimeout {
            get => httpStream.WriteTimeout;
            set => httpStream.WriteTimeout = value;
        }

        protected override void Dispose(bool disposing) {
            if (disposing) {
                httpStream.Dispose();
                requestBuffer.Dispose();
                responseBuffer.Dispose();
            }
            base.Dispose(disposing);
        }

        protected virtual async ValueTask DisposeAsyncCore() {
            await httpStream.DisposeAsync().ConfigureAwait(false);
            await requestBuffer.DisposeAsync().ConfigureAwait(false);
            await responseBuffer.DisposeAsync().ConfigureAwait(false);
        }

        public sealed override async ValueTask DisposeAsync() {
            await DisposeAsyncCore().ConfigureAwait(false);
            await base.DisposeAsync().ConfigureAwait(false);
            GC.SuppressFinalize(this);
        }

        #endregion

    }
#else
    public ValueTask OnBeforeRequest(IUnfuckedHttpHandler handler) {
        return new ValueTask();
    }

#endif

}