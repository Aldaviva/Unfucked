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

    /// <summary>Test if a URL's second-level domain is a given value. This can be used for site locking.</summary>
    /// <param name="url">A URL that has the domain in question.</param>
    /// <param name="ancestorOrSelfDomain">A second-level (registerable) domain name.</param>
    /// <returns><c>true</c> if the <paramref name="url"/> hostname is the same as <paramref name="ancestorOrSelfDomain"/> or is a subdomain of <paramref name="ancestorOrSelfDomain"/>; <c>false</c> otherwise</returns>
    [Pure]
    public static bool BelongsToDomain(this Uri url, string ancestorOrSelfDomain) {
        string actualHostname = url.Host;
        return actualHostname.Equals(ancestorOrSelfDomain, StringComparison.InvariantCultureIgnoreCase)
            || actualHostname.EndsWith("." + ancestorOrSelfDomain, StringComparison.InvariantCultureIgnoreCase);
    }

    [Pure]
    public static string Origin(this Uri uri) => new UriBuilder(uri) {
        UserName = string.Empty,
        Password = string.Empty,
        Path     = string.Empty,
        Query    = string.Empty,
        Fragment = string.Empty
    }.Uri.ToString().TrimEnd('/');

    [Pure]
    public static UrlBuilder ToBuilder(this Uri uri) => new(uri);

}