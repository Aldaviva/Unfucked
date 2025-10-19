using System.Diagnostics.Contracts;
using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using Unfucked.HTTP.Config;
using Unfucked.HTTP.Exceptions;

namespace Unfucked.HTTP;

public interface WebTarget: Configurable<WebTarget> {

    /// <summary>
    /// Get the built URL of the HTTP request
    /// </summary>
    [Pure]
    Uri Url { get; }

    /// <summary>
    /// Set the user info (such as <c>username:password</c>) in the URL authority
    /// </summary>
    /// <param name="userInfo">user authentication, or <c>null</c> to clear an existing value</param>
    /// <returns>New immutable target instance with the changed value</returns>
    [Pure]
    WebTarget UserInfo(string? userInfo);

    /// <summary>
    /// Add URL path segments, also called pathname, script info, path info, filePath, or directory/fileName.
    /// </summary>
    /// <param name="segments">New path suffix to append to this request's URL path. To replace instead of append, make this start with <c>/</c>. To remove, pass <c>null</c>.</param>
    /// <param name="autoSplit">If <c>true</c> (default), <paramref name="segments"/> will be split by <c>/</c> into multiple path segments, all of them will be appended, and the <c>/</c> separators won't be URL-encoded into <c>%2F</c>. Otherwise, if <c>false</c>, <paramref name="segments"/> will be appended as one big path segment, and any <c>/</c> separators inside it will be URL-encoded as <c>%2F</c>. You should set this to <c>false</c> if <paramref name="segments"/> is user-supplied.</param>
    /// <returns>New immutable target instance with the changed value</returns>
    [Pure]
    WebTarget Path(string? segments, bool autoSplit = true);

    /// <summary>
    /// Add URL path segments, also called pathname, script info, path info, filePath, or directory/fileName
    /// </summary>
    /// <param name="segments">New path suffix to append to this request's URL path. To replace instead of append, make this start with <c>/</c>.</param>
    /// <returns>New immutable target instance with the changed value</returns>
    [Pure]
    WebTarget Path(object segments);

    /// <summary>
    /// Add URL path segments, also called pathname, script info, path info, filePath, or directory/fileName
    /// </summary>
    /// <param name="segments">New path suffixes to append to this request's URL path. To replace instead of append, make the first segment start with <c>/</c>.</param>
    /// <returns>New immutable target instance with the changed value</returns>
    [Pure]
    WebTarget Path(params IEnumerable<string> segments);

    /// <summary>
    /// Set the URL host port
    /// </summary>
    /// <param name="port">IP port number, or <c>null</c> to remove and use the default for the scheme</param>
    /// <returns>New immutable target instance with the changed value</returns>
    [Pure]
    WebTarget Port(ushort? port);

    /// <summary>
    /// Set the URL hostname (sometimes incorrectly called the host which can include a port number)
    /// </summary>
    /// <param name="hostname">Domain name or IP address. Must not include a port number; to set that, call <see cref="Port"/> instead. For an IPv6 address, enclose it in square brackets (like <c>[::1]</c>).</param>
    /// <returns>New immutable target instance with the changed value</returns>
    [Pure]
    WebTarget Hostname(string hostname);

    /// <summary>
    /// Set the URL scheme (sometimes incorrectly called the protocol)
    /// </summary>
    /// <param name="scheme">URL scheme, such as <c>https</c> or <c>http</c></param>
    /// <returns>New immutable target instance with the changed value</returns>
    [Pure]
    WebTarget Scheme(string scheme);

    /// <summary>
    /// <para>Add a query parameter to the URL (sometimes incorrectly called a GET parameter, search, or parameter to the exclusion of other types of parameters like path parameters)</para>
    /// <para>If a parameter with the same <paramref name="key"/> already exists in this URL, the new <paramref name="value"/> will be added such that the URL will have multiple occurrences of the <paramref name="key"/>, for example <c>?a=b&amp;a=c</c>.</para>
    /// </summary>
    /// <param name="key">Name of the parameter</param>
    /// <param name="value">Value of the parameter, or <c>null</c> to remove all query parameters which have their name set to <paramref name="key"/> from this URL.</param>
    /// <returns>New immutable target instance with the changed value</returns>
    [Pure]
    WebTarget QueryParam(string key, object? value);

