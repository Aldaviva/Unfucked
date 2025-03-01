using System.Buffers;
using System.Collections.Immutable;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;

#pragma warning disable SYSLIB1045 // Generate regex at compile time - not possible with .NET Standard

namespace Unfucked;

public class URIBuilder: ICloneable {

    public static readonly object ValuelessQueryParam = new();

    private static readonly Regex  PlaceholderPattern           = new(@"\{(?<key>\w+?)\}");
    private static readonly char[] QueryParamSeparators         = ['&'];
    private static readonly char[] QueryParamKeyValueSeparators = ['='];
    private static readonly char[] PathSeparators               = ['/'];

    // ReSharper disable InconsistentNaming - don't collide with method names
    private string? _scheme { get; init; }
    private string? _schemeSpecificPart { get; init; }
    private string? _userInfo { get; init; }
    private string? _hostname { get; init; }
    private ushort? _port { get; init; }
    private ImmutableList<string> _path { get; init; } = ImmutableList<string>.Empty;
    private ImmutableList<KeyValuePair<string, object>> _queryParameters { get; init; } = ImmutableList<KeyValuePair<string, object>>.Empty;
    private string? _fragment { get; init; }
    private ImmutableDictionary<string, object?> _templateValues { get; init; } = ImmutableDictionary<string, object?>.Empty;
    private bool _enableTemplates { get; init; } = true;
    private ImmutableHashSet<string>? _unusedTemplateQueryParameterRealNames { get; init; }
    // ReSharper restore InconsistentNaming

    #region Construction

    public URIBuilder(string scheme, string schemeSpecificPart, string? fragment) {
        _scheme             = scheme;
        _schemeSpecificPart = schemeSpecificPart;
        _fragment           = fragment?.TrimStart(1, '#');
    }

    public URIBuilder(string scheme, string hostname, ushort? port = null) {
        _scheme   = scheme.TrimEnd(':');
        _hostname = hostname.TrimStart("//");
        _port     = port;
    }

    public URIBuilder(Uri uri) {
        _scheme   = uri.Scheme.EmptyToNull();
        _userInfo = uri.UserInfo.EmptyToNull();
        _hostname = uri.Host.EmptyToNull();
        _port     = uri.Port == -1 ? null : (ushort?) uri.Port;
        _path     = uri.Segments.SkipWhile(s => s == "/").Select(s => s.TrimEnd('/')).ToImmutableList();
        _queryParameters = uri.GetQuery() is var originalQuery
            ? originalQuery.Keys.Cast<string>().Select(k => new KeyValuePair<string, object>(k, originalQuery[k] ?? ValuelessQueryParam)).ToImmutableList()
            : ImmutableList<KeyValuePair<string, object>>.Empty;
        _fragment = uri.Fragment.TrimStart(1, '#').EmptyToNull();
    }

    public URIBuilder(UriBuilder uriBuilder) {
        _scheme   = uriBuilder.Scheme.EmptyToNull();
        _userInfo = uriBuilder.UserName.HasLength() || uriBuilder.Password.HasLength() ? $"{uriBuilder.UserName}:{uriBuilder.Password}" : null;
        _hostname = uriBuilder.Host.EmptyToNull();
        _port     = uriBuilder.Port == -1 ? null : (ushort?) uriBuilder.Port;
        _path     = uriBuilder.Path.TrimStart('/').Split(PathSeparators, StringSplitOptions.RemoveEmptyEntries).ToImmutableList();
        _queryParameters = uriBuilder.Query.TrimStart('?').Split(QueryParamSeparators, StringSplitOptions.RemoveEmptyEntries).Select(p => {
            string[] split = p.Split(QueryParamKeyValueSeparators, 2);
            return new KeyValuePair<string, object>(split[0], split.ElementAtOrDefault(1) ?? ValuelessQueryParam);
        }).ToImmutableList();
        _fragment = uriBuilder.Fragment.TrimStart(1, '#').EmptyToNull();
    }

