﻿using System.Collections.Immutable;
using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Mime;
using System.Runtime.Serialization;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
#if NET6_0_OR_GREATER
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
#endif

namespace Unfucked.HTTP;

public class WebTarget {

    private const string ApplicationXmlMediaType = "application/xml";

    private static readonly HttpMethod PatchVerb = new("PATCH");

#if NET6_0_OR_GREATER
    private static readonly JsonSerializerOptions JsonSerializerOptions = new(JsonSerializerDefaults.Web);
#endif

    private ImmutableList<KeyValuePair<string, string>> Headers { get; init; } = ImmutableList<KeyValuePair<string, string>>.Empty;

    private readonly URIBuilder uriBuilder;
    private readonly HttpClient client;

    #region Construction

    public WebTarget(HttpClient client, URIBuilder uriBuilder) {
        this.client     = client;
        this.uriBuilder = uriBuilder;
    }

    public WebTarget(HttpClient client, Uri uri): this(client, new URIBuilder(uri)) { }
    public WebTarget(HttpClient client, string uri): this(client, new URIBuilder(uri)) { }
    public WebTarget(HttpClient client, UriBuilder uriBuilder): this(client, new URIBuilder(uriBuilder)) { }

    #endregion

    #region URIs

    private WebTarget With(URIBuilder newUriBuilder) => new(client, newUriBuilder) { Headers = Headers };
    public Uri Uri => uriBuilder.ToUri();

    public WebTarget UserInfo(string? userInfo) => With(uriBuilder.UserInfo(userInfo));
    public WebTarget Path(string? segments, bool autoSplit = true) => With(uriBuilder.Path(segments, autoSplit));
    public WebTarget Path(object segments) => With(uriBuilder.Path(segments));
    public WebTarget Path(params IEnumerable<string> segments) => With(uriBuilder.Path(segments));
    public WebTarget Port(ushort? port) => With(uriBuilder.Port(port));
    public WebTarget Hostname(string hostname) => With(uriBuilder.Hostname(hostname));
    public WebTarget Scheme(string scheme) => With(uriBuilder.Scheme(scheme));
    public WebTarget QueryParam(string key, object? value) => With(uriBuilder.QueryParam(key, value));
    public WebTarget QueryParam(string key, IEnumerable<object> values) => With(uriBuilder.QueryParam(key, values));
    public WebTarget QueryParam(IEnumerable<KeyValuePair<string, object>>? parameters) => With(uriBuilder.QueryParam(parameters));
    public WebTarget Fragment(string? fragment) => With(uriBuilder.Fragment(fragment));

    #endregion

    #region Headers

    public WebTarget Header(string key, object? value) => new(client, uriBuilder) {
        Headers = value is null
            ? Headers.RemoveAll(pair => pair.Key == key)
            : Headers.Add(new KeyValuePair<string, string>(key, value.ToString() ?? string.Empty))
    };

    public WebTarget Header(string key, params IEnumerable<object> values) => new(client, uriBuilder) {
        Headers = Headers.AddRange(values.Select(value => new KeyValuePair<string, string>(key, value.ToString() ?? string.Empty)))
    };

    public WebTarget Accept(params IEnumerable<string> mediaTypes) => Header(HttpHeaders.Accept, mediaTypes);
    public WebTarget Accept(params IEnumerable<MediaTypeHeaderValue> mediaTypes) => Accept(mediaTypes.Select(mediaType => mediaType.ToString()));

    public WebTarget AcceptEncoding(params IEnumerable<string> encodings) => Header(HttpHeaders.AcceptEncoding, encodings);

    public WebTarget AcceptLanguage(params IEnumerable<string> languages) => Header(HttpHeaders.AcceptLanguage, languages);
    public WebTarget AcceptLanguage(params IEnumerable<CultureInfo> languages) => AcceptLanguage(languages.Select(culture => culture.IetfLanguageTag));

