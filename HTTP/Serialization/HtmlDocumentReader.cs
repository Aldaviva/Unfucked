using AngleSharp;
using AngleSharp.Dom;
using AngleSharp.Io;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Unfucked.Caching;
using Unfucked.HTTP.Config;

namespace Unfucked.HTTP.Serialization;

public sealed class HtmlDocumentReader: MessageBodyReader {

    private static readonly InMemoryCache<string, IBrowsingContext> BROWSERS_BY_USER_AGENT = new(new CacheOptions { ExpireAfterRead = new TimeSpan(15) }, (userAgent, _) =>
        new ValueTask<IBrowsingContext>(BrowsingContext.New(Configuration.Default.With(new DefaultHttpRequester(userAgent)).WithDefaultLoader())));

    static HtmlDocumentReader() {
        BROWSERS_BY_USER_AGENT.Removal += (_, _, value, _) => value.Dispose();
    }

    public bool CanRead<T>(MediaTypeHeaderValue? contentType, string? bodyPrefix) => typeof(T) == typeof(IDocument);

    public async Task<T> Read<T>(Stream responseBody, HttpStatusCode status, HttpResponseHeaders headers, HttpContentHeaders bodyHeaders, HttpRequestMessage request, IDisposable response,
                                 Encoding? responseEncoding, Configurable? clientConfig, CancellationToken cancellationToken) {
        IBrowsingContext browser = await BROWSERS_BY_USER_AGENT.Get(request.Headers.UserAgent.ToString(), cancellationToken: cancellationToken).ConfigureAwait(false);

        return (T) await browser.OpenAsync(virtualResponse => {
            if (request.RequestUri is {} uri) {
                virtualResponse.Address(uri);
            }

            virtualResponse
                .Status(status)
                .Headers(headers.Concat(bodyHeaders).ToDictionary(pair => pair.Key, pair => pair.Value.Join(", ")))
                .Content(responseBody, true);
        }, cancellationToken).ConfigureAwait(false);
    }

}