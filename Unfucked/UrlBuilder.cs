using System.Buffers;
using System.Collections.Immutable;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;

// ReSharper disable UseIndexFromEndExpression - incompatible with .NET Standard 2.0, which this project obviously targets

#pragma warning disable SYSLIB1045 // Generate regex at compile time - not possible with .NET Standard

namespace Unfucked;

public class UrlBuilder: ICloneable {

    private static readonly Regex  PLACEHOLDER_PATTERN              = new(@"\{(?<key>\w+?)\}");
    private static readonly object VALUELESS_QUERY_PARAM            = new();
    private static readonly char[] QUERY_PARAM_SEPARATORS           = ['&'];
    private static readonly char[] QUERY_PARAM_KEY_VALUE_SEPARATORS = ['='];
    private static readonly char[] PATH_SEPARATORS                  = ['/'];

    // ReSharper disable InconsistentNaming - don't collide with method names
    private string? _scheme { get; init; }
    private string? _userInfo { get; init; }
    private string? _hostname { get; init; }
    private ushort? _port { get; init; }
    private ImmutableList<string> _path { get; init; } = ImmutableList<string>.Empty;
    private bool _trailingSlash { get; init; }
    private ImmutableList<KeyValuePair<string, object>> _queryParameters { get; init; } = ImmutableList<KeyValuePair<string, object>>.Empty;
    private string? _fragment { get; init; }
    private ImmutableDictionary<string, object?> _templateValues { get; init; } = ImmutableDictionary<string, object?>.Empty;
    private bool _enableTemplates { get; init; } = true;
    private ImmutableHashSet<string>? _unusedTemplateQueryParameterRealNames { get; init; }
    // ReSharper restore InconsistentNaming

    #region Construction

    public UrlBuilder(string scheme, string hostname, ushort? port = null) {
        _scheme   = scheme.TrimEnd(':');
        _hostname = hostname.TrimStart("//");
        _port     = port;
    }

    public UrlBuilder(Uri uri) {
        _scheme        = uri.Scheme.EmptyToNull();
        _userInfo      = uri.UserInfo.EmptyToNull();
        _hostname      = uri.Host.EmptyToNull();
        _port          = uri.Port == -1 ? null : (ushort?) uri.Port;
        _path          = uri.Segments.SkipWhile(s => s == "/").Select(s => s.TrimEnd('/')).ToImmutableList();
        _trailingSlash = uri.Segments.LastOrDefault()?.EndsWith('/') ?? false;
        _queryParameters = uri.GetQuery() is var originalQuery
            ? originalQuery.Keys.Cast<string>().Select(k => new KeyValuePair<string, object>(k, originalQuery[k] ?? VALUELESS_QUERY_PARAM)).ToImmutableList()
            : ImmutableList<KeyValuePair<string, object>>.Empty;
        _fragment = uri.Fragment.TrimStart(1, '#').EmptyToNull();
    }

    public UrlBuilder(UriBuilder uriBuilder) {
        _scheme        = uriBuilder.Scheme.EmptyToNull();
        _userInfo      = uriBuilder.UserName.HasLength() || uriBuilder.Password.HasLength() ? $"{uriBuilder.UserName}:{uriBuilder.Password}" : null;
        _hostname      = uriBuilder.Host.EmptyToNull();
        _port          = uriBuilder.Port == -1 ? null : (ushort?) uriBuilder.Port;
        _path          = uriBuilder.Path.TrimStart('/').Split(PATH_SEPARATORS, StringSplitOptions.RemoveEmptyEntries).ToImmutableList();
        _trailingSlash = uriBuilder.Path.EndsWith('/');
        _queryParameters = uriBuilder.Query.TrimStart('?').Split(QUERY_PARAM_SEPARATORS, StringSplitOptions.RemoveEmptyEntries).Select(p => {
            string[] split = p.Split(QUERY_PARAM_KEY_VALUE_SEPARATORS, 2);
            return new KeyValuePair<string, object>(split[0], split.ElementAtOrDefault(1) ?? VALUELESS_QUERY_PARAM);
        }).ToImmutableList();
        _fragment = uriBuilder.Fragment.TrimStart(1, '#').EmptyToNull();
    }

