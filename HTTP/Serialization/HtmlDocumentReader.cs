using AngleSharp;
using AngleSharp.Dom;
using AngleSharp.Io;
using System.Net.Http.Headers;
using System.Text;
using Unfucked.Caching;
using Unfucked.HTTP.Config;

namespace Unfucked.HTTP.Serialization;

public sealed class HtmlDocumentReader: AbstractMessageBodyReader {

    private static readonly InMemoryCache<string, IBrowsingContext> BROWSERS_BY_USER_AGENT = new(new CacheOptions { ExpireAfterRead = new TimeSpan(15) }, (userAgent, _) =>
        new ValueTask<IBrowsingContext>(BrowsingContext.New(Configuration.Default.With(new DefaultHttpRequester(userAgent)).WithDefaultLoader())));

    static HtmlDocumentReader() {
        BROWSERS_BY_USER_AGENT.Removal += (_, _, value, _) => value.Dispose();
    }

    public override bool CanRead<T>(MediaTypeHeaderValue? contentType) => typeof(T) == typeof(IDocument);

    public override async Task<T> Read<T>(HttpResponseMessage response, Encoding? responseEncoding, Configurable? clientConfig, CancellationToken cancellationToken) {
        IBrowsingContext browser = await BROWSERS_BY_USER_AGENT.Get(response.RequestMessage?.Headers.UserAgent.ToString() ?? string.Empty, cancellationToken: cancellationToken).ConfigureAwait(false);

        Stream bodyStream = await GetBodyStream(response, cancellationToken);

        return (T) await browser.OpenAsync(virtualResponse => {
            if (response.RequestMessage?.RequestUri is {} uri) {
                virtualResponse.Address(uri);
            }

            virtualResponse
                .Status(response.StatusCode)
                .Headers(response.Headers.Concat(response.Content.Headers).ToDictionary(pair => pair.Key, pair => pair.Value.Join(", ")))
                .Content(bodyStream, true);
        }, cancellationToken).ConfigureAwait(false);
    }

}