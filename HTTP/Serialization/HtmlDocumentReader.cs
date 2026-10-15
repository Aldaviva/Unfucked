using AngleSharp;
using AngleSharp.Dom;
using System.Text;
using Unfucked.HTTP.Config;

namespace Unfucked.HTTP.Serialization;

public sealed class HtmlDocumentReader: MessageBodyReader, IDisposable {

    private readonly Lazy<IBrowsingContext> browser = new(() => BrowsingContext.New(Configuration.Default), LazyThreadSafetyMode.PublicationOnly);

    public bool CanRead<T>(string? mimeType, string? bodyPrefix) => typeof(T) == typeof(IDocument);

    public async Task<T> Read<T>(HttpResponseMessage response, Encoding? responseEncoding, Configurable? clientConfig, CancellationToken cancellationToken) {
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        await using
#else
        using
#endif
            Stream responseStream = await response.Content
#if NET5_0_OR_GREATER
                .ReadAsStreamAsync(cancellationToken)
#else
                .ReadAsStreamAsync()
#endif
                .ConfigureAwait(false);

        return (T) await browser.Value.OpenAsync(virtualResponse => {
            if (response.RequestMessage?.RequestUri is {} uri) {
                virtualResponse.Address(uri);
            }

            virtualResponse
                .Status(response.StatusCode)
                .Headers(response.Headers.Concat(response.Content.Headers).ToDictionary(pair => pair.Key, pair => pair.Value.Join(", ")))
                .Content(responseStream);
        }, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public void Dispose() => browser.TryDisposeValue();

}