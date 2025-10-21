using System.Net.Http.Headers;
using System.Reflection;
using Unfucked.HTTP.Config;
using Unfucked.HTTP.Exceptions;
#if NET8_0_OR_GREATER
using Unfucked.HTTP.Filters;
#endif

namespace Unfucked.HTTP;

/// <summary>
/// Interface for <see cref="UnfuckedHttpClient"/>, an improved subclass of <see cref="HttpClient"/>
/// </summary>
public interface IUnfuckedHttpClient {

    /// <summary>
    /// The HTTP message handler, such as an <see cref="UnfuckedHttpHandler"/>.
    /// </summary>
    IUnfuckedHttpHandler Handler { get; }

    /// <summary>
    /// <para>Send an HTTP request using a nice parameterized options struct.</para>
    /// <para>Generally, this is called internally by the <see cref="WebTarget"/> builder, which is more fluent (<c>HttpClient.Target(url).Get&lt;string&gt;()</c>, for example).</para>
    /// </summary>
    /// <param name="request">the HTTP verb, URL, headers, and optional body to send</param>
    /// <param name="cancellationToken">cancel the request</param>
    /// <returns>HTTP response, after the response headers only are read</returns>
    /// <exception cref="ProcessingException">the HTTP request timed out (<see cref="TimeoutException"/>) or threw an <see cref="HttpRequestException"/></exception>
    Task<HttpResponseMessage> SendAsync(HttpRequest request, CancellationToken cancellationToken = default);

}

/// <summary>
/// <para>An improved subclass of <see cref="HttpClient"/>.</para>
/// <para>Usage:</para>
/// <para><c>using HttpClient client = new UnfuckedHttpClient();
/// MyObject response = await client.Target(url).Get&lt;MyObject&gt;();</c></para>
/// </summary>
public class UnfuckedHttpClient: HttpClient, IUnfuckedHttpClient {

    private static readonly TimeSpan DefaultTimeout = new(0, 0, 30);

    /// <inheritdoc />
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

    // ExceptionAdjustment: M:System.Net.Http.Headers.HttpHeaders.Add(System.String,System.Collections.Generic.IEnumerable{System.String}) -T:System.FormatException
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

    /// <inheritdoc />
    public virtual Task<HttpResponseMessage> SendAsync(HttpRequest request, CancellationToken cancellationToken = default) {
#if NET8_0_OR_GREATER
        WireLogFilter.AsyncState.Value = new WireLogFilter.WireAsyncState();
#endif

        HttpRequestMessage req = new(request.Verb, request.Uri) {
            Content = request.Body
        };

        try {
            foreach (KeyValuePair<string, string> header in request.Headers) {
                req.Headers.Add(header.Key, header.Value);
            }
        } catch (FormatException e) {
            throw new ProcessingException(e, new HttpExceptionParams(req));
        }

        // Set wire logging AsyncLocal outside of this async method so it is available higher in the await chain when the response finishes
        return SendAsync(this, req, cancellationToken);
    }

    /// <exception cref="ProcessingException"></exception>
    internal static async Task<HttpResponseMessage> SendAsync(HttpClient client, HttpRequestMessage request, CancellationToken cancellationToken) {
        try {
            return await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
        } catch (OperationCanceledException e) {
            // Official documentation is wrong: .NET Framework throws a TaskCanceledException for an HTTP request timeout, not an HttpRequestException (https://learn.microsoft.com/en-us/dotnet/api/system.net.http.httpclient.sendasync)
            TimeoutException cause = e.InnerException as TimeoutException ??
                new TimeoutException($"The request was canceled due to the configured {nameof(HttpClient)}.{nameof(Timeout)} of {client.Timeout.TotalSeconds} seconds elapsing.");
            throw new ProcessingException(cause, new HttpExceptionParams(request));
        } catch (HttpRequestException e) {
            throw new ProcessingException(e.InnerException ?? e, new HttpExceptionParams(request));
        } finally {
            request.Dispose();
        }
    }

}