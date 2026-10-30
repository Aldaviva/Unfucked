using AngleSharp;
using AngleSharp.Dom;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Unfucked.HTTP.Config;

namespace Unfucked.HTTP.Serialization;

public sealed class HtmlDocumentReader: MessageBodyReader, IDisposable {

    private readonly Lazy<IBrowsingContext> browser = new(() => BrowsingContext.New(Configuration.Default), LazyThreadSafetyMode.PublicationOnly);

    public bool CanRead<T>(MediaTypeHeaderValue? contentType, string? bodyPrefix) => typeof(T) == typeof(IDocument);

    public async Task<T> Read<T>(Stream responseBody, HttpStatusCode status, HttpResponseHeaders headers, HttpContentHeaders bodyHeaders, HttpRequestMessage request, IDisposable response,
                                 Encoding? responseEncoding,
                                 Configurable? clientConfig,
                                 CancellationToken cancellationToken) {
        return (T) await browser.Value.OpenAsync(virtualResponse => {
            if (request.RequestUri is {} uri) {
                virtualResponse.Address(uri);
            }

            virtualResponse
                .Status(status)
                .Headers(headers.Concat(bodyHeaders).ToDictionary(pair => pair.Key, pair => pair.Value.Join(", ")))
                .Content(responseBody, true);
        }, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public void Dispose() => browser.TryDisposeValue();

}