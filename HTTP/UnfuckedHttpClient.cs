using System.Net.Http.Headers;
using System.Reflection;
using Unfucked.HTTP.Config;
using Unfucked.HTTP.Exceptions;
#if NET8_0_OR_GREATER
using Unfucked.HTTP.Filters;
#endif

namespace Unfucked.HTTP;

public interface IUnfuckedHttpClient {

    IUnfuckedHttpHandler Handler { get; }

    Task<HttpResponseMessage> SendAsync(HttpRequest request, CancellationToken cancellationToken = default);

}

public class UnfuckedHttpClient: HttpClient, IUnfuckedHttpClient {

    private static readonly TimeSpan DefaultTimeout = new(0, 0, 30);

    public IUnfuckedHttpHandler Handler { get; }

    public UnfuckedHttpClient(HttpMessageHandler? handler = null, bool disposeHandler = true, IClientConfig? configuration = null): this(
        handler as UnfuckedHttpHandler ?? new UnfuckedHttpHandler(handler, configuration), disposeHandler) { }

    public UnfuckedHttpClient(IUnfuckedHttpHandler unfuckedHandler, bool disposeHandler = true): base(unfuckedHandler as HttpMessageHandler ?? new IUnfuckedHttpHandlerWrapper(unfuckedHandler),
        disposeHandler) {
        Handler = unfuckedHandler;
        Timeout = DefaultTimeout;
        if (Assembly.GetEntryAssembly()?.GetName() is { Name: { } programName, Version: { } programVersion }) {
            DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue(programName, programVersion.ToString(4, true)));
        }
        UnfuckedHttpHandler.CacheClientHandler(this, unfuckedHandler);
    }

    public UnfuckedHttpClient(HttpClient toClone, bool disposeHandler = true, IClientConfig? configuration = null): this(
        toClone is UnfuckedHttpClient u ? u.Handler : new UnfuckedHttpHandler(toClone, configuration), disposeHandler) {
        BaseAddress                  = toClone.BaseAddress;
        Timeout                      = toClone.Timeout;
        MaxResponseContentBufferSize = toClone.MaxResponseContentBufferSize;
#if NETCOREAPP3_0_OR_GREATER
        DefaultRequestVersion = toClone.DefaultRequestVersion;
        DefaultVersionPolicy  = toClone.DefaultVersionPolicy;
#endif
        foreach (KeyValuePair<string, IEnumerable<string>> wrappedDefaultHeader in toClone.DefaultRequestHeaders) {
            DefaultRequestHeaders.Add(wrappedDefaultHeader.Key, wrappedDefaultHeader.Value);
        }
    }

    /// <exception cref="ProcessingException"></exception>
    public virtual Task<HttpResponseMessage> SendAsync(HttpRequest request, CancellationToken cancellationToken = default) {
#if NET8_0_OR_GREATER
        WireLoggingFilter.AsyncState.Value = new WireLoggingFilter.WireAsyncState();
#endif

        HttpRequestMessage req = new(request.Verb, request.Uri) {
            Content = request.Body
        };

        foreach (KeyValuePair<string, string> header in request.Headers) {
            req.Headers.Add(header.Key, header.Value);
        }

        Task<HttpResponseMessage> sendAsync = SendAsync(req, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

        return SendInner();

        // Set wire logging AsyncLocal outside of this async method so it is available higher in the await chain when the response finishes
        async Task<HttpResponseMessage> SendInner() {
            try {
                return await sendAsync.ConfigureAwait(false);
            } catch (OperationCanceledException e) {
                // Official documentation is wrong: .NET Framework throws a TaskCanceledException for an HTTP request timeout, not an HttpRequestException (https://learn.microsoft.com/en-us/dotnet/api/system.net.http.httpclient.sendasync)
                TimeoutException cause = e.InnerException as TimeoutException ??
                    new TimeoutException($"The request was canceled due to the configured {nameof(HttpClient)}.{nameof(Timeout)} of {Timeout.TotalSeconds} seconds elapsing.");
                throw new ProcessingException(cause, CreateHttpExceptionParams(req));
            } catch (HttpRequestException e) {
                throw new ProcessingException(e.InnerException ?? e, CreateHttpExceptionParams(req));
            } finally {
                req.Dispose();
            }
        }
    }

    private static HttpExceptionParams CreateHttpExceptionParams(HttpRequestMessage request) => new(request.Method, request.RequestUri, null, null);

}