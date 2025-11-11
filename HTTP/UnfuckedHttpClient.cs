using System.Net.Http.Headers;
using System.Reflection;
using Unfucked.HTTP.Config;
using Unfucked.HTTP.Exceptions;
#if NET8_0_OR_GREATER
using Unfucked.HTTP.Filters;
#endif

namespace Unfucked.HTTP;

/// <summary>
/// Interface for <see cref="UnfuckedHttpClient"/>, an improved subclass of <see cref="HttpClient"/>.
/// </summary>
public interface IHttpClient: IDisposable {

    /// <summary>
    /// The HTTP message handler, such as an <see cref="UnfuckedHttpHandler"/>.
    /// </summary>
    IUnfuckedHttpHandler? Handler { get; }

    /// <summary>
    /// <para>Send an HTTP request using a nice parameterized options struct.</para>
    /// <para>Generally, this is called internally by the <see cref="IWebTarget"/> builder, which is more fluent (<c>HttpClient.Target(url).Get&lt;string&gt;()</c>, for example).</para>
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
public class UnfuckedHttpClient: HttpClient, IHttpClient {

    private static readonly TimeSpan DEFAULT_TIMEOUT = new(0, 0, 30);

    private static readonly Lazy<(string name, Version version)?> USER_AGENT = new(() => Assembly.GetEntryAssembly()?.GetName() is { Name: {} programName, Version: {} programVersion }
        ? (programName, programVersion) : null, LazyThreadSafetyMode.PublicationOnly);

    /// <inheritdoc />
    public IUnfuckedHttpHandler? Handler { get; }

    /// <summary>
    /// <para>Create a new <see cref="UnfuckedHttpClient"/> with a default message handler and configuration.</para>
    /// <para>Includes a default 30 second response timeout, 10 second connect timeout, 1 hour connection pool lifetime, and user-agent header named after your program.</para>
    /// </summary>
    public UnfuckedHttpClient(): this((IUnfuckedHttpHandler) new UnfuckedHttpHandler()) {}

    // This is not a factory method because it lets us both pass a SocketsHttpHandler with custom properties like PooledConnectionLifetime, as well as init properties on the UnfuckedHttpClient like Timeout. If this were a factory method, init property accessors would not be available, and callers would have to set them later on a temporary variable which can't all fit in one expression.
    /// <summary>
    /// Create a new <see cref="UnfuckedHttpClient"/> instance with the given handler.
    /// </summary>
    /// <param name="handler">An <see cref="HttpMessageHandler"/> used to send requests, typically a <see cref="SocketsHttpHandler"/> with custom properties.</param>
    /// <param name="disposeHandler"><c>true</c> to dispose of <paramref name="handler"/> when this instance is disposed, or <c>false</c> to not dispose it.</param>
    public UnfuckedHttpClient(HttpMessageHandler handler, bool disposeHandler = true): this(handler as IUnfuckedHttpHandler ?? new UnfuckedHttpHandler(handler), disposeHandler) {}

    /// <summary>
    /// Create a new <see cref="UnfuckedHttpClient"/> instance with a new handler and the given <paramref name="configuration"/>.
    /// </summary>
    /// <param name="configuration">Properties, filters, and message body readers to use in the new instance.</param>
    public UnfuckedHttpClient(IClientConfig configuration): this((IUnfuckedHttpHandler) new UnfuckedHttpHandler(null, configuration)) {}

    /// <summary>
    /// Main constructor that other constructors and factory methods delegate to.
    /// </summary>
    /// <param name="unfuckedHandler"></param>
    /// <param name="disposeHandler"></param>
    protected UnfuckedHttpClient(IUnfuckedHttpHandler unfuckedHandler, bool disposeHandler = true): base(unfuckedHandler as HttpMessageHandler ?? new IUnfuckedHttpHandlerWrapper(unfuckedHandler),
        disposeHandler) {
        Handler = unfuckedHandler;
        Timeout = DEFAULT_TIMEOUT;
        if (USER_AGENT.Value is var (programName, programVersion)) {
            DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue(programName, programVersion.ToString(4, true)));
        }
        UnfuckedHttpHandler.CacheClientHandler(this, unfuckedHandler);
    }

    /// <summary>
    /// Create a new <see cref="UnfuckedHttpClient"/> instance that uses the given <paramref name="unfuckedHandler"/> to send requests. This is mostly useful for testing where <paramref name="unfuckedHandler"/> is a mock. When it's a real <see cref="UnfuckedHttpHandler"/>, you can use the <see cref="UnfuckedHttpClient(HttpMessageHandler,bool)"/> constructor instead.
    /// </summary>
    /// <param name="unfuckedHandler">Handler used to send requests.</param>
    /// <param name="disposeHandler"><c>true</c> to dispose of <paramref name="unfuckedHandler"/> when this instance is disposed, or <c>false</c> to not dispose it.</param>
    /// <returns></returns>
    public static UnfuckedHttpClient Create(IUnfuckedHttpHandler unfuckedHandler, bool disposeHandler = true) => new(unfuckedHandler, disposeHandler);

    // This factory method is no longer constructors so DI gets less confused by the arguments, even though many are optional, to prevent it trying to inject a real HttpMessageHandler in a symmetric dependency. Microsoft.Extensions.DependencyInjection always picks the constructor overload with the most injectable arguments, but I want it to pick the no-arg constructor.
    /// <summary>
    /// Create a new <see cref="UnfuckedHttpClient"/> that copies the settings of an existing <see cref="HttpClient"/>.
    /// </summary>
    /// <param name="toClone"><see cref="HttpClient"/> to copy.</param>
    /// <returns>A new instance of an <see cref="UnfuckedHttpClient"/> with the same handler and configuration as <paramref name="toClone"/>.</returns>
    // ExceptionAdjustment: M:System.Net.Http.Headers.HttpHeaders.Add(System.String,System.Collections.Generic.IEnumerable{System.String}) -T:System.FormatException
    public static UnfuckedHttpClient Create(HttpClient toClone) {
        IUnfuckedHttpHandler newHandler;
        bool                 disposeHandler;
        if (toClone is UnfuckedHttpClient { Handler: {} h }) {
            newHandler     = h;
            disposeHandler = false; // we don't own it, toClone does
        } else {
            newHandler     = UnfuckedHttpHandler.Create(toClone);
            disposeHandler = true; // we own it, although it won't dispose toClone's inner handler because it wasn't created by the new UnfuckedHttpHandler
        }

        UnfuckedHttpClient newClient = new(newHandler, disposeHandler) {
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
        WireLogFilter.ASYNC_STATE.Value = new WireLogFilter.WireAsyncState();
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

internal class HttpClientWrapper: IHttpClient {

    private readonly HttpClient realClient;

    public IUnfuckedHttpHandler? Handler { get; }

    private HttpClientWrapper(HttpClient realClient) {
        this.realClient = realClient;
        Handler         = UnfuckedHttpHandler.FindHandler(realClient);
    }

    public static IHttpClient Wrap(IHttpClient client) => client is HttpClient httpClient and not UnfuckedHttpClient ? new HttpClientWrapper(httpClient) : client;
    public static IHttpClient Wrap(HttpClient client) => client as UnfuckedHttpClient as IHttpClient ?? new HttpClientWrapper(client);

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