using System.Collections.Specialized;
using System.Globalization;
using System.Web;

namespace Unfucked;

/// <summary>
/// Methods that make it easier to work with URIs and URLs.
/// </summary>
public static class UriExtensions {

    /// <param name="uri">A URI that could have query parameters.</param>
    extension(Uri uri) {

        /// <summary>
        /// Get the query parameters from a URI.
        /// </summary>
        /// <returns>Collection of string key-value pairs of the query parameters.</returns>
        [Pure]
        public NameValueCollection QueryParams => HttpUtility.ParseQueryString(uri.Query);

        /// <summary>Get the decoded path segments from a URI. Like <see cref="Uri.Segments"/>, but without the annoying URL-encoding or slash separators at the beginning of every segment.</summary>
        /// <returns>List of path segments, URL-decoded, without slash separators. Empty segments (created by consecutive separators, like //) will be represented by <see cref="string.Empty"/>.</returns>
        [Pure]
        public IReadOnlyList<string> Path {
            get {
                string       rawPath  = uri.AbsolutePath;
                List<string> segments = [];

                int start;
                int end = 0; // starts at 1 to skip the ubiquitous leading /
                do {
                    start = end + 1;
                    end   = rawPath.IndexOf('/', start);

                    if (start < end) {
                        segments.Add(UrlEncoder.Decode(rawPath.AsSpan(start, end - start), UrlEncoder.Component.PathSegment).ToString());
                    } else if (start == end) {
                        segments.Add(string.Empty);
                    }
                } while (end != -1);

                segments.Add(start < rawPath.Length ? UrlEncoder.Decode(rawPath.AsSpan(start), UrlEncoder.Component.PathSegment).ToString() : string.Empty);
                return segments.AsReadOnly();
            }
        }

    }

    private static IdnMapping? idnMapping;

    /// <summary>Test if a URL has the same domain as <paramref name="expectedBaseDomain"/>, or if it is a subdomain of it. This can be used for site locking.</summary>
    /// <param name="url">A URL to test, such as <c>https://west.aldaviva.com</c>.</param>
    /// <param name="expectedBaseDomain">The expected exact domain or ancestor domain of <paramref name="url"/>, such as <c>aldaviva.com</c>.</param>
    /// <returns><c>true</c> if the <paramref name="url"/> hostname is the same as <paramref name="expectedBaseDomain"/> or is a subdomain of it; <c>false</c> otherwise. For example, <c>https://west.aldaviva.com</c> does belong to the domain <c>aldaviva.com</c>, so this would return <c>true</c>; however, <c>https://aldaviva.com.evilsite.com</c> does not belong to the domain <c>aldaviva.com</c>, so this would return <c>false</c>.</returns>
    [Pure]
    public static bool BelongsToDomain(this Uri url, string expectedBaseDomain) {
        if (!url.IsAbsoluteUri || url.IsFile) return false;

        string actualHostname = url.IdnHost;
        idnMapping         ??= new IdnMapping();
        expectedBaseDomain =   idnMapping.GetAscii(expectedBaseDomain);
        return actualHostname.Equals(expectedBaseDomain, StringComparison.InvariantCultureIgnoreCase)
            || actualHostname.EndsWith('.' + expectedBaseDomain, StringComparison.InvariantCultureIgnoreCase);
    }

    /// <summary>Test if a URL has the same domain as <paramref name="expectedBaseDomain"/>, or if it is a subdomain of it. This can be used for site locking.</summary>
    /// <param name="url">A URL to test, such as <c>https://west.aldaviva.com</c>.</param>
    /// <param name="expectedBaseDomain">A URI with the expected exact domain or ancestor domain of <paramref name="url"/>, such as <c>https://aldaviva.com</c>. All URI components besides the hostname are ignored and do not have to match, such as the scheme and path.</param>
    /// <returns><c>true</c> if the <paramref name="url"/> hostname is the same as the host of <paramref name="expectedBaseDomain"/> or is a subdomain of it; <c>false</c> otherwise. For example, <c>https://west.aldaviva.com</c> does belong to the domain of <c>http://aldaviva.com/</c>, so this would return <c>true</c>; however, <c>https://aldaviva.com.evilsite.com</c> does not belong to the domain of <c>https://aldaviva.com</c>, so this would return <c>false</c>.</returns>
    [Pure]
    public static bool BelongsToDomain(this Uri url, Uri expectedBaseDomain) => url.BelongsToDomain(expectedBaseDomain.IdnHost);

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

    /*
     * This does not return a Uri because some return values are not isomorphic/roundtrippable with Uris, such as origins, which must not have a trailing /, but all pathless .NET Uris have a trailing slash.
     */
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

    /// <summary>
    /// Convert this <see cref="Uri"/> to a <see cref="UrlBuilder"/> so you can manipulate it and generate a modified URL from it.
    /// </summary>
    /// <param name="uri">Source</param>
    /// <returns>URL builder initialized to the value of <paramref name="uri"/></returns>
    [Pure]
    public static UrlBuilder ToBuilder(this Uri uri) => new(uri);

}