    private URIBuilder(URIBuilder other) {
        _scheme                                = other._scheme;
        _schemeSpecificPart                    = other._schemeSpecificPart;
        _userInfo                              = other._userInfo;
        _hostname                              = other._hostname;
        _port                                  = other._port;
        _path                                  = other._path;
        _queryParameters                       = other._queryParameters;
        _fragment                              = other._fragment;
        _templateValues                        = other._templateValues;
        _enableTemplates                       = other._enableTemplates;
        _unusedTemplateQueryParameterRealNames = other._unusedTemplateQueryParameterRealNames;
    }

    public URIBuilder(string uri): this(new Uri(uri, UriKind.Absolute)) { }

    public static implicit operator URIBuilder(Uri uri) => new(uri);

    public static implicit operator URIBuilder(UriBuilder uri) => new(uri);

    public static explicit operator URIBuilder(string uri) => new(uri);

    public static URIBuilder FromTemplate(string uriTemplate) {
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
                        fakeName = GenerateFakeName();
                        fakeToRealTemplateNames.Add(fakeName, realName);
                        queryParameterRealNames.Add(realName);
                        replacement.Append(replacement.Length == 0 && prefix == "?" ? '?' : '&').Append(realName).Append('=').Append(fakeName);
                    }
                    return replacement.ToString();
                default:
                    fakeName = GenerateFakeName();
                    fakeToRealTemplateNames.Add(fakeName, match.Groups["names"].Value);
                    return $"{prefix}{fakeName}";
            }
        });

        URIBuilder uriBuilder = new(templateWithFakePlaceholders) { _unusedTemplateQueryParameterRealNames = queryParameterRealNames.ToImmutableHashSet() };
        if (RestoreRealName(uriBuilder._scheme) is { } realScheme) {
            uriBuilder = uriBuilder.Scheme(realScheme);
        }
        if (RestoreRealName(uriBuilder._schemeSpecificPart) is { } realSsp) {
            uriBuilder = uriBuilder.SchemeSpecificPart(realSsp);
        }
        if (RestoreRealName(uriBuilder._hostname) is { } realHostname) {
            uriBuilder = uriBuilder.Hostname(realHostname);
        }
        for (int i = 0; i < uriBuilder._path.Count; i++) {
            if (RestoreRealName(uriBuilder._path[i]) is { } realPathSegment) {
                uriBuilder = new URIBuilder(uriBuilder) { _path = uriBuilder._path.SetItem(i, realPathSegment) };
            }
        }
        for (int i = 0; i < uriBuilder._queryParameters.Count; i++) {
            if (uriBuilder._queryParameters[i].Value is string value && RestoreRealName(value) is { } realQueryParam) {
                uriBuilder = new URIBuilder(uriBuilder)
                    { _queryParameters = uriBuilder._queryParameters.SetItem(i, new KeyValuePair<string, object>(uriBuilder._queryParameters[i].Key, realQueryParam)) };
            }
        }
        if (RestoreRealName(uriBuilder._fragment) is { } realFragment) {
            uriBuilder = uriBuilder.Fragment(realFragment);
        }

        return uriBuilder;

        string GenerateFakeName() {
            string fakeName;
            do {
                fakeName = "template" + Cryptography.GenerateRandomString(16, alphabet);
            } while (uriTemplate.Contains(fakeName) || fakeToRealTemplateNames.ContainsKey(fakeName));
            return fakeName;
        }

        string? RestoreRealName(string? haystack) {
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
    public object Clone() => new URIBuilder(this);

    #endregion

    #region Serialization

    [Pure]
    public Uri ToUri() {
        StringBuilder built = new();

        if (_scheme != null) {
            built.Append(ReplacePlaceholders(_scheme)).Append(':');
        }

        if (_schemeSpecificPart != null) {
            built.Append(UriEncoder.Encode(ReplacePlaceholders(_schemeSpecificPart), UriEncoder.UriPart.SchemeSpecificPart));
        } else {
            if (_userInfo != null || _hostname != null || _port != null) {
                built.Append("//");
            }

            if (_userInfo != null) {
                built.Append(UriEncoder.Encode(ReplacePlaceholders(_userInfo), UriEncoder.UriPart.UserInfo)).Append('@');
            }

            if (_hostname is { } hostname) {
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
                    _path.Select(p => UriEncoder.Encode(ReplacePlaceholders(p.Trim('/')), UriEncoder.UriPart.PathSegment)));
            }

            IList<KeyValuePair<string, object>> queryParameters = _unusedTemplateQueryParameterRealNames != null
                ? _queryParameters.Where(pair => !_unusedTemplateQueryParameterRealNames.Contains(pair.Key)).ToList() : _queryParameters;
            if (queryParameters.Count != 0) {
                built.Append('?').AppendJoin('&', queryParameters.Select(pair => ReferenceEquals(pair.Value, ValuelessQueryParam)
                    ? UriEncoder.Encode(pair.Key, UriEncoder.UriPart.QueryParameter)
                    : $"{UriEncoder.Encode(pair.Key, UriEncoder.UriPart.QueryParameter)}={UriEncoder.Encode(ReplacePlaceholders(pair.Value.ToString() ?? string.Empty), UriEncoder.UriPart.QueryParameter)}"));
            }
        }

        if (_fragment != null) {
            built.Append('#').Append(UriEncoder.Encode(ReplacePlaceholders(_fragment), UriEncoder.UriPart.Fragment));
        }

        string uriString = built.ToString();
        return new Uri(uriString, UriKind.Absolute);
    }

    [Pure]
    public override string ToString() => ToUri().AbsoluteUri;

    [Pure]
    public static implicit operator Uri(URIBuilder builder) => builder.ToUri();

    [Pure]
    public static explicit operator string(URIBuilder builder) => builder.ToString();

    #endregion

    #region Building

    [Pure]
    public URIBuilder EnableTemplates(bool enableTemplates) => new(this) { _enableTemplates = enableTemplates };

    [Pure]
    public URIBuilder UserInfo(string? userInfo) => new(this) { _userInfo = userInfo };

    [Pure]
    public URIBuilder Path(string? segments, bool autoSplit = true) {
        ImmutableList<string> newPath = _path;
        if (segments is null) {
            return new URIBuilder(this) { _path = ImmutableList<string>.Empty };
        } else if (segments.StartsWith('/')) {
            newPath = ImmutableList<string>.Empty;
        }

        if (autoSplit) {
            string[] paths = segments.Split(PathSeparators, StringSplitOptions.RemoveEmptyEntries);
            newPath = newPath.AddRange(paths);
        } else {
            newPath = newPath.Add(segments);
        }

        return new URIBuilder(this) { _path = newPath };
    }

    [Pure]
    public URIBuilder Path(object segments) => Path(segments.ToString());

    [Pure]
    public URIBuilder Path(params IEnumerable<string> segments) => segments.Aggregate(this, (builder, segment) => builder.Path(segment));

    [Pure]
    public URIBuilder Port(ushort? port) => new(this) { _port = port };

    [Pure]
    public URIBuilder Hostname(string hostname) => new(this) { _hostname = hostname, _schemeSpecificPart = null };

    [Pure]
    public URIBuilder Scheme(string scheme) => new(this) { _scheme = scheme };

    [Pure]
    public URIBuilder SchemeSpecificPart(string ssp) => new(this) { _schemeSpecificPart = ssp, _hostname = null, _userInfo = null, _port = null };

    [Pure]
    public URIBuilder QueryParam(string key, object? value) => new(this) {
        _queryParameters = value != null
            ? _queryParameters.Add(new KeyValuePair<string, object>(key, value.ToString() ?? string.Empty))
            : _queryParameters.RemoveAll(pair => pair.Key == key)
    };

    [Pure]
    public URIBuilder QueryParam(string key, IEnumerable<object> values) =>
        new(this) { _queryParameters = _queryParameters.AddRange(values.Select(v => new KeyValuePair<string, object>(key, v.ToString() ?? string.Empty))) };

    [Pure]
    public URIBuilder QueryParam(IEnumerable<KeyValuePair<string, object>>? parameters) => new(this) {
        _queryParameters = parameters != null
            ? _queryParameters.AddRange(parameters.Select(p => new KeyValuePair<string, object>(p.Key, p.Value.ToString() ?? string.Empty)))
            : ImmutableList<KeyValuePair<string, object>>.Empty
    };

    [Pure]
    public URIBuilder Fragment(string? fragment) => new(this) { _fragment = fragment };

    // [Pure]
    // public URIBuilder RemoveQueryParam(string? key) => new(this) { _queryParameters = _queryParameters.Where(pair => key == null || pair.Key != key).ToImmutableList() };

    #endregion

    #region Templates

    [Pure]
    public URIBuilder ResolveTemplate(string key, object? value) => new(this) {
        _templateValues = _templateValues.SetItem(key, value),
        _unusedTemplateQueryParameterRealNames =
            _unusedTemplateQueryParameterRealNames != null && value != null ? _unusedTemplateQueryParameterRealNames.Remove(key) : _unusedTemplateQueryParameterRealNames
    };

    [Pure]
    public URIBuilder ResolveTemplate(IEnumerable<KeyValuePair<string, object?>> values) {
        values = values.ToList();
        return new URIBuilder(this) {
            _templateValues = _templateValues.SetItems(values),
            _unusedTemplateQueryParameterRealNames = _unusedTemplateQueryParameterRealNames != null ? _unusedTemplateQueryParameterRealNames.Except(values.ToImmutableDictionary().Keys)
                : _unusedTemplateQueryParameterRealNames
        };
    }

    private string ReplacePlaceholders(string inputWithPlaceholders) =>
        _templateValues.IsEmpty || !_enableTemplates ? inputWithPlaceholders : PlaceholderPattern.Replace(inputWithPlaceholders, match => {
            string key = match.Groups["key"].Value;
            _templateValues.TryGetValue(key, out object? value);
            return value?.ToString() ?? string.Empty;
        });

    #endregion

}