    public WebTarget CacheControl(string cacheControl) => Header(HttpHeaders.CacheControl, cacheControl);
    public WebTarget CacheControl(CacheControlHeaderValue cacheControl) => CacheControl(cacheControl.ToString());

    public WebTarget Cookie(Cookie cookie) => Header(HttpHeaders.Cookie, cookie.ToString());
    public WebTarget Cookie(string key, string value) => Cookie(new Cookie(key, value));

    public WebTarget UserAgent(string userAgentString) => Header(HttpHeaders.UserAgent, userAgentString);
    public WebTarget UserAgent(ProductInfoHeaderValue userAgentString) => Header(HttpHeaders.UserAgent, userAgentString.ToString());

    public WebTarget Authorization(string credentials) => Header(HttpHeaders.Authorization, credentials);
    public WebTarget Authorization(string username, string password) => Authorization(Convert.ToBase64String(Strings.Utf8.GetBytes($"{username}:{password}"), Base64FormattingOptions.None));
    public WebTarget Authorization(NetworkCredential credentials) => Authorization(credentials.UserName, credentials.Password);

    public WebTarget Referrer(string referrer) => Header(HttpHeaders.Referrer, referrer);
    public WebTarget Referrer(Uri referrer) => Referrer(referrer.AbsoluteUri);

    public WebTarget RequestedWith(string requester = "XMLHttpRequest") => Header(HttpHeaders.XRequestedWith, requester);

    #endregion

    #region Sending

    public async Task<HttpResponseMessage> Send(HttpMethod verb, HttpContent? requestBody = null, CancellationToken cancellationToken = default) {
        using HttpRequestMessage request = new(verb, uriBuilder.ToUri()) { Content = requestBody };

        foreach (IGrouping<string, string> header in Headers.GroupBy(pair => pair.Key, pair => pair.Value, StringComparer.OrdinalIgnoreCase)) {
            request.Headers.Add(header.Key, header);
        }

        return await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
    }

    public Task<HttpResponseMessage> Get(CancellationToken cancellationToken = default) => Send(HttpMethod.Get, null, cancellationToken);
    public Task<HttpResponseMessage> Head(CancellationToken cancellationToken = default) => Send(HttpMethod.Head, null, cancellationToken);
    public Task<HttpResponseMessage> Post(HttpContent? requestBody, CancellationToken cancellationToken = default) => Send(HttpMethod.Post, requestBody, cancellationToken);
    public Task<HttpResponseMessage> Put(HttpContent? requestBody, CancellationToken cancellationToken = default) => Send(HttpMethod.Put, requestBody, cancellationToken);
    public Task<HttpResponseMessage> Patch(HttpContent? requestBody, CancellationToken cancellationToken = default) => Send(PatchVerb, requestBody, cancellationToken);
    public Task<HttpResponseMessage> Delete(HttpContent? requestBody = null, CancellationToken cancellationToken = default) => Send(HttpMethod.Delete, requestBody, cancellationToken);

    public async Task<T> Get<T>(CancellationToken cancellationToken = default) {
        using HttpResponseMessage response = await Get(cancellationToken).ConfigureAwait(false);
        return await ParseResponseBody<T>(response, cancellationToken).ConfigureAwait(false);
    }

    public async Task<T> Post<T>(HttpContent? requestBody, CancellationToken cancellationToken = default) {
        using HttpResponseMessage response = await Post(requestBody, cancellationToken).ConfigureAwait(false);
        return await ParseResponseBody<T>(response, cancellationToken).ConfigureAwait(false);
    }

    public async Task<T> Put<T>(HttpContent? requestBody, CancellationToken cancellationToken = default) {
        using HttpResponseMessage response = await Put(requestBody, cancellationToken).ConfigureAwait(false);
        return await ParseResponseBody<T>(response, cancellationToken).ConfigureAwait(false);
    }