    private UrlBuilder(UrlBuilder other) {
        _scheme                                = other._scheme;
        _userInfo                              = other._userInfo;
        _hostname                              = other._hostname;
        _port                                  = other._port;
        _path                                  = other._path;
        _trailingSlash                         = other._trailingSlash;
        _queryParameters                       = other._queryParameters;
        _fragment                              = other._fragment;
        _templateValues                        = other._templateValues;
        _enableTemplates                       = other._enableTemplates;
        _unusedTemplateQueryParameterRealNames = other._unusedTemplateQueryParameterRealNames;
    }

    /// <exception cref="UriFormatException"></exception>
    public UrlBuilder(string uri): this(new Uri(uri, UriKind.Absolute)) {}

    public static implicit operator UrlBuilder(Uri uri) => new(uri);

    public static implicit operator UrlBuilder(UriBuilder uri) => new(uri);

    /// <exception cref="UriFormatException"></exception>
    public static explicit operator UrlBuilder(string uri) => new(uri);

    /// <exception cref="UriFormatException"></exception>
    public static UrlBuilder FromTemplate(string uriTemplate) {
        const string alphabet            = "abcdefghijklmnopqrstuvwxyz";
        Regex        templatePattern     = new(@"\{(?<prefix>[/?&]?)(?<names>[\w-,]+?)\}");
        Regex        fakeTemplatePattern = new("template[A-Za-z]{16}");

        var fakeToRealTemplateNames = new Dictionary<string, string>();
        var queryParameterRealNames = new HashSet<string>();

        string templateWithFakePlaceholders = templatePattern.Replace(uriTemplate, match => {
            string fakeName;
            string prefix = match.Groups["prefix"].Value;
            switch (prefix) {
                case "?":
                case "&":
                    StringBuilder replacement = new();
                    foreach (string realName in match.Groups["names"].Value.Split(',')) {
                        fakeName = generateFakeName();
                        fakeToRealTemplateNames.Add(fakeName, realName);
                        queryParameterRealNames.Add(realName);
                        replacement.Append(replacement.Length == 0 && prefix == "?" ? '?' : '&').Append(realName).Append('=').Append(fakeName);
                    }
                    return replacement.ToString();
                default:
                    fakeName = generateFakeName();
                    fakeToRealTemplateNames.Add(fakeName, match.Groups["names"].Value);
                    return $"{prefix}{fakeName}";
            }
        });

        UrlBuilder urlBuilder = new(templateWithFakePlaceholders) { _unusedTemplateQueryParameterRealNames = queryParameterRealNames.ToImmutableHashSet() };
        if (restoreRealName(urlBuilder._scheme) is {} realScheme) {
            urlBuilder = urlBuilder.Scheme(realScheme);
        }
        if (restoreRealName(urlBuilder._hostname) is {} realHostname) {
            urlBuilder = urlBuilder.Hostname(realHostname);
        }
        for (int i = 0; i < urlBuilder._path.Count; i++) {
            if (restoreRealName(urlBuilder._path[i]) is {} realPathSegment) {
                urlBuilder = new UrlBuilder(urlBuilder) { _path = urlBuilder._path.SetItem(i, realPathSegment) };
            }
        }
        for (int i = 0; i < urlBuilder._queryParameters.Count; i++) {
            if (urlBuilder._queryParameters[i].Value is string value && restoreRealName(value) is {} realQueryParam) {
                urlBuilder = new UrlBuilder(urlBuilder)
                    { _queryParameters = urlBuilder._queryParameters.SetItem(i, new KeyValuePair<string, object>(urlBuilder._queryParameters[i].Key, realQueryParam)) };
            }
        }
        if (restoreRealName(urlBuilder._fragment) is {} realFragment) {
            urlBuilder = urlBuilder.Fragment(realFragment);
        }

        return urlBuilder;

        string generateFakeName() {
            string fakeName;
            do {
                fakeName = "template" + Cryptography.GenerateRandomString(16, alphabet);
            } while (uriTemplate.Contains(fakeName) || fakeToRealTemplateNames.ContainsKey(fakeName));
            return fakeName;
        }

        string? restoreRealName(string? haystack) {
            if (haystack == null) return null;
            bool wasReplaced = false;
            string replaced = fakeTemplatePattern.Replace(haystack, match => {
                bool hasRealName = fakeToRealTemplateNames.TryGetValue(match.Value, out string? realName);
                wasReplaced |= hasRealName;
                return hasRealName ? $"{{{realName}}}" : match.Value;
            });
            return wasReplaced ? replaced : null;
        }
    }

