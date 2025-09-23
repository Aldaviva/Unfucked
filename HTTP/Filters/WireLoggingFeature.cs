using Unfucked.HTTP.Config;
#if NET5_0_OR_GREATER
using System.Reflection;
using System.Text;
#endif

namespace Unfucked.HTTP.Filters;

public class WireLoggingFeature(WireLoggingFeature.Config config): Feature {

    public record Config {

        public delegate void LogWriter(string message, ulong requestId);

        public Func<bool> IsLogEnabled { get; init; } = () => true;
        public required LogWriter LogRequestTransmitted { get; init; }
        public required LogWriter LogResponseReceived { get; init; }
        public bool MergeChunks { get; init; } = false;

    }

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
                        ? async (ctx, ct) => new WireLoggingStream(await existingStreamProvider(ctx, ct).ConfigureAwait(false), config)
                        : (ctx, _) => ValueTask.FromResult<Stream>(new WireLoggingStream(ctx.PlaintextStream, config));
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

    internal class WireLoggingStream(Stream httpStream, Config config): Stream {

        private static readonly Encoding Encoding        = Encoding.UTF8;
        private static readonly byte[]   LineEndingsUtf8 = "\r\n"u8.ToArray();
        private static readonly char[]   LineEndings     = ['\r', '\n'];

        private static ulong _mostRecentRequestId;

        private readonly MemoryStream? requestBuffer  = config.MergeChunks ? new MemoryStream() : null;
        private readonly MemoryStream? responseBuffer = config.MergeChunks ? new MemoryStream() : null;

        private ulong requestId;
        private bool  isNewRequest = true;

        /**
         * Sending a request
         */
        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default) {
            if (isNewRequest) {
                isNewRequest = false;

                if (config.IsLogEnabled() && responseBuffer is { Length: not 0 }) { // log previously buffered response from a different request
                    responseBuffer.Position = 0;
                    using StreamReader reader = new(responseBuffer, Encoding, leaveOpen: true);
                    config.LogResponseReceived(reader.ReadToEnd().TrimEnd(LineEndings), requestId);
                    responseBuffer.SetLength(0);
                }

                requestId = Interlocked.Increment(ref _mostRecentRequestId);
            }

            if (requestBuffer != null) {
                requestBuffer.Write(buffer.Span);
            } else if (config.IsLogEnabled()) {
                config.LogRequestTransmitted(Encoding.GetString(buffer.Span.TrimEnd(LineEndingsUtf8)), requestId);
            }

            return httpStream.WriteAsync(buffer, cancellationToken);
        }

        /**
         * Receiving a response
         */
        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) {
            if (requestBuffer is { Length: not 0 }) { // log previous buffered request for this response
                if (config.IsLogEnabled()) {
                    requestBuffer.Position = 0;
                    using StreamReader reader = new(requestBuffer, Encoding, leaveOpen: true);
                    config.LogRequestTransmitted(reader.ReadToEnd().TrimEnd(LineEndings), requestId);
                }
                requestBuffer.SetLength(0);
            }

            int bytesRead = await httpStream.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
            isNewRequest = true;

            if (responseBuffer != null) {
                responseBuffer.Write(buffer.Span[..bytesRead]);
                if (bytesRead < buffer.Length && responseBuffer.Length != 0) { // log response because this is the last chunk
                    if (config.IsLogEnabled()) {
                        responseBuffer.Position = 0;
                        using StreamReader reader = new(responseBuffer, Encoding, leaveOpen: true);
                        config.LogResponseReceived(reader.ReadToEnd().TrimEnd(LineEndings), requestId);
                    }
                    responseBuffer.SetLength(0);
                }
            } else if (config.IsLogEnabled()) {
                config.LogResponseReceived(Encoding.GetString(buffer.Span[..bytesRead].TrimEnd(LineEndingsUtf8)), requestId);
            }

            return bytesRead;
        }

        #region Delegated

        public override void Close() => httpStream.Close();
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
                requestBuffer?.Dispose();
                responseBuffer?.Dispose();
                Console.WriteLine("disposed wireloggingstream");
            }
            base.Dispose(disposing);
        }

        protected virtual async ValueTask DisposeAsyncCore() {
            await httpStream.DisposeAsync().ConfigureAwait(false);
            if (requestBuffer != null) {
                await requestBuffer.DisposeAsync().ConfigureAwait(false);
            }
            if (responseBuffer != null) {
                await responseBuffer.DisposeAsync().ConfigureAwait(false);
            }
            Console.WriteLine("disposed wireloggingstream");
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