internal static class UriEncoder {

    private static readonly ArrayPool<byte> EscapingUtfBuffers = ArrayPool<byte>.Create(4, 50);

    public static string Encode(string raw, UriPart part) {
        switch (part) {
            case UriPart.UserInfo:
                return CharCategories.UserInfoIllegal.Replace(raw, EscapeMatch);
            case UriPart.PathSegment:
                return CharCategories.PathSegmentIllegal.Replace(raw, EscapeMatch);
            case UriPart.QueryParameter:
                return CharCategories.QueryParameterIllegal.Replace(raw, EscapeMatch);
            case UriPart.SchemeSpecificPart:
            case UriPart.Fragment:
            default:
                return CharCategories.UriIllegal.Replace(raw, EscapeMatch);
        }
    }

    private static string EscapeMatch(Match match) {
        byte[] utf8Buffer = EscapingUtfBuffers.Rent(4);

        int utf8BytesUsed;
#if NET6_0_OR_GREATER
        utf8BytesUsed = Strings.Utf8.GetBytes(match.ValueSpan, utf8Buffer);
#else
        utf8BytesUsed = Strings.Utf8.GetBytes(match.Value, 0, match.Length, utf8Buffer, 0);
#endif

        string escaped = string.Join(null, utf8Buffer.Take(utf8BytesUsed).Select(b => $"%{b:X2}"));
        EscapingUtfBuffers.Return(utf8Buffer);
        return escaped;
    }