    [Pure]
    public object Clone() => new UrlBuilder(this);

    #endregion

    #region Serialization

    // ExceptionAdjustment: M:System.Uri.#ctor(System.String,System.UriKind) -T:System.UriFormatException
    [Pure]
    public Uri ToUrl() {
        StringBuilder built = new();

        if (_scheme != null) {
            built.Append(ReplacePlaceholders(_scheme)).Append(':');
        }

        if (_userInfo != null || _hostname != null || _port != null) {
            built.Append("//");
        }

        if (_userInfo != null) {
            built.Append(UrlEncoder.Encode(ReplacePlaceholders(_userInfo), UrlEncoder.Component.USER_INFO)).Append('@');
        }

        if (_hostname is {} hostname) {
            hostname = ReplacePlaceholders(hostname);
            if (IPAddress.TryParse(hostname, out IPAddress? ipAddress) && ipAddress.AddressFamily == AddressFamily.InterNetworkV6) {
                built.Append('[').Append(hostname).Append(']');
            } else {
                built.Append(hostname);
            }
        }

        if (_port.HasValue) {
            built.Append(':').Append(_port.Value);
        }

        if (!_path.IsEmpty) {
            built.Append('/').AppendJoin('/',
                _path.Select(p => UrlEncoder.Encode(ReplacePlaceholders(p.Trim('/')), UrlEncoder.Component.PATH_SEGMENT)));

#pragma warning disable IDE0056 // Use index operator - not available when targeting .NET Standard 2
            if (_trailingSlash && built[built.Length - 1] != '/') {
                built.Append('/');
            }
#pragma warning restore IDE0056 // Use index operator
        }

        IList<KeyValuePair<string, object>> queryParameters = _unusedTemplateQueryParameterRealNames != null
            ? _queryParameters.Where(pair => !_unusedTemplateQueryParameterRealNames.Contains(pair.Key)).ToList() : _queryParameters;
        if (queryParameters.Count != 0) {
            built.Append('?').AppendJoin('&', queryParameters.Select(pair => ReferenceEquals(pair.Value, VALUELESS_QUERY_PARAM)
                ? UrlEncoder.Encode(pair.Key, UrlEncoder.Component.QUERY_PARAMETER)
                : $"{UrlEncoder.Encode(pair.Key, UrlEncoder.Component.QUERY_PARAMETER)}={UrlEncoder.Encode(ReplacePlaceholders(Stringify(pair.Value) ?? string.Empty), UrlEncoder.Component.QUERY_PARAMETER)}"));
        }

        if (_fragment != null) {
            built.Append('#').Append(UrlEncoder.Encode(ReplacePlaceholders(_fragment), UrlEncoder.Component.FRAGMENT));
        }

        return new Uri(built.ToString(), UriKind.Absolute);
    }

    /// <summary>
    /// Converts the URL to a string
    /// </summary>
    [Pure]
    public override string ToString() => ToUrl().AbsoluteUri;