    /// <summary>
    /// <para>Add query parameters to the URL (sometimes incorrectly called GET parameters, search, or just parameters to the exclusion of other types of parameters like path parameters)</para>
    /// <para>If a parameter with the same <paramref name="key"/> already exists in this URL, the new <paramref name="values"/> will be added such that the URL will have multiple occurrences of the <paramref name="key"/>, for example <c>?a=b&amp;a=c&amp;a=d</c>.</para>
    /// </summary>
    /// <param name="key">Name of the parameter</param>
    /// <param name="values">Multiple values to add, all with the same <paramref name="key"/>. Any <c>null</c> values will be ignored.</param>
    /// <returns>New immutable target instance with the changed value</returns>
    [Pure]
    WebTarget QueryParam(string key, IEnumerable<object?> values);

    /// <summary>
    /// <para>Add query parameters to the URL (sometimes incorrectly called GET parameters, search, or just parameters to the exclusion of other types of parameters like path parameters)</para>
    /// <para>If parameters with the same key already exist in this URL, the new parameters will be added such that the URL will have multiple occurrences of the key, for example <c>?a=b&amp;a=c&amp;a=d</c>.</para>
    /// </summary>
    /// <param name="parameters">Multiple key-value pairs to add. Parameters with <c>null</c> values will be ignored. If this entire argument is <c>null</c>, then all query parameters will be removed from this URL.</param>
    /// <returns>New immutable target instance with the changed value</returns>
    [Pure]
    WebTarget QueryParam(IEnumerable<KeyValuePair<string, string>> parameters);

    /// <inheritdoc cref="QueryParam(IEnumerable{KeyValuePair{string,string?}}?)" />
    [Pure]
    WebTarget QueryParam(IEnumerable<KeyValuePair<string, object?>>? parameters);

    /// <summary>
    /// Set the URL fragment, also called the hash, fragmentid, or ref
    /// </summary>
    /// <param name="fragment">The new fragment, without the leading <c>#</c> (if you include it, it will be URL-encoded after the read <c>#</c>). Any existing fragment will be replaced. To remove a fragment from the URL, pass <c>null</c>.</param>
    /// <returns>New immutable target instance with the changed value</returns>
    [Pure]
    WebTarget Fragment(string? fragment);

    /// <summary>
    /// <para>Fill in placeholder values in the URL, which are keys surrounded by single curly braces, like <c>{key}</c>.</para>
    /// <para>Also useful for parameters whose values include curly braces, such as query parameters whose value is a JSON object. To avoid the JSON object's braces being treated as a placeholder, pass the JSON object using a template: <c>client.Target(url).QueryParam("value", "{jsonValue}").ResolveTemplate("jsonValue", JsonSerializer.Serialize(obj))</c>.</para>
    /// </summary>
    /// <param name="key">Placeholder name, without the surrounding curly braces.</param>
    /// <param name="value">The value to replace all occurrences of <c>{key}</c> with in the URL. Missing or <c>null</c> values will be replaced by the empty string.</param>
    /// <returns>New immutable target instance with the changed value</returns>
    [Pure]
    WebTarget ResolveTemplate(string key, object? value);

    /// <summary>
    /// <para>Fill in placeholder values in the URL, which are keys surrounded by single curly braces, like <c>{key}</c>.</para>
    /// </summary>
    /// <param name="values">Key-value pairs of placeholder names and replacement values. Missing or <c>null</c> values will be replaced by the empty string.</param>
    /// <returns>New immutable target instance with the changed value</returns>
    [Pure]
    WebTarget ResolveTemplate(IEnumerable<KeyValuePair<string, object?>> values);

    /// <summary>
    /// <para>Fill in placeholder values in the URL, which are keys surrounded by single curly braces, like <c>{key}</c>.</para>
    /// <para>Example: <c>client.Target(url).Path("{a}").QueryParam("b", "{b}").ResolveTemplate(new { a = 1, b = 2 }).Get&lt;string&gt;()</c></para>
    /// </summary>
    /// <param name="anonymousType">An anonymous object which contains properties that will be used to resolve template placeholders. Each property name represents the placeholder key, and the placeholder will be replaced with the property's value.</param>
    /// <returns>New immutable target instance with the changed value</returns>
    [Pure]
    WebTarget ResolveTemplate(object anonymousType);