    private static class CharCategories {

        // public static readonly ISet<char> Alphabet     = Range('A', 'z');
        // public static readonly ISet<char> Digit        = Range('0', '9');
        // public static readonly ISet<char> AlphaNumeric = Union(Alphabet, Digit);
        // public static readonly ISet<char> Unreserved   = Union(AlphaNumeric, Set("_-!.~'()*"));
        // public static readonly ISet<char> Punctuation  = Set(",;:$&+=");
        // public static readonly ISet<char> Reserved     = Union(Punctuation, Set("?/[]@"));

        // public static readonly Regex UriLegal        = new(@"(?:[a-z0-9_\-!.~'()*,;:$&+=?/\[\]@]|(?:%[0-9a-f]{2})|\P{Zs}|\P{Cc})*", RegexOptions.IgnoreCase);
        public static readonly Regex UriIllegal            = new(@"[^a-z0-9_\-!.~'()*,;:$&+=?/\[\]@]", RegexOptions.IgnoreCase);
        public static readonly Regex UserInfoIllegal       = new(@"[^a-z0-9_\-!.~'()*,;:$&+=]", RegexOptions.IgnoreCase);
        public static readonly Regex PathSegmentIllegal    = new(@"[^a-z0-9_\-!.~'()*,;:$&+=@]", RegexOptions.IgnoreCase);
        public static readonly Regex QueryParameterIllegal = new(@"[^a-z0-9_\-!.~'()*,;:$+=/\[\]@]", RegexOptions.IgnoreCase);