    [Pure]
    public static implicit operator Uri(UrlBuilder builder) => builder.ToUrl();

    [Pure]
    public static explicit operator string(UrlBuilder builder) => builder.ToString();

    /// Who the fuck uses title case booleans?
    private static string? Stringify(object? value) => value switch {
        true  => "true",
        false => "false",
        _     => value?.ToString()
    };

    #endregion

    #region Building

    [Pure]
    public UrlBuilder EnableTemplates(bool enableTemplates) => new(this) { _enableTemplates = enableTemplates };

    [Pure]
    public UrlBuilder UserInfo(string? userInfo) => new(this) { _userInfo = userInfo };

    [Pure]
    public UrlBuilder Path(string? segments, bool autoSplit = true) {
        ImmutableList<string> newPath = _path;
        if (segments is null) {
            return new UrlBuilder(this) { _path = ImmutableList<string>.Empty, _trailingSlash = false };
        } else if (segments.StartsWith('/')) {
            newPath = ImmutableList<string>.Empty;
        }

        if (autoSplit) {
            string[] paths = segments.Split(PATH_SEPARATORS, StringSplitOptions.RemoveEmptyEntries);
            newPath = newPath.AddRange(paths);
        } else {
            newPath = newPath.Add(segments);
        }

        return new UrlBuilder(this) { _path = newPath, _trailingSlash = segments.EndsWith('/') };
    }

    [Pure]
    public UrlBuilder Path(object segments) => Path(Stringify(segments), false);

    [Pure]
    public UrlBuilder Path(params string[] segments) => Path((IEnumerable<string>) segments);

    [Pure]
    public UrlBuilder Path(params IEnumerable<string> segments) => segments.Aggregate(this, (builder, segment) => builder.Path(segment));

    [Pure]
    public UrlBuilder Port(ushort? port) => new(this) { _port = port };

    [Pure]
    public UrlBuilder Hostname(string hostname) => new(this) { _hostname = hostname };

    [Pure]
    public UrlBuilder Scheme(string scheme) => new(this) { _scheme = scheme };

    [Pure]
    public UrlBuilder QueryParam(string key, object? value) => new(this) {
        _queryParameters = value != null
            ? _queryParameters.Add(new KeyValuePair<string, object>(key, Stringify(value) ?? string.Empty))
            : _queryParameters.RemoveAll(pair => pair.Key == key)
    };

    [Pure]
    public UrlBuilder QueryParam(string key, IEnumerable<object?> values) =>
        new(this) { _queryParameters = _queryParameters.AddRange(values.Compact().Select(v => new KeyValuePair<string, object>(key, Stringify(v) ?? string.Empty))) };

    [Pure]
    public UrlBuilder QueryParam(IEnumerable<KeyValuePair<string, string>> parameters) => new(this) {
        _queryParameters = _queryParameters.AddRange(parameters.Select(p => new KeyValuePair<string, object>(p.Key, p.Value)))
    };

    [Pure]
    public UrlBuilder QueryParam(IEnumerable<KeyValuePair<string, object?>>? parameters) => new(this) {
        _queryParameters = parameters is null
            ? ImmutableList<KeyValuePair<string, object>>.Empty
            : _queryParameters.AddRange(parameters.Compact().Select(pair => new KeyValuePair<string, object>(pair.Key, Stringify(pair.Value) ?? string.Empty)))
    };

    [Pure]
    public UrlBuilder Fragment(string? fragment) => new(this) { _fragment = fragment };

    // [Pure] public URIBuilder RemoveQueryParam(string? key) => new(this) { _queryParameters = _queryParameters.Where(pair => key == null || pair.Key != key).ToImmutableList() };

    #endregion

    #region Templates

    [Pure]
    public UrlBuilder ResolveTemplate(string key, object? value) => new(this) {
        _templateValues = _templateValues.SetItem(key, value),
        _unusedTemplateQueryParameterRealNames =
            _unusedTemplateQueryParameterRealNames != null && value != null ? _unusedTemplateQueryParameterRealNames.Remove(key) : _unusedTemplateQueryParameterRealNames
    };