    public async Task<T> Patch<T>(HttpContent? requestBody, CancellationToken cancellationToken = default) {
        using HttpResponseMessage response = await Patch(requestBody, cancellationToken).ConfigureAwait(false);
        return await ParseResponseBody<T>(response, cancellationToken).ConfigureAwait(false);
    }

    public async Task<T> Delete<T>(HttpContent? requestBody = null, CancellationToken cancellationToken = default) {
        using HttpResponseMessage response = await Delete(requestBody, cancellationToken).ConfigureAwait(false);
        return await ParseResponseBody<T>(response, cancellationToken).ConfigureAwait(false);
    }

    private static async Task<T> ParseResponseBody<T>(HttpResponseMessage response, CancellationToken cancellationToken) {
        response.EnsureSuccessStatusCode();

        Type                  deserializedType    = typeof(T);
        MediaTypeHeaderValue? responseContentType = response.Content.Headers.ContentType;
        Encoding?             responseEncoding    = null;
        try {
            responseEncoding = responseContentType?.CharSet is { } responseEncodingName ? Encoding.GetEncoding(responseEncodingName) : null;
        } catch (ArgumentException) { }

        if (deserializedType == typeof(XmlDocument)) {
            return (T) (object) await response.Content.ReadDomFromXmlAsync(responseEncoding, cancellationToken).ConfigureAwait(false);
        } else if (deserializedType == typeof(XPathNavigator)) {
            return (T) (object) await response.Content.ReadXPathFromXmlAsync(responseEncoding, cancellationToken).ConfigureAwait(false);
        } else if (deserializedType == typeof(XDocument)) {
            return (T) (object) await response.Content.ReadLinqFromXmlAsync(responseEncoding, cancellationToken: cancellationToken).ConfigureAwait(false);
        }
#if NET6_0_OR_GREATER
        else if (deserializedType == typeof(JsonObject) || deserializedType == typeof(JsonArray) || responseContentType?.MediaType is MediaTypeNames.Application.Json) {
            return await ParseJson().ConfigureAwait(false);
        }
#endif
        else if (responseContentType?.MediaType is MediaTypeNames.Text.Xml or ApplicationXmlMediaType) {
            return await ParseXml().ConfigureAwait(false);
        } else {
#if NET6_0_OR_GREATER
            await using Stream responseBodyStream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
#elif NETSTANDARD2_1_OR_GREATER
            await using Stream responseBodyStream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
#else
            using Stream responseBodyStream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
#endif
            using StreamReader streamReader = new(responseBodyStream, responseEncoding ?? Strings.Utf8, true);
            char[]             headBuffer   = new char[32];
            int                headSize     = await streamReader.ReadAsync(headBuffer, 0, headBuffer.Length).ConfigureAwait(false);
            string             head         = new string(headBuffer, 0, headSize).Trim();
#if NET6_0_OR_GREATER
            if (head.StartsWith('{') || head.StartsWith('[') || head.Contains("\"$schema\"")) {
                return await ParseJson().ConfigureAwait(false);
            } else
#endif
            if (head.StartsWith('<') || head.Contains("<?xml", StringComparison.OrdinalIgnoreCase) || head.Contains("<!--") || head.Contains("xmlns", StringComparison.OrdinalIgnoreCase) ||
                head.Contains("<!doctype", StringComparison.OrdinalIgnoreCase)) {
                return await ParseXml().ConfigureAwait(false);
            }
        }

        throw new SerializationException(
            $"Could not determine content type of response body to deserialize (URI: {response.RequestMessage?.RequestUri}, Content-Type: {responseContentType}, .NET type: {typeof(T)})");

        async Task<T> ParseXml() {
            return await response.Content.ReadObjectFromXmlAsync<T>(responseEncoding, cancellationToken: cancellationToken).ConfigureAwait(false);
        }

#if NET6_0_OR_GREATER
        async Task<T> ParseJson() {
            return await response.Content.ReadFromJsonAsync<T>(JsonSerializerOptions, cancellationToken).ConfigureAwait(false)!;
        }
#endif
    }

    #endregion

}