        // private static ISet<char> Set(string characters) => characters.ToFrozenSet();
        // private static ISet<char> Range(char first, char last) => Enumerable.Range((byte) first, last - first + 1).Select(i => (char) i).ToFrozenSet();
        // private static ISet<char> Union(params ISet<char>[] sets) => sets.Aggregate((IEnumerable<char>) [], (set1, set2) => set1.Union(set2), set => set.ToFrozenSet());

    }

    public enum UriPart {

        SchemeSpecificPart,
        UserInfo,
        PathSegment,
        QueryParameter,
        Fragment

    }

}

/*public static class UriBuilderExtensions {

    public static Task<HttpResponseMessage> DeleteAsync(this HttpClient client, URIBuilder uriBuilder, CancellationToken cancellationToken = default) {
        return client.DeleteAsync(uriBuilder.ToUri(), cancellationToken);
    }

    public static Task<HttpResponseMessage> GetAsync(this HttpClient client, URIBuilder uriBuilder, HttpCompletionOption completionOption = HttpCompletionOption.ResponseContentRead,
                                                     CancellationToken cancellationToken = default) {
        return client.GetAsync(uriBuilder.ToUri(), completionOption, cancellationToken);
    }

    public static Task<byte[]> GetByteArrayAsync(this HttpClient client, URIBuilder uriBuilder) {
        return client.GetByteArrayAsync(uriBuilder.ToUri());
    }

#if NET6_0_OR_GREATER
    public static Task<byte[]> GetByteArrayAsync(this HttpClient client, URIBuilder uriBuilder, CancellationToken cancellationToken) {
        return client.GetByteArrayAsync(uriBuilder.ToUri(), cancellationToken);
    }
#endif

    public static Task<Stream> GetStreamAsync(this HttpClient client, URIBuilder uriBuilder) {
        return client.GetStreamAsync(uriBuilder.ToUri());
    }

#if NET6_0_OR_GREATER
    public static Task<Stream> GetStreamAsync(this HttpClient client, URIBuilder uriBuilder, CancellationToken cancellationToken) {
        return client.GetStreamAsync(uriBuilder.ToUri(), cancellationToken);
    }
#endif

    public static Task<string> GetStringAsync(this HttpClient client, URIBuilder uriBuilder) {
        return client.GetStringAsync(uriBuilder.ToUri());
    }

#if NET6_0_OR_GREATER
    public static Task<string> GetStringAsync(this HttpClient client, URIBuilder uriBuilder, CancellationToken cancellationToken) {
        return client.GetStringAsync(uriBuilder.ToUri(), cancellationToken);
    }
#endif

    public static Task<HttpResponseMessage> PatchAsync(this HttpClient client, URIBuilder uriBuilder, HttpContent? content) {
        return client.PatchAsync(uriBuilder.ToUri(), content);
    }

#if NETSTANDARD2_1_OR_GREATER || NET6_0_OR_GREATER
    public static Task<HttpResponseMessage> PatchAsync(this HttpClient client, URIBuilder uriBuilder, HttpContent? content, CancellationToken cancellationToken) {
        return client.PatchAsync(uriBuilder.ToUri(), content, cancellationToken);
    }
#endif

    public static Task<HttpResponseMessage> PostAsync(this HttpClient client, URIBuilder uriBuilder, HttpContent? content, CancellationToken cancellationToken) {
        return client.PostAsync(uriBuilder.ToUri(), content, cancellationToken);
    }

    public static Task<HttpResponseMessage> PutAsync(this HttpClient client, URIBuilder uriBuilder, HttpContent? content, CancellationToken cancellationToken) {
        return client.PutAsync(uriBuilder.ToUri(), content, cancellationToken);
    }

#if NET6_0_OR_GREATER
    public static Task<HttpResponseMessage> PostAsJsonAsync<T>(this HttpClient client, URIBuilder uriBuilder, T value, JsonSerializerOptions? jsonSerializerOptions = null,
                                                               CancellationToken cancellationToken = default) {
        return HttpClientJsonExtensions.PostAsJsonAsync(client, uriBuilder.ToUri(), value, jsonSerializerOptions, cancellationToken);
    }

    public static Task<HttpResponseMessage> PutAsJsonAsync<T>(this HttpClient client, URIBuilder uriBuilder, T value, JsonSerializerOptions? jsonSerializerOptions = null,
                                                              CancellationToken cancellationToken = default) {
        return HttpClientJsonExtensions.PutAsJsonAsync(client, uriBuilder.ToUri(), value, jsonSerializerOptions, cancellationToken);
    }

    public static Task<T?> GetFromJsonAsync<T>(this HttpClient client, URIBuilder uriBuilder, JsonSerializerOptions? jsonSerializerOptions = null, CancellationToken cancellationToken = default) {
        return HttpClientJsonExtensions.GetFromJsonAsync<T>(client, uriBuilder.ToUri(), jsonSerializerOptions, cancellationToken);
    }

    public static Task<object?> GetFromJsonAsync(this HttpClient client, URIBuilder uriBuilder, Type type, JsonSerializerOptions? jsonSerializerOptions = null,
                                                 CancellationToken cancellationToken = default) {
        return HttpClientJsonExtensions.GetFromJsonAsync(client, uriBuilder.ToUri(), type, jsonSerializerOptions, cancellationToken);
    }

#endif

#if NET8_0_OR_GREATER
    public static Task<HttpResponseMessage> PatchAsJsonAsync<T>(this HttpClient client, URIBuilder uriBuilder, T value, JsonSerializerOptions? jsonSerializerOptions = null,
                                                                CancellationToken cancellationToken = default) {
        return HttpClientJsonExtensions.PatchAsJsonAsync(client, uriBuilder.ToUri(), value, jsonSerializerOptions, cancellationToken);
    }

    public static Task<T?> DeleteFromJsonAsync<T>(this HttpClient client, URIBuilder uriBuilder, JsonSerializerOptions? jsonSerializerOptions = null,
                                                  CancellationToken cancellationToken = default) {
        return HttpClientJsonExtensions.DeleteFromJsonAsync<T>(client, uriBuilder.ToUri(), jsonSerializerOptions, cancellationToken);
    }

    public static Task<object?> DeleteFromJsonAsync(this HttpClient client, URIBuilder uriBuilder, Type type, JsonSerializerOptions? jsonSerializerOptions = null,
                                                    CancellationToken cancellationToken = default) {
        return HttpClientJsonExtensions.DeleteFromJsonAsync(client, uriBuilder.ToUri(), type, jsonSerializerOptions, cancellationToken);
    }

    public static IAsyncEnumerable<T?> GetFromJsonAsAsyncEnumerable<T>(this HttpClient client, URIBuilder uriBuilder, JsonSerializerOptions? jsonSerializerOptions = null,
                                                                       CancellationToken cancellationToken = default) {
        return HttpClientJsonExtensions.GetFromJsonAsAsyncEnumerable<T>(client, uriBuilder.ToUri(), jsonSerializerOptions, cancellationToken);
    }

#endif

}*/