    /// <summary>
    /// Add an HTTP header to the request
    /// </summary>
    /// <param name="key">Name of the header</param>
    /// <param name="value">Value of the header, or <c>null</c> to remove all occurrences of headers with <paramref name="key"/> in this request. If another header with the same <paramref name="key"/> already exists in this request, both will be sent as separate headers, rather than being sent as one header with combined values. If this is <c>null</c>, all headers with this <paramref name="key"/> will be removed from this request.</param>
    /// <returns>New immutable target instance with the changed value</returns>
    [Pure]
    WebTarget Header(string key, object? value);

    /// <summary>
    /// Add multiple HTTP headers to this request
    /// </summary>
    /// <param name="key">One name to use for all added headers</param>
    /// <param name="values">Multiple values to add headers for. Each value will be sent in a separate header with the <paramref name="key"/>, rather than being sent as one header with combined values.</param>
    /// <returns>New immutable target instance with the changed value</returns>
    [Pure]
    WebTarget Header(string key, params IEnumerable<object> values);

    /// <summary>
    /// Adds zero or more HTTP headers to the request
    /// </summary>
    /// <param name="headers">Map of header keys and values. If a value is <c>null</c>, that key-value pair will be skipped. If this argument is <c>null</c>, then all headers will be removed from the request. If this argument is empty, the headers in the request will be left unmodified. If another header with the same key already exists in this request, both will be sent as separate headers, rather than being sent as one header with combined values.</param>
    /// <returns>New immutable target instance with the changed values</returns>
    [Pure]
    WebTarget Header(IEnumerable<KeyValuePair<string, object?>>? headers);

    /// <summary>
    /// Adds zero or more HTTP headers to the request
    /// </summary>
    /// <param name="headers">Map of header keys and values. If a value is <c>null</c>, that key-value pair will be skipped. If this argument is empty, the headers in the request will be left unmodified. If another header with the same key already exists in this request, both will be sent as separate headers, rather than being sent as one header with combined values.</param>
    /// <returns>New immutable target instance with the changed values</returns>
    [Pure]
    WebTarget Header(IEnumerable<KeyValuePair<string, string>> headers);

    /// <summary>
    /// Set the <c>Accept</c> request header
    /// </summary>
    /// <param name="mediaTypes">One or more media types, also called content types or MIME types</param>
    /// <returns>New immutable target instance with the changed value</returns>
    [Pure]
    WebTarget Accept(params IEnumerable<string> mediaTypes);

    /// <inheritdoc cref="Accept(IEnumerable{string})" />
    [Pure]
    WebTarget Accept(params IEnumerable<MediaTypeHeaderValue> mediaTypes);

    /// <summary>
    /// Set the <c>Accept-Encoding</c> request header
    /// </summary>
    /// <param name="encodings">One or more encodings, also called character sets or codepages</param>
    /// <returns>New immutable target instance with the changed value</returns>
    [Pure]
    WebTarget AcceptEncoding(params IEnumerable<string> encodings);

    /// <summary>
    /// Set the <c>Accept-Language</c> request header
    /// </summary>
    /// <param name="languages">One or more languages, also called locales or cultures</param>
    /// <returns>New immutable target instance with the changed value</returns>
    [Pure]
    WebTarget AcceptLanguage(params IEnumerable<string> languages);

    /// <inheritdoc cref="AcceptLanguage(IEnumerable{string})" />
    [Pure]
    WebTarget AcceptLanguage(params IEnumerable<CultureInfo> languages);

    /// <summary>
    /// Set the <c>Cache-Control</c> request header
    /// </summary>
    /// <param name="cacheControl">Header value</param>
    /// <returns>New immutable target instance with the changed value</returns>
    [Pure]
    WebTarget CacheControl(string cacheControl);

    /// <inheritdoc cref="CacheControl(string)" />
    [Pure]
    WebTarget CacheControl(CacheControlHeaderValue cacheControl);

    /// <summary>
    /// Set the <c>Cookie</c> request header
    /// </summary>
    /// <param name="cookie">One cookie to send</param>
    /// <returns>New immutable target instance with the changed value</returns>
    [Pure]
    WebTarget Cookie(Cookie cookie);

