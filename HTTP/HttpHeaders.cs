namespace Unfucked.HTTP;

public static class HttpHeaders {

    #region Requests

    public const string Accept             = "Accept";
    public const string AcceptEncoding     = "Accept-Encoding";
    public const string AcceptLanguage     = "Accept-Language";
    public const string Authorization      = "Authorization";
    public const string Cookie             = "Cookie";
    public const string Expect             = "Expect";
    public const string Forwarded          = "Forwarded";
    public const string From               = "From";
    public const string Host               = "Host";
    public const string IfMatch            = "If-Match";
    public const string IfModifiedSince    = "If-Modified-Since";
    public const string IfNoneMatch        = "If-None-Match";
    public const string IfRange            = "If-Range";
    public const string IfUnmodifiedSince  = "If-Unmodified-Since";
    public const string Origin             = "Origin";
    public const string ProxyAuthorization = "Proxy-Authorization";
    public const string Range              = "Range";
    public const string Referrer           = "Referer";
    public const string SecFetchDest       = "Sec-Fetch-Dest";
    public const string SecFetchMode       = "Sec-Fetch-Mode";
    public const string SecFetchSite       = "Sec-Fetch-Site";
    public const string SecFetchUser       = "Sec-Fetch-User";
    public const string UserAgent          = "User-Agent";
    public const string XForwardedFor      = "X-Forwarded-For";
    public const string XForwardedHost     = "X-Forwarded-Host";
    public const string XForwardedProto    = "X-Forwarded-Proto";
    public const string XRequestedWith     = "X-Requested-With";

    #endregion

    #region Requests and responses

    public const string CacheControl     = "Cache-Control";
    public const string ContentLanguage  = "Content-Language";
    public const string ContentType      = "Content-Type";
    public const string Date             = "Date";
    public const string TransferEncoding = "Transfer-Encoding";

    #endregion

}