using System.Collections.Specialized;
using System.Web;

namespace Unfucked;

public static class Uris {

    public static NameValueCollection GetQuery(this Uri uri) => HttpUtility.ParseQueryString(uri.Query);

    /// <returns><c>true</c> if the <paramref name="url"/> hostname is the same as <paramref name="ancestorOrSelfDomain"/> or is a subdomain of <paramref name="ancestorOrSelfDomain"/>; <c>false</c> otherwise</returns>
    public static bool BelongsToDomain(this Uri url, string ancestorOrSelfDomain) {
        string actualHostname = url.Host;
        return actualHostname.Equals(ancestorOrSelfDomain, StringComparison.InvariantCultureIgnoreCase)
            || actualHostname.EndsWith("." + ancestorOrSelfDomain, StringComparison.InvariantCultureIgnoreCase);
    }

}