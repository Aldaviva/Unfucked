#if NET8_0_OR_GREATER
using System.Diagnostics.Metrics;
using System.Reflection;
using System.Text;
using Unfucked.HTTP.Config;
#endif

namespace Unfucked.HTTP.Filters;

#pragma warning disable CS9113 // Parameter is unread. - it's read in other targets through conditional compilation
/// <summary>
/// <para>Log raw plaintext HTTP requests and responses to your logging solution.</para>
/// <para>Usage example:</para>
/// <para><code>NLog.Logger wireLogger = NLog.LogManager.GetLogger("wire");
/// using HttpClient client = new UnfuckedHttpClient()
///     .Register(new WireLogFilter(new WireLogFilter.WireConfig {
///         LogRequestTransmitted = (message, id) => wireLogger.Trace("{0} &gt;&gt; {1}", id, message),
///         LogRequestReceived    = (message, id) => wireLogger.Trace("{0} &lt;&lt; {1}", id, message),
///         IsLogEnabled          = () => wireLogger.IsTraceEnabled()
///     }));</code></para>
/// <para>Requirements: .NET ≥ 8, otherwise it is a no-op. Only tested with HTTP 1.1, which is the default for all versions of .NET through 10 so far. Must not be using a custom
/// <see cref="HttpMessageHandler"/> or <c>IMeterFactory</c>. Does not work with a plain <see cref="HttpClient"/> with an <see cref="UnfuckedHttpHandler"/>, you must use an
/// <see cref="UnfuckedHttpClient"/> (such as <c>new UnfuckedHttpClient()</c> or <c>new UnfuckedHttpHandler.CreateClient()</c>.</para>
/// </summary>
/// <param name="config">Configure how to log the messages.</param>
public class WireLogFilter(WireLogFilter.WireConfig config): ClientRequestFilter {

#pragma warning restore CS9113 // Parameter is unread.

    /// <summary>
    /// Configure the wire logging filter
    /// </summary>
    public record WireConfig {

        /// <summary>
        /// Callback that logs an HTTP request or response to any logging library.
        /// </summary>
        /// <param name="message">The HTTP request or response, including both headers and body (parsed as UTF-8).</param>
        /// <param name="requestId">Monotonically-increasing number (starting at 1) used to correlate responses to their requests.</param>
        public delegate void LogWriter(string message, ulong requestId);

        /// <summary>
        /// Whether or not the logger for HTTP messages is enabled at the level that <see cref="LogRequestTransmitted"/> and <see cref="LogResponseReceived"/> log at.
        /// </summary>
        public Func<bool> IsLogEnabled { get; init; } = () => true;

        /// <summary>
        /// When an HTTP request is sent, this callback should log it.
        /// </summary>
        public required LogWriter LogRequestTransmitted { get; init; }

        /// <summary>
        /// When an HTTP response is received, this callback should log it.
        /// </summary>
        public required LogWriter LogResponseReceived { get; init; }

        /// <summary>
        /// <c>true</c> (default) to combine chunked requests or responses into one log message, which is much easier to read and analyze, but uses more memory and can result in delayed logs for
        /// concurrent streamed responses, or <c>false</c> to log each chunk separately. Even if this is enabled, responses will still be logged separately from their requests.
        /// </summary>
        public bool ReassembleChunks { get; init; } = true;

        /// <summary>
        /// Optional limit on the size of a request or response that will be buffered and logged, in bytes. By default, this is 0, which means no limit. Values greater than 0 cause truncation when
        /// buffering HTTP messages.
        /// </summary>
        public long MaxMessageSize { get; init; } = 0;

    }

#if NET8_0_OR_GREATER
    private static readonly  PropertyKey<bool>          Activated      = new($"{typeof(WireLogFilter).Namespace!}.{nameof(WireLogFilter)}.{nameof(Activated)}");
    private static readonly  object                     ActivationLock = new();
    internal static readonly AsyncLocal<WireAsyncState> AsyncState     = new();

    private static readonly Lazy<FieldInfo?> SocketsHttpHandlerField = new(() => typeof(HttpClientHandler).GetFields(BindingFlags.NonPublic | BindingFlags.Instance)
        .FirstOrDefault(field => field.FieldType == typeof(SocketsHttpHandler)), LazyThreadSafetyMode.PublicationOnly);

