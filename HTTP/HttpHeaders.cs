namespace Unfucked.HTTP;

public static class HttpHeaders {

    #region Requests

    public const string ACCEPT              = "Accept";
    public const string ACCEPT_ENCODING     = "Accept-Encoding";
    public const string ACCEPT_LANGUAGE     = "Accept-Language";
    public const string AUTHORIZATION       = "Authorization";
    public const string COOKIE              = "Cookie";
    public const string EXPECT              = "Expect";
    public const string FORWARDED           = "Forwarded";
    public const string FROM                = "From";
    public const string HOST                = "Host";
    public const string IF_MATCH            = "If-Match";
    public const string IF_MODIFIED_SINCE   = "If-Modified-Since";
    public const string IF_NONE_MATCH       = "If-None-Match";
    public const string IF_RANGE            = "If-Range";
    public const string IF_UNMODIFIED_SINCE = "If-Unmodified-Since";
    public const string ORIGIN              = "Origin";
    public const string PROXY_AUTHORIZATION = "Proxy-Authorization";
    public const string RANGE               = "Range";
    public const string REFERRER            = "Referer";
    public const string SEC_FETCH_DEST      = "Sec-Fetch-Dest";
    public const string SEC_FETCH_MODE      = "Sec-Fetch-Mode";
    public const string SEC_FETCH_SITE      = "Sec-Fetch-Site";
    public const string SEC_FETCH_USER      = "Sec-Fetch-User";
    public const string USER_AGENT          = "User-Agent";
    public const string X_FORWARDED_FOR     = "X-Forwarded-For";
    public const string X_FORWARDED_HOST    = "X-Forwarded-Host";
    public const string X_FORWARDED_PROTO   = "X-Forwarded-Proto";
    public const string X_REQUESTED_WITH    = "X-Requested-With";

    #endregion

    #region Requests and responses

    public const string CACHE_CONTROL     = "Cache-Control";
    public const string CONTENT_LANGUAGE  = "Content-Language";
    public const string CONTENT_TYPE      = "Content-Type";
    public const string DATE              = "Date";
    public const string TRANSFER_ENCODING = "Transfer-Encoding";

    #endregion

}