    [Pure]
    public UrlBuilder ResolveTemplate(IEnumerable<KeyValuePair<string, object?>> values) {
        values = values.ToList();
        return new UrlBuilder(this) {
            _templateValues = _templateValues.SetItems(values),
            _unusedTemplateQueryParameterRealNames = _unusedTemplateQueryParameterRealNames != null ? _unusedTemplateQueryParameterRealNames.Except(values.ToImmutableDictionary().Keys)
                : _unusedTemplateQueryParameterRealNames
        };
    }

    [Pure]
    public UrlBuilder ResolveTemplate(object anonymousType) =>
        ResolveTemplate(anonymousType.GetType().GetProperties().Select(property => new KeyValuePair<string, object?>(property.Name, property.GetValue(anonymousType))));

    private string ReplacePlaceholders(string inputWithPlaceholders) =>
        _templateValues.IsEmpty || !_enableTemplates ? inputWithPlaceholders : PLACEHOLDER_PATTERN.Replace(inputWithPlaceholders, match => {
            string key = match.Groups["key"].Value;
            _templateValues.TryGetValue(key, out object? value);
            return Stringify(value) ?? string.Empty;
        });

    #endregion

}

internal static class UrlEncoder {

    private static readonly ArrayPool<byte> ESCAPING_UTF_BUFFERS = ArrayPool<byte>.Create(4, 50);

    public static string Encode(string raw, Component component) => component switch {
        Component.USER_INFO       => CharCategories.USER_INFO_ILLEGAL.Replace(raw, EscapeMatch),
        Component.PATH_SEGMENT    => CharCategories.PATH_SEGMENT_ILLEGAL.Replace(raw, EscapeMatch),
        Component.QUERY_PARAMETER => CharCategories.QUERY_PARAMETER_ILLEGAL.Replace(raw, EscapeMatch),
        // case UrlPart.SchemeSpecificPart:
        Component.FRAGMENT => CharCategories.URI_ILLEGAL.Replace(raw, EscapeMatch),
        _                  => CharCategories.URI_ILLEGAL.Replace(raw, EscapeMatch)
    };

    private static string EscapeMatch(Match match) {
        byte[] utf8Buffer = ESCAPING_UTF_BUFFERS.Rent(4);

        int utf8BytesUsed;
#if NET6_0_OR_GREATER
        utf8BytesUsed = Strings.UTF8.GetBytes(match.ValueSpan, utf8Buffer);
#else
        utf8BytesUsed = Strings.UTF8.GetBytes(match.Value, 0, match.Length, utf8Buffer, 0);
#endif

        string escaped = string.Join(null, utf8Buffer.Take(utf8BytesUsed).Select(b => $"%{b:X2}"));
        ESCAPING_UTF_BUFFERS.Return(utf8Buffer);
        return escaped;
    }

    private static class CharCategories {

        public static readonly Regex URI_ILLEGAL             = new(@"[^a-z0-9_\-!.~'()*,;:$&+=?/\[\]@]", RegexOptions.IgnoreCase);
        public static readonly Regex USER_INFO_ILLEGAL       = new(@"[^a-z0-9_\-!.~'()*,;:$&+=]", RegexOptions.IgnoreCase);
        public static readonly Regex PATH_SEGMENT_ILLEGAL    = new(@"[^a-z0-9_\-!.~'()*,;:$&+=@]", RegexOptions.IgnoreCase);
        public static readonly Regex QUERY_PARAMETER_ILLEGAL = new(@"[^a-z0-9_\-!.~'()*,;:$+=/\[\]@]", RegexOptions.IgnoreCase);

    }

    public enum Component {

        USER_INFO,
        PATH_SEGMENT,
        QUERY_PARAMETER,
        FRAGMENT

    }

}