    public Task<HttpRequestMessage> Filter(HttpRequestMessage request, FilterContext context, CancellationToken cancellationToken) {
        lock (ActivationLock) {
            if (!context.Handler.Property(Activated, out bool isActivated) || !isActivated) {
                if (FindDescendantSocketsHandler(context.Handler as UnfuckedHttpHandler) is { } socketsHttpHandler) {
                    socketsHttpHandler.PlaintextStreamFilter = socketsHttpHandler.PlaintextStreamFilter is { } existingStreamProvider
                        ? async (ctx, ct) => new WireLoggingStream(await existingStreamProvider(ctx, ct).ConfigureAwait(false), config)
                        : (ctx, _) => ValueTask.FromResult<Stream>(new WireLoggingStream(ctx.PlaintextStream, config));

                    if (!config.ReassembleChunks && socketsHttpHandler.MeterFactory is WireLoggingMeterFactory meterFactory) {
                        try {
                            socketsHttpHandler.MeterFactory = null;
                            meterFactory.Dispose();
                        } catch (InvalidOperationException) {
                            // handler has already sent a request, leave the meterfactory intact and don't worry about this minor performance improvement
                        }
                    }
                    context.Handler.Property(Activated, true);
                } else {
                    context.Handler.Property(Activated, false);
                }
            }
        }
        return Task.FromResult(request);
    }

    private static SocketsHttpHandler? FindDescendantSocketsHandler(HttpMessageHandler? parent) => parent switch {
        SocketsHttpHandler s => s,
        DelegatingHandler d  => FindDescendantSocketsHandler(d.InnerHandler),
        HttpClientHandler h  => SocketsHttpHandlerField.Value?.GetValue(h) as SocketsHttpHandler,
        _                    => null
    };

    internal class WireLoggingStream: Stream {

        private static readonly Encoding MessageEncoding = new UTF8Encoding(false, false); // MaxMessageSize might truncate trailing bytes of a multi-byte character
        private static readonly byte[]   LineEndingsUtf8 = "\r\n"u8.ToArray();

        private static ulong _mostRecentRequestId;

        private readonly Stream     httpStream;
        private readonly WireConfig config;
        private readonly Stream     requestBuffer;
        private readonly Stream     responseBuffer;

        private ulong requestId;
        private bool  isNewRequest                = true;
        private bool  isFinalResponseChunkWritten = true;
        private bool  warnedAboutClientClass;

        public WireLoggingStream(Stream httpStream, WireConfig config) {
            this.httpStream = httpStream;
            this.config     = config;

            requestBuffer  = config.ReassembleChunks ? new MemoryStream() : Null;
            responseBuffer = config.ReassembleChunks ? new MemoryStream() : Null;

            if (config.ReassembleChunks && AsyncState.Value != null) {
                AsyncState.Value.wireStream = this;
            }
        }

        /**
         * Sending a request
         */
        /// <exception cref="OperationCanceledException"></exception>
        public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default) {
            if (config.ReassembleChunks) {
                if (AsyncState.Value != null) {
                    AsyncState.Value.wireStream = this;
                }

                if (isNewRequest) {
                    isNewRequest                = false;
                    isFinalResponseChunkWritten = true;

                    if (responseBuffer.Length != 0) { // log previously buffered response from a different request
                        if (config.IsLogEnabled()) {
                            TrimTrailingLineEndings(responseBuffer);
                            using StreamReader reader = new(responseBuffer, MessageEncoding, leaveOpen: true);
                            // ReSharper disable once MethodHasAsyncOverloadWithCancellation - the source is already in memory because it's a MemoryStream, so there is no reason to use async or cancellations
                            config.LogResponseReceived(reader.ReadToEnd(), requestId);
                        }
                        responseBuffer.SetLength(0);
                    }

                    requestId = Interlocked.Increment(ref _mostRecentRequestId);
                }

                if (config.IsLogEnabled()) {
                    requestBuffer.Write(buffer.Span[..(config.MaxMessageSize > 0 ? (Index) (config.MaxMessageSize - requestBuffer.Length).Clip(0, buffer.Length) : ^0)]);
                }
            } else {
                requestId = Interlocked.Increment(ref _mostRecentRequestId);
                if (config.IsLogEnabled()) {
                    config.LogRequestTransmitted(MessageEncoding.GetString(buffer.Span[..(config.MaxMessageSize > 0 ? (Index) config.MaxMessageSize : ^0)].TrimEnd(LineEndingsUtf8)), requestId);
                }
            }

            await httpStream.WriteAsync(buffer, cancellationToken).ConfigureAwait(false);
        }