    /// <summary>
    /// Set the <c>Cookie</c> request header
    /// </summary>
    /// <param name="key">Cookie name to send</param>
    /// <param name="value">Cookie value to send</param>
    /// <returns>New immutable target instance with the changed value</returns>
    /// <exception cref="CookieException"></exception>
    [Pure]
    WebTarget Cookie(string key, string value);

    /// <summary>
    /// <para>Set the <c>User-Agent</c> request header</para>
    /// <para>This overrides any default User Agent header value, such as those set in <see cref="HttpClient.DefaultRequestHeaders"/> and especially including <see cref="UnfuckedHttpClient"/>, which sends the program's name and version by default.</para>
    /// </summary>
    /// <param name="userAgentString">User agent string to send</param>
    /// <returns>New immutable target instance with the changed value</returns>
    [Pure]
    WebTarget UserAgent(string userAgentString);

    /// <inheritdoc cref="UserAgent(string)" />
    [Pure]
    WebTarget UserAgent(ProductInfoHeaderValue userAgentString);

    /// <summary>
    /// Set the <c>Authorization</c> request header, which is incorrectly named and actually represents authentication
    /// </summary>
    /// <param name="credentials">Credentials such as a basic username and password, Digest auth, NTLM, or a bearer token. This value should include the conventional authentication protocol prefix and a space (such as "<c>Bearer </c>").</param>
    /// <returns>New immutable target instance with the changed value</returns>
    [Pure]
    WebTarget Authorization(string credentials);

    /// <summary>
    /// Set the <c>Authorization</c> request header, which is incorrectly named and actually represents authentication
    /// </summary>
    /// <param name="username">Basic auth username</param>
    /// <param name="password">Basic auth password</param>
    /// <returns>New immutable target instance with the changed value</returns>
    [Pure]
    WebTarget Authorization(string username, string password);

    /// <summary>
    /// <para>Set the <c>Authorization</c> request header, which is incorrectly named and actually represents authentication</para>
    /// <para>See also <seealso cref="HttpClientHandler.UseDefaultCredentials"/>.</para>
    /// </summary>
    /// <param name="credentials">Windows credentials to send, such as <see cref="CredentialCache.DefaultCredentials"/> or <see cref="CredentialCache.DefaultNetworkCredentials"/></param>
    /// <returns>New immutable target instance with the changed value</returns>
    [Pure]
    WebTarget Authorization(NetworkCredential credentials);

    /// <summary>
    /// Set the <c>Referer</c> request header, which is misspelled in the HTTP protocol but not in this library's API (it is sent in the HTTP-compliant way though).
    /// </summary>
    /// <param name="referrer">Referrer URL</param>
    /// <returns>New immutable target instance with the changed value</returns>
    [Pure]
    WebTarget Referrer(string referrer);

    /// <inheritdoc cref="Referrer(string)" />
    [Pure]
    WebTarget Referrer(Uri referrer);

    /// <summary>
    /// Set the <c>X-Requested-With</c> request header, which is nonstandard but common and useful
    /// </summary>
    /// <param name="requester">Header value, or omit this to send the common value <c>XMLHttpRequest</c></param>
    /// <returns>New immutable target instance with the changed value</returns>
    [Pure]
    WebTarget RequestedWith(string requester = "XMLHttpRequest");

