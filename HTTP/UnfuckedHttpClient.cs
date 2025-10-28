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
public interface IUnfuckedHttpClient: IDisposable {

    /// <summary>
    /// The HTTP message handler, such as an <see cref="UnfuckedHttpHandler"/>.
    /// </summary>
    IUnfuckedHttpHandler? Handler { get; }

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
    public IUnfuckedHttpHandler? Handler { get; }

    public UnfuckedHttpClient(): this(new UnfuckedHttpHandler()) { }

    // These factory methods are no longer constructors so DI gets less confused by the arguments, even though many are optional, to prevent it trying to inject a real HttpMessageHandler in a symmetric dependency. Microsoft.Extensions.DependencyInjection always picks the constructor overload with the most injectable arguments, but I want it to pick the no-arg constructor.
    public static UnfuckedHttpClient Create(HttpMessageHandler? handler = null, bool disposeHandler = true, IClientConfig? configuration = null) =>
        new(handler as UnfuckedHttpHandler ?? new UnfuckedHttpHandler(handler, configuration), disposeHandler);

    public static UnfuckedHttpClient Create(IUnfuckedHttpHandler unfuckedHandler, bool disposeHandler = true) => new(unfuckedHandler, disposeHandler);

    protected UnfuckedHttpClient(IUnfuckedHttpHandler unfuckedHandler, bool disposeHandler = true): base(unfuckedHandler as HttpMessageHandler ?? new IUnfuckedHttpHandlerWrapper(unfuckedHandler),
        disposeHandler) {
        Handler = unfuckedHandler;
        Timeout = DefaultTimeout;
        if (Assembly.GetEntryAssembly()?.GetName() is { Name: { } programName, Version: { } programVersion }) {
            DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue(programName, programVersion.ToString(4, true)));
        }
        UnfuckedHttpHandler.CacheClientHandler(this, unfuckedHandler);
    }

    // ExceptionAdjustment: M:System.Net.Http.Headers.HttpHeaders.Add(System.String,System.Collections.Generic.IEnumerable{System.String}) -T:System.FormatException
    public static UnfuckedHttpClient Create(HttpClient toClone, bool disposeHandler = true, IClientConfig? configuration = null) {
        UnfuckedHttpClient newClient = new(toClone is UnfuckedHttpClient { Handler: { } h } ? h : new UnfuckedHttpHandler(toClone, configuration), disposeHandler) {
            BaseAddress                  = toClone.BaseAddress,
            Timeout                      = toClone.Timeout,
            MaxResponseContentBufferSize = toClone.MaxResponseContentBufferSize,
#if NETCOREAPP3_0_OR_GREATER
            DefaultRequestVersion = toClone.DefaultRequestVersion,
            DefaultVersionPolicy  = toClone.DefaultVersionPolicy
#endif
        };

        foreach (KeyValuePair<string, IEnumerable<string>> wrappedDefaultHeader in toClone.DefaultRequestHeaders) {
            newClient.DefaultRequestHeaders.Add(wrappedDefaultHeader.Key, wrappedDefaultHeader.Value);
        }

        return newClient;
    }

    /// <inheritdoc />
    public virtual Task<HttpResponseMessage> SendAsync(HttpRequest request, CancellationToken cancellationToken = default) {
#if NET8_0_OR_GREATER
        WireLogFilter.AsyncState.Value = new WireLogFilter.WireAsyncState();
#endif

        UnfuckedHttpRequestMessage req = new(request.Verb, request.Uri) {
            Content = request.Body,
            Config  = request.ClientConfig
        };
        try {
            foreach (KeyValuePair<string, string> header in request.Headers) {
                req.Headers.Add(header.Key, header.Value);
            }
        } catch (FormatException e) {
            throw new ProcessingException(e, HttpExceptionParams.FromRequest(req));
        }

        // Set wire logging AsyncLocal outside of this async method so it is available higher in the await chain when the response finishes
        return SendAsync(this, req, cancellationToken);
    }

    /// <exception cref="ProcessingException"></exception>
    internal static async Task<HttpResponseMessage> SendAsync(HttpClient client, UnfuckedHttpRequestMessage request, CancellationToken cancellationToken) {
        try {
            return await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
        } catch (OperationCanceledException e) {
            // Official documentation is wrong: .NET Framework throws a TaskCanceledException for an HTTP request timeout, not an HttpRequestException (https://learn.microsoft.com/en-us/dotnet/api/system.net.http.httpclient.sendasync)
            TimeoutException cause = e.InnerException as TimeoutException ??
                new TimeoutException($"The request was canceled due to the configured {nameof(HttpClient)}.{nameof(Timeout)} of {client.Timeout.TotalSeconds} seconds elapsing.");
            throw new ProcessingException(cause, HttpExceptionParams.FromRequest(request));
        } catch (HttpRequestException e) {
            throw new ProcessingException(e.InnerException ?? e, HttpExceptionParams.FromRequest(request));
        } finally {
            request.Dispose();
        }
    }

}

internal class HttpClientWrapper: IUnfuckedHttpClient {

    private readonly HttpClient realClient;

    public IUnfuckedHttpHandler? Handler { get; }

    private HttpClientWrapper(HttpClient realClient) {
        this.realClient = realClient;
        Handler         = UnfuckedHttpHandler.FindHandler(realClient);
    }

    public static IUnfuckedHttpClient Wrap(IUnfuckedHttpClient client) => client is HttpClient httpClient and not UnfuckedHttpClient ? new HttpClientWrapper(httpClient) : client;
    public static IUnfuckedHttpClient Wrap(HttpClient client) => client as UnfuckedHttpClient as IUnfuckedHttpClient ?? new HttpClientWrapper(client);

    /// <exception cref="ProcessingException"></exception>
    public Task<HttpResponseMessage> SendAsync(HttpRequest request, CancellationToken cancellationToken = default) {
        using UnfuckedHttpRequestMessage req = new(request);

        try {
            foreach (IGrouping<string, string> header in request.Headers.GroupBy(pair => pair.Key, pair => pair.Value, StringComparer.OrdinalIgnoreCase)) {
                req.Headers.Add(header.Key, header);
            }
        } catch (FormatException e) {
            throw new ProcessingException(e, HttpExceptionParams.FromRequest(req));
        }
        return UnfuckedHttpClient.SendAsync(realClient, new UnfuckedHttpRequestMessage(request), cancellationToken);
    }

    public void Dispose() {
        GC.SuppressFinalize(this);
    }

}