        /**
         * Receiving a response
         */
        /// <exception cref="OperationCanceledException"></exception>
        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) {
            int bytesRead = await httpStream.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);

            if (config.ReassembleChunks) {
                if (AsyncState.Value != null) {
                    AsyncState.Value.wireStream = this;
                } else if (!warnedAboutClientClass) {
                    warnedAboutClientClass = true;
                    config.LogResponseReceived(
                        $"Warning: Unable to detect the end of message for HTTP responses during wire logging. To fix this, create the {nameof(HttpClient)} using `new {nameof(UnfuckedHttpClient)}()` instead of `new {nameof(HttpClient)}(new {nameof(UnfuckedHttpHandler)}())`.",
                        0);
                }

                if (requestBuffer.Length != 0) { // log previous buffered request for this response
                    if (config.IsLogEnabled()) {
                        TrimTrailingLineEndings(requestBuffer);
                        using StreamReader reader = new(requestBuffer, MessageEncoding, leaveOpen: true);
                        // ReSharper disable once MethodHasAsyncOverloadWithCancellation - the source is already in memory because it's a MemoryStream, so there is no reason to use async or cancellations
                        config.LogRequestTransmitted(reader.ReadToEnd(), requestId);
                    }

                    requestBuffer.SetLength(0);
                }

                isNewRequest = true;
                if (config.IsLogEnabled()) {
                    responseBuffer.Write(buffer.Span[.. (Index) (config.MaxMessageSize > 0 ? (config.MaxMessageSize - requestBuffer.Length).Clip(0, bytesRead) : bytesRead)]);
                }

                isFinalResponseChunkWritten = false;
            } else if (config.IsLogEnabled()) {
                config.LogResponseReceived(
                    MessageEncoding.GetString(buffer.Span[.. (Index) (config.MaxMessageSize > 0 ? config.MaxMessageSize.Clip(0, bytesRead) : bytesRead)].TrimEnd(LineEndingsUtf8)), requestId);
            }

            return bytesRead;
        }

        public override void Close() {
            httpStream.Close();
            OnResponseFinished();
        }

        public void OnResponseFinished() {
            if (!isFinalResponseChunkWritten && config.ReassembleChunks) {
                isFinalResponseChunkWritten = true;
                if (responseBuffer.Length != 0) { // log response because this is the last chunk
                    if (config.IsLogEnabled()) {
                        TrimTrailingLineEndings(responseBuffer);
                        using StreamReader reader = new(responseBuffer, MessageEncoding, leaveOpen: true);
                        config.LogResponseReceived(reader.ReadToEnd(), requestId);
                    }

                    responseBuffer.SetLength(0);
                }
            }
        }

        internal static void TrimTrailingLineEndings(Stream stream) {
            stream.Position = Math.Max(0, stream.Length - 1);
            long newLength = stream.Length;
            while (stream.ReadByte() is '\r' or '\n') {
                newLength--;
                if (stream.Position < 2) break;
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

    internal class WireAsyncState {

        public WireLoggingStream? wireStream { get; set; }

    }

    internal class WireLoggingMeterFactory: IMeterFactory {

        private const string InstrumentName = "http.client.open_connections";
        private const string TagName        = "http.connection.state";

        private readonly MeterListener meterListener = new();

        public WireLoggingMeterFactory() {
            meterListener.SetMeasurementEventCallback<long>(OnMeasurement);
            meterListener.Start();
        }

        public Meter Create(MeterOptions options) {
            Meter myMeter = new(options);
            UpDownCounter<long> openConnectionsInstrument =
                myMeter.CreateUpDownCounter<long>(InstrumentName, "{connection}", "Number of outbound HTTP connections that are currently active or idle on the client.");
            meterListener.EnableMeasurementEvents(openConnectionsInstrument);
            return myMeter;
        }

        private static void OnMeasurement(Instrument instrument, long measurement, ReadOnlySpan<KeyValuePair<string, object?>> tags, object? state) {
            if (instrument.Name == InstrumentName && measurement == 1) {
                bool isIdleConnection = false;
                foreach (KeyValuePair<string, object?> tag in tags) {
                    if (tag.Key == TagName) {
                        isIdleConnection = tag.Value is "idle";
                        break;
                    }
                }

                if (isIdleConnection) {
                    AsyncState.Value?.wireStream?.OnResponseFinished();
                }
            }
        }

        public void Dispose() {
            meterListener.Dispose();
            GC.SuppressFinalize(this);
        }

    }
#else
    public Task<HttpRequestMessage> Filter(HttpRequestMessage request, FilterContext context, CancellationToken cancellationToken) => Task.FromResult(request);

#endif

}