    /// <summary>
    /// Finish building the request and send it with an arbitrary verb and optional body, and don't deserialize the response body.
    /// </summary>
    /// <param name="verb">HTTP verb, such as <see cref="HttpMethod.Get"/>. Sometimes called a method.</param>
    /// <param name="requestBody">Optional body to send with the request, or <c>null</c> to not send a body</param>
    /// <param name="cancellationToken">To stop waiting for the response</param>
    /// <returns>A raw <see cref="HttpResponseMessage"/>, whose status and headers you can inspect and whose response body you can parse manually. You should dispose this.</returns>
    /// <exception cref="ProcessingException">A network error occurred, such a timeout, DNS error, or connection refused</exception>
    Task<HttpResponseMessage> Send(HttpMethod verb, HttpContent? requestBody = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Finish building the request and send it with an arbitrary verb and optional body, and deserialize the response body.
    /// </summary>
    /// <typeparam name="T">Type to deserialize the response body into</typeparam>
    /// <param name="verb">HTTP verb, such as <see cref="HttpMethod.Get"/>. Sometimes called a method.</param>
    /// <param name="requestBody">Optional body to send with the request, or <c>null</c> to not send a body</param>
    /// <param name="cancellationToken">To stop waiting for the response</param>
    /// <returns>The <typeparamref name="T"/>  instance deserialized from the response body</returns>
    /// <exception cref="ProcessingException">A network error occurred, such a timeout, DNS error, or connection refused; or response body deserization failed</exception>
    /// <exception cref="WebApplicationException">The server responded with an unsuccessful status code, and <see cref="PropertyKey.ThrowOnUnsuccessfulStatusCode"/> was left enabled</exception>
    Task<T> Send<T>(HttpMethod verb, HttpContent? requestBody = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Finish building the request and send it with the GET verb, and don't deserialize the response body.
    /// </summary>
    /// <param name="cancellationToken">To stop waiting for the response</param>
    /// <returns>A raw <see cref="HttpResponseMessage"/>, whose status and headers you can inspect and whose response body you can parse manually. You should dispose this.</returns>
    /// <exception cref="ProcessingException">A network error occurred, such a timeout, DNS error, or connection refused</exception>
    Task<HttpResponseMessage> Get(CancellationToken cancellationToken = default);

    /// <summary>
    /// Finish building the request and send it with the GET verb, and deserialize the response body.
    /// </summary>
    /// <typeparam name="T">Type to deserialize the response body into</typeparam>
    /// <param name="cancellationToken">To stop waiting for the response</param>
    /// <returns>The <typeparamref name="T"/>  instance deserialized from the response body</returns>
    /// <exception cref="ProcessingException">A network error occurred, such a timeout, DNS error, or connection refused; or response body deserization failed</exception>
    /// <exception cref="WebApplicationException">The server responded with an unsuccessful status code, and <see cref="PropertyKey.ThrowOnUnsuccessfulStatusCode"/> was left enabled</exception>
    Task<T> Get<T>(CancellationToken cancellationToken = default);

    /// <summary>
    /// Finish building the request and send it with the HEAD verb.
    /// </summary>
    /// <param name="cancellationToken">To stop waiting for the response</param>
    /// <returns>A raw <see cref="HttpResponseMessage"/>, whose status and headers you can inspect. You should dispose this.</returns>
    /// <exception cref="ProcessingException">A network error occurred, such a timeout, DNS error, or connection refused</exception>
    Task<HttpResponseMessage> Head(CancellationToken cancellationToken = default);

    /// <summary>
    /// Finish building the request and send it with the POST verb and optional body, and don't deserialize the response body.
    /// </summary>
    /// <param name="requestBody">Optional body to send with the request, or <c>null</c> to not send a body</param>
    /// <param name="cancellationToken">To stop waiting for the response</param>
    /// <returns>A raw <see cref="HttpResponseMessage"/>, whose status and headers you can inspect and whose response body you can parse manually. You should dispose this.</returns>
    /// <exception cref="ProcessingException">A network error occurred, such a timeout, DNS error, or connection refused</exception>
    Task<HttpResponseMessage> Post(HttpContent? requestBody, CancellationToken cancellationToken = default);

    /// <summary>
    /// Finish building the request and send it with the POST verb and optional body, and deserialize the response body.
    /// </summary>
    /// <typeparam name="T">Type to deserialize the response body into</typeparam>
    /// <param name="requestBody">Optional body to send with the request, or <c>null</c> to not send a body</param>
    /// <param name="cancellationToken">To stop waiting for the response</param>
    /// <returns>The <typeparamref name="T"/>  instance deserialized from the response body</returns>
    /// <exception cref="ProcessingException">A network error occurred, such a timeout, DNS error, or connection refused; or response body deserization failed</exception>
    /// <exception cref="WebApplicationException">The server responded with an unsuccessful status code, and <see cref="PropertyKey.ThrowOnUnsuccessfulStatusCode"/> was left enabled</exception>
    Task<T> Post<T>(HttpContent? requestBody, CancellationToken cancellationToken = default);

    /// <summary>
    /// Finish building the request and send it with the PUT verb and optional body, and don't deserialize the response body.
    /// </summary>
    /// <param name="requestBody">Optional body to send with the request, or <c>null</c> to not send a body</param>
    /// <param name="cancellationToken">To stop waiting for the response</param>
    /// <returns>A raw <see cref="HttpResponseMessage"/>, whose status and headers you can inspect and whose response body you can parse manually. You should dispose this.</returns>
    /// <exception cref="ProcessingException">A network error occurred, such a timeout, DNS error, or connection refused</exception>
    Task<HttpResponseMessage> Put(HttpContent? requestBody, CancellationToken cancellationToken = default);

    /// <summary>
    /// Finish building the request and send it with the PUT verb and optional body, and deserialize the response body.
    /// </summary>
    /// <typeparam name="T">Type to deserialize the response body into</typeparam>
    /// <param name="requestBody">Optional body to send with the request, or <c>null</c> to not send a body</param>
    /// <param name="cancellationToken">To stop waiting for the response</param>
    /// <returns>The <typeparamref name="T"/>  instance deserialized from the response body</returns>
    /// <exception cref="ProcessingException">A network error occurred, such a timeout, DNS error, or connection refused; or response body deserization failed</exception>
    /// <exception cref="WebApplicationException">The server responded with an unsuccessful status code, and <see cref="PropertyKey.ThrowOnUnsuccessfulStatusCode"/> was left enabled</exception>
    Task<T> Put<T>(HttpContent? requestBody, CancellationToken cancellationToken = default);

    /// <summary>
    /// Finish building the request and send it with the PATCH verb and optional body, and don't deserialize the response body.
    /// </summary>
    /// <param name="requestBody">Optional body to send with the request, or <c>null</c> to not send a body</param>
    /// <param name="cancellationToken">To stop waiting for the response</param>
    /// <returns>A raw <see cref="HttpResponseMessage"/>, whose status and headers you can inspect and whose response body you can parse manually. You should dispose this.</returns>
    /// <exception cref="ProcessingException">A network error occurred, such a timeout, DNS error, or connection refused</exception>
    Task<HttpResponseMessage> Patch(HttpContent? requestBody, CancellationToken cancellationToken = default);

    /// <summary>
    /// Finish building the request and send it with the PATCH verb and optional body, and deserialize the response body.
    /// </summary>
    /// <typeparam name="T">Type to deserialize the response body into</typeparam>
    /// <param name="requestBody">Optional body to send with the request, or <c>null</c> to not send a body</param>
    /// <param name="cancellationToken">To stop waiting for the response</param>
    /// <returns>The <typeparamref name="T"/>  instance deserialized from the response body</returns>
    /// <exception cref="ProcessingException">A network error occurred, such a timeout, DNS error, or connection refused; or response body deserization failed</exception>
    /// <exception cref="WebApplicationException">The server responded with an unsuccessful status code, and <see cref="PropertyKey.ThrowOnUnsuccessfulStatusCode"/> was left enabled</exception>
    Task<T> Patch<T>(HttpContent? requestBody, CancellationToken cancellationToken = default);

    /// <summary>
    /// Finish building the request and send it with the DELETE verb and optional body, and don't deserialize the response body.
    /// </summary>
    /// <param name="requestBody">Optional body to send with the request, or <c>null</c> to not send a body</param>
    /// <param name="cancellationToken">To stop waiting for the response</param>
    /// <returns>A raw <see cref="HttpResponseMessage"/>, whose status and headers you can inspect and whose response body you can parse manually. You should dispose this.</returns>
    /// <exception cref="ProcessingException">A network error occurred, such a timeout, DNS error, or connection refused</exception>
    Task<HttpResponseMessage> Delete(HttpContent? requestBody = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Finish building the request and send it with the DELETE verb and optional body, and deserialize the response body.
    /// </summary>
    /// <typeparam name="T">Type to deserialize the response body into</typeparam>
    /// <param name="requestBody">Optional body to send with the request, or <c>null</c> to not send a body</param>
    /// <param name="cancellationToken">To stop waiting for the response</param>
    /// <returns>The <typeparamref name="T"/>  instance deserialized from the response body</returns>
    /// <exception cref="ProcessingException">A network error occurred, such a timeout, DNS error, or connection refused; or response body deserization failed</exception>
    /// <exception cref="WebApplicationException">The server responded with an unsuccessful status code, and <see cref="PropertyKey.ThrowOnUnsuccessfulStatusCode"/> was left enabled</exception>
    Task<T> Delete<T>(HttpContent? requestBody = null, CancellationToken cancellationToken = default);

}