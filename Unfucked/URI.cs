using System.Collections.Specialized;
using System.Web;

namespace Unfucked;

/// <summary>
/// Methods that make it easier to work with URIs and URLs.
/// </summary>
public static class URI {

    /// <summary>
    /// Get the query parameters from a URI.
    /// </summary>
    /// <param name="uri">A URI that could have query parameters.</param>
    /// <returns>Collection of string key-value pairs of the query parameters.</returns>
    [Pure]
    public static NameValueCollection GetQuery(this Uri uri) => HttpUtility.ParseQueryString(uri.Query);

    /// <summary>Test if a URL has the same domain as <paramref name="ancestorOrSelfDomain"/>, or if it is a subdomain of it. This can be used for site locking.</summary>
    /// <param name="url">A URL to test, such as <c>https://west.aldaviva.com</c>.</param>
    /// <param name="ancestorOrSelfDomain">The expected exact domain or ancestor domain of <paramref name="url"/>, such as <c>aldaviva.com</c>.</param>
    /// <returns><c>true</c> if the <paramref name="url"/> hostname is the same as <paramref name="ancestorOrSelfDomain"/> or is a subdomain of it; <c>false</c> otherwise. For example, <c>https://west.aldaviva.com</c> does belong to the domain <c>aldaviva.com</c>, so this would return <c>true</c>.</returns>
    [Pure]
    public static bool BelongsToDomain(this Uri url, string ancestorOrSelfDomain) {
        string actualHostname = url.Host;
        return actualHostname.Equals(ancestorOrSelfDomain, StringComparison.InvariantCultureIgnoreCase)
            || actualHostname.EndsWith("." + ancestorOrSelfDomain, StringComparison.InvariantCultureIgnoreCase);
    }

    /// <summary>Test if a URL has the same domain as <paramref name="ancestorOrSelfUri"/>, or if it is a subdomain of it. This can be used for site locking.</summary>
    /// <param name="url">A URL to test, such as <c>https://west.aldaviva.com</c>.</param>
    /// <param name="ancestorOrSelfUri">A URI with the expected exact domain or ancestor domain of <paramref name="url"/>, such as <c>aldaviva.com</c>.</param>
    /// <returns><c>true</c> if the <paramref name="url"/> hostname is the same as the host of <paramref name="ancestorOrSelfUri"/> or is a subdomain of it; <c>false</c> otherwise. For example, <c>https://west.aldaviva.com</c> does belong to the domain of <c>http://aldaviva.com/</c>, so this would return <c>true</c>.</returns>
    [Pure]
    public static bool BelongsToDomain(this Uri url, Uri ancestorOrSelfUri) => BelongsToDomain(url, ancestorOrSelfUri.Host);

    /// <summary>Test if a request's URL has the same domain as <paramref name="ancestorOrSelfDomain"/>, or if it is a subdomain of it. This can be used for site locking.</summary>
    /// <param name="request">A request whose URL (such as <c>https://west.aldaviva.com/</c>) will be tested.</param>
    /// <param name="ancestorOrSelfDomain">The expected exact domain or ancestor domain of the <paramref name="request"/> URL, such as <c>aldaviva.com</c>.</param>
    /// <returns><c>true</c> if the <paramref name="request"/> URL's hostname is the same as <paramref name="ancestorOrSelfDomain"/> or is a subdomain of it; <c>false</c> otherwise. For example, <c>https://west.aldaviva.com</c> does belong to the domain <c>aldaviva.com</c>, so this would return <c>true</c>.</returns>
    [Pure]
    public static bool UriBelongsToDomain(this HttpRequestMessage request, string ancestorOrSelfDomain) => request.RequestUri?.BelongsToDomain(ancestorOrSelfDomain) ?? false;

    /// <summary>Test if a request's URL has the same domain as <paramref name="ancestorOrSelfUri"/>, or if it is a subdomain of it. This can be used for site locking.</summary>
    /// <param name="request">A request whose URL (such as <c>https://west.aldaviva.com/</c>) will be tested.</param>
    /// <param name="ancestorOrSelfUri">A URI with the expected exact domain or ancestor domain of the <paramref name="request"/> URL, such as <c>aldaviva.com</c>.</param>
    /// <returns><c>true</c> if the <paramref name="request"/> URL's hostname is the same as the host of <paramref name="ancestorOrSelfUri"/> or is a subdomain of it; <c>false</c> otherwise. For example, <c>https://west.aldaviva.com</c> does belong to the domain of <c>http://aldaviva.com/</c>, so this would return <c>true</c>.</returns>
    [Pure]
    public static bool UriBelongsToDomain(this HttpRequestMessage request, Uri ancestorOrSelfUri) => request.UriBelongsToDomain(ancestorOrSelfUri.Host);

    /// <summary>
    /// Get a left part of a URI.
    /// </summary>
    /// <param name="uri">The URI to manipulate.</param>
    /// <param name="preserve">The rightmost part of <paramref name="uri"/> to keep (inclusive). All parts to the right of this value will be removed.</param>
    /// <returns>The string form of <paramref name="uri"/> with all parts to the right of <paramref name="preserve"/> omitted.</returns>
    [Pure]
    public static string Truncate(this Uri uri, Part preserve) {
        UriBuilder builder = new(uri) { Fragment = string.Empty };
        if (preserve < Part.Query) {
            builder.Query = string.Empty;
        }
        if (preserve < Part.Path) {
            builder.Path = string.Empty;
        }
        if (preserve < Part.Authority) {
            builder.UserName = builder.Password = string.Empty;
        }
        string truncated = builder.Uri.ToString();
        if (preserve == Part.Origin) {
            truncated = truncated.TrimEnd('/');
        }
        return truncated;
    }

    /// <summary>
    /// <see href="https://tantek.com/2011/238/b1/many-ways-slice-url-name-pieces"/>
    /// </summary>
    public enum Part {

        /// <summary>The scheme, hostname, and port, like <c>https://aldaviva.com:443</c> (<see href="https://developer.mozilla.org/en-US/docs/Glossary/Origin"/>)</summary>
        Origin,

        /// <summary>The scheme, user info, hostname, port, and the root path (<c>/</c>), like <c>https://user:pass@aldaviva.com:443/</c></summary>
        Authority,

        /// <summary>The scheme, user info, hostname, port, and path, like <c>https://user:pass@aldaviva.com:443/my/path/</c> (everything to the left of the query)</summary>
        Path,

        /// <summary>The scheme, user info, hostname, port, path, and query parameters, like <c>https://user:pass@aldaviva.com:443/my/path/?q=r&amp;s=t</c> (everything except the fragment)</summary>
        Query

    }

    [Pure]
    public static UrlBuilder ToBuilder(this Uri uri) => new(uri);

}