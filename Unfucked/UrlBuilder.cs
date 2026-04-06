using System.Collections.Immutable;
using System.Collections.Specialized;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;

// ReSharper disable UseIndexFromEndExpression - incompatible with .NET Standard 2.0, which this project obviously targets

#pragma warning disable SYSLIB1045 // Generate regex at compile time - not possible with .NET Standard

namespace Unfucked;

/// <summary>
/// Programmatically construct and manipulate a URL. Like <see cref="UriBuilder"/> except it understands path segments, query parameters, and templates.
/// </summary>
public sealed class UrlBuilder {

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

    /// <summary>
    /// Construct a new <see cref="UrlBuilder"/> with a given scheme, hostname, and optional port number.
    /// </summary>
    /// <param name="scheme">URL scheme/protocol</param>
    /// <param name="hostname">URL hostname/domain name/host/server name/FQDN</param>
    /// <param name="port">URL port number, or <c>null</c> to leave it blank</param>
    public UrlBuilder(string scheme, string hostname, ushort? port = null) {
        _scheme   = scheme.TrimEnd(':');
        _hostname = hostname.TrimStart("//");
        _port     = port;
    }

    /// <summary>
    /// Construct a <see cref="UrlBuilder"/> based on an existing <see cref="Uri"/>.
    /// </summary>
    /// <param name="uri">URI to populate builder with.</param>
    public UrlBuilder(Uri uri) {
        NameValueCollection originalQuery = uri.QueryParams;
        _scheme          = uri.Scheme.EmptyToNull;
        _userInfo        = uri.UserInfo.EmptyToNull;
        _hostname        = uri.Host.EmptyToNull;
        _port            = uri.Port == -1 ? null : (ushort?) uri.Port;
        _path            = uri.Segments.SkipWhile(static s => s == "/").Select(static s => s.TrimEnd('/')).ToImmutableList();
        _trailingSlash   = uri.Segments.LastOrDefault()?.EndsWith('/') ?? false;
        _queryParameters = originalQuery.Keys.Cast<string>().Select(k => new KeyValuePair<string, object>(k, originalQuery[k] ?? VALUELESS_QUERY_PARAM)).ToImmutableList();
        _fragment        = uri.Fragment.TrimStart(1, '#').EmptyToNull;
    }

    /// <summary>
    /// Construct a <see cref="UrlBuilder"/> based on an existing <see cref="UriBuilder"/>.
    /// </summary>
    /// <param name="uriBuilder"><see cref="UriBuilder"/> to populate builder with.</param>
    public UrlBuilder(UriBuilder uriBuilder) {
        _scheme        = uriBuilder.Scheme.EmptyToNull;
        _userInfo      = uriBuilder.UserName.HasLength || uriBuilder.Password.HasLength ? $"{uriBuilder.UserName}:{uriBuilder.Password}" : null;
        _hostname      = uriBuilder.Host.EmptyToNull;
        _port          = uriBuilder.Port == -1 ? null : (ushort?) uriBuilder.Port;
        _path          = uriBuilder.Path.TrimStart('/').Split(PATH_SEPARATORS, StringSplitOptions.RemoveEmptyEntries).ToImmutableList();
        _trailingSlash = uriBuilder.Path.EndsWith('/');
        _queryParameters = uriBuilder.Query.TrimStart('?').Split(QUERY_PARAM_SEPARATORS, StringSplitOptions.RemoveEmptyEntries).Select(static p => {
            string[] split = p.Split(QUERY_PARAM_KEY_VALUE_SEPARATORS, 2);
            return new KeyValuePair<string, object>(split[0], split.ElementAtOrDefault(1) ?? VALUELESS_QUERY_PARAM);
        }).ToImmutableList();
        _fragment = uriBuilder.Fragment.TrimStart(1, '#').EmptyToNull;
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

    /// <inheritdoc cref="UrlBuilder(Uri)" />
    public static implicit operator UrlBuilder(Uri uri) => new(uri);

    /// <inheritdoc cref="UrlBuilder(UriBuilder)" />
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

    #endregion

    #region Serialization

    /// <summary>
    /// Build a URL from this builder's state.
    /// </summary>
    /// <returns>The URL representation of this builder instance.</returns>
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
            built.Append(UrlEncoder.Encode(ReplacePlaceholders(_userInfo), UrlEncoder.Component.UserInfo)).Append('@');
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
                _path.Select(p => UrlEncoder.Encode(ReplacePlaceholders(p.Trim('/')), UrlEncoder.Component.PathSegment)));

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
                ? UrlEncoder.Encode(pair.Key, UrlEncoder.Component.QueryParameter)
                : $"{UrlEncoder.Encode(pair.Key, UrlEncoder.Component.QueryParameter)}={UrlEncoder.Encode(ReplacePlaceholders(Stringify(pair.Value) ?? string.Empty), UrlEncoder.Component.QueryParameter)}"));
        }

        if (_fragment != null) {
            built.Append('#').Append(UrlEncoder.Encode(ReplacePlaceholders(_fragment), UrlEncoder.Component.Fragment));
        }

        return new Uri(built.ToString(), UriKind.Absolute);
    }

    /// <inheritdoc cref="ToUrl" />
    [Pure]
    public override string ToString() => ToUrl().AbsoluteUri;

    /// <summary>
    /// Implicitly cast a <see cref="UrlBuilder"/> to a <see cref="Uri"/> by building its URL.
    /// </summary>
    /// <param name="builder">A URL builder</param>
    [Pure]
    public static implicit operator Uri(UrlBuilder builder) => builder.ToUrl();

    /// <inheritdoc cref="op_Implicit(Unfucked.UrlBuilder)"/>
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

    /// <summary>
    /// Set whether template <c>{placeholders}</c> are allowed in this URL builder. By default, this is enabled. You may want to disable this if you want literal curly brace <c>{}</c> characters to appear in your URLs, and don't want to use templates.
    /// </summary>
    /// <param name="enableTemplates"><c>true</c> to enable replacing templates in the builder with values when building, or <c>false</c> to leave them as the literal <c>{placeholders}</c>.</param>
    /// <returns>New immutable builder instance with the changed value.</returns>
    [Pure]
    public UrlBuilder EnableTemplates(bool enableTemplates) => new(this) { _enableTemplates = enableTemplates };

    /// <summary>
    /// Set the user info (such as <c>username:password</c>) in the URL authority.
    /// </summary>
    /// <param name="userInfo">User authentication, or <c>null</c> to clear an existing value.</param>
    /// <returns>New immutable builder instance with the changed value.</returns>
    [Pure]
    public UrlBuilder UserInfo(string? userInfo) => new(this) { _userInfo = userInfo };

    /// <summary>
    /// Add URL path segments, also called pathname, script info, path info, filePath, or directory/fileName.
    /// </summary>
    /// <param name="segments">New path suffix to append to this request's URL path. To replace instead of append, make this start with <c>/</c>. To remove, pass <c>null</c>.</param>
    /// <param name="autoSplit">If <c>true</c> (default), <paramref name="segments"/> will be split by <c>/</c> into multiple path segments, all of them will be appended, and the <c>/</c> separators won't be URL-encoded into <c>%2F</c>. Otherwise, if <c>false</c>, <paramref name="segments"/> will be appended as one big path segment, and any <c>/</c> separators inside it will be URL-encoded as <c>%2F</c>. You should set this to <c>false</c> if <paramref name="segments"/> is user-supplied.</param>
    /// <returns>New immutable builder instance with the changed value.</returns>
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

    /// <summary>
    /// Add URL path segments, also called pathname, script info, path info, filePath, or directory/fileName.
    /// </summary>
    /// <param name="segments">New path suffix to append to this request's URL path. To replace instead of append, make this start with <c>/</c>.</param>
    /// <returns>New immutable builder instance with the changed value.</returns>
    [Pure]
    public UrlBuilder Path(object segments) => Path(Stringify(segments), false);

    /// <inheritdoc cref="Path(IEnumerable{string})" />
    [Pure]
    public UrlBuilder Path(params string[] segments) => Path((IEnumerable<string>) segments);

    /// <summary>
    /// Add URL path segments, also called pathname, script info, path info, filePath, or directory/fileName.
    /// </summary>
    /// <param name="segments"><para>New path suffixes to append to this request's URL path.</para>
    /// <para>To replace instead of append, make the first segment start with <c>/</c>.</para>
    /// <para>Each segment is also split on <c>/</c> into multiple segments; to disable this (including when one of <paramref name="segments"/> is untrusted), call <see cref="Path(string?,bool)"/> instead.</para></param>
    /// <returns>New immutable builder instance with the changed value.</returns>
    [Pure]
    public UrlBuilder Path(params IEnumerable<string> segments) => segments.Aggregate(this, static (builder, segment) => builder.Path(segment));

    /// <summary>
    /// Set the URL host port.
    /// </summary>
    /// <param name="port">IP port number, or <c>null</c> to remove and use the default for the scheme.</param>
    /// <returns>New immutable builder instance with the changed value.</returns>
    [Pure]
    public UrlBuilder Port(ushort? port) => new(this) { _port = port };

    /// <summary>
    /// Set the URL hostname (sometimes incorrectly called the host which can include a port number).
    /// </summary>
    /// <param name="hostname">Domain name or IP address. Must not include a port number; to set that, call <see cref="Port"/> instead. For an IPv6 address, enclose it in square brackets (like <c>[::1]</c>).</param>
    /// <returns>New immutable builder instance with the changed value.</returns>
    [Pure]
    public UrlBuilder Hostname(string hostname) => new(this) { _hostname = hostname };

    /// <summary>
    /// Set the URL scheme (sometimes incorrectly called the protocol).
    /// </summary>
    /// <param name="scheme">URL scheme, such as <c>https</c> or <c>http</c></param>
    /// <returns>New immutable builder instance with the changed value.</returns>
    [Pure]
    public UrlBuilder Scheme(string scheme) => new(this) { _scheme = scheme };

    /// <summary>
    /// <para>Add a query parameter to the URL (sometimes incorrectly called a GET parameter, search, or parameter to the exclusion of other types of parameters like path parameters).</para>
    /// <para>If a parameter with the same <paramref name="key"/> already exists in this URL, the new <paramref name="value"/> will be added such that the URL will have multiple occurrences of the <paramref name="key"/>, for example <c>?a=b&amp;a=c</c>.</para>
    /// </summary>
    /// <param name="key">Name of the parameter.</param>
    /// <param name="value">Value of the parameter, or <c>null</c> to remove all query parameters which have their name set to <paramref name="key"/> from this URL.</param>
    /// <returns>New immutable builder instance with the changed value.</returns>
    [Pure]
    public UrlBuilder QueryParam(string key, object? value) => new(this) {
        _queryParameters = value != null
            ? _queryParameters.Add(new KeyValuePair<string, object>(key, Stringify(value) ?? string.Empty))
            : _queryParameters.RemoveAll(pair => pair.Key == key)
    };

    /// <summary>
    /// <para>Add query parameters to the URL (sometimes incorrectly called GET parameters, search, or just parameters to the exclusion of other types of parameters like path parameters).</para>
    /// <para>If a parameter with the same <paramref name="key"/> already exists in this URL, the new <paramref name="values"/> will be added such that the URL will have multiple occurrences of the <paramref name="key"/>, for example <c>?a=b&amp;a=c&amp;a=d</c>.</para>
    /// </summary>
    /// <param name="key">Name of the parameter.</param>
    /// <param name="values">Multiple values to add, all with the same <paramref name="key"/>. Any <c>null</c> values will be ignored.</param>
    /// <returns>New immutable builder instance with the changed value.</returns>
    [Pure]
    public UrlBuilder QueryParam(string key, IEnumerable<object?> values) =>
        new(this) { _queryParameters = _queryParameters.AddRange(values.Compact().Select(v => new KeyValuePair<string, object>(key, Stringify(v) ?? string.Empty))) };

    /// <summary>
    /// <para>Add query parameters to the URL (sometimes incorrectly called GET parameters, search, or just parameters to the exclusion of other types of parameters like path parameters).</para>
    /// <para>If parameters with the same key already exist in this URL, the new parameters will be added such that the URL will have multiple occurrences of the key, for example <c>?a=b&amp;a=c&amp;a=d</c>.</para>
    /// </summary>
    /// <param name="parameters">Multiple key-value pairs to add. Parameters with <c>null</c> values will be ignored. If this entire argument is <c>null</c>, then all query parameters will be removed from this URL.</param>
    /// <returns>New immutable builder instance with the changed value.</returns>
    [Pure]
    public UrlBuilder QueryParam(IEnumerable<KeyValuePair<string, string>>? parameters) => new(this) {
        _queryParameters = parameters is null
            ? ImmutableList<KeyValuePair<string, object>>.Empty
            : _queryParameters.AddRange(parameters.Select(static p => new KeyValuePair<string, object>(p.Key, p.Value)))
    };

    /// <inheritdoc cref="QueryParam(IEnumerable{KeyValuePair{string,string}})" />
    [Pure]
    public UrlBuilder QueryParam(IEnumerable<KeyValuePair<string, object?>>? parameters) => new(this) {
        _queryParameters = parameters is null
            ? ImmutableList<KeyValuePair<string, object>>.Empty
            : _queryParameters.AddRange(parameters.Compact().Select(static pair => new KeyValuePair<string, object>(pair.Key, Stringify(pair.Value) ?? string.Empty)))
    };

    /// <summary>
    /// Set the URL fragment, also called the hash, fragment id, or ref.
    /// </summary>
    /// <param name="fragment">The new fragment, without the leading <c>#</c> (if you include it, it will be URL-encoded after the read <c>#</c>). Any existing fragment will be replaced. To remove a fragment from the URL, pass <c>null</c>.</param>
    /// <returns>New immutable builder instance with the changed value.</returns>
    [Pure]
    public UrlBuilder Fragment(string? fragment) => new(this) { _fragment = fragment };

    #endregion

    #region Templates

    /// <summary>
    /// <para>Fill in placeholder values in the URL, which are keys surrounded by single curly braces, like <c>{key}</c>.</para>
    /// <para>Also useful for parameters whose values include curly braces, such as query parameters whose value is a JSON object. To avoid the JSON object's braces being treated as a placeholder, pass the JSON object using a template: <c>client.Target(url).QueryParam("value", "{jsonValue}").ResolveTemplate("jsonValue", JsonSerializer.Serialize(obj))</c>.</para>
    /// </summary>
    /// <param name="key">Placeholder name, without the surrounding curly braces.</param>
    /// <param name="value">The value to replace all occurrences of <c>{key}</c> with in the URL. Missing or <c>null</c> values will be replaced by the empty string.</param>
    /// <returns>New immutable builder instance with the changed value.</returns>
    [Pure]
    public UrlBuilder ResolveTemplate(string key, object? value) => new(this) {
        _templateValues = _templateValues.SetItem(key, value),
        _unusedTemplateQueryParameterRealNames =
            _unusedTemplateQueryParameterRealNames != null && value != null ? _unusedTemplateQueryParameterRealNames.Remove(key) : _unusedTemplateQueryParameterRealNames
    };

    /// <summary>
    /// <para>Fill in placeholder values in the URL, which are keys surrounded by single curly braces, like <c>{key}</c>.</para>
    /// </summary>
    /// <param name="values">Key-value pairs of placeholder names and replacement values. Missing or <c>null</c> values will be replaced by the empty string.</param>
    /// <returns>New immutable builder instance with the changed value.</returns>
    [Pure]
    public UrlBuilder ResolveTemplate(IEnumerable<KeyValuePair<string, object?>> values) {
        values = values.ToList();
        return new UrlBuilder(this) {
            _templateValues = _templateValues.SetItems(values),
            _unusedTemplateQueryParameterRealNames = _unusedTemplateQueryParameterRealNames != null ? _unusedTemplateQueryParameterRealNames.Except(values.ToImmutableDictionary().Keys)
                : _unusedTemplateQueryParameterRealNames
        };
    }

    /// <summary>
    /// <para>Fill in placeholder values in the URL, which are keys surrounded by single curly braces, like <c>{key}</c>.</para>
    /// <para>Example: <c>client.Target(url).Path("{a}").QueryParam("b", "{b}").ResolveTemplate(new { a = 1, b = 2 }).Get&lt;string&gt;()</c></para>
    /// </summary>
    /// <param name="anonymousType">An anonymous object which contains properties that will be used to resolve template placeholders. Each property name represents the placeholder key, and the placeholder will be replaced with the property's value.</param>
    /// <returns>New immutable builder instance with the changed value.</returns>
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

    public static string Encode(string raw, Component component) => component switch {
        Component.UserInfo       => CharCategories.UserInfoIllegal.Replace(raw, EscapeMatch),
        Component.PathSegment    => CharCategories.PathSegmentIllegal.Replace(raw, EscapeMatch),
        Component.QueryParameter => CharCategories.QueryParameterIllegal.Replace(raw, EscapeMatch),
        _                        => CharCategories.URIIllegal.Replace(raw, EscapeMatch)
    };

    private static string EscapeMatch(Match match) {
        byte[] utf8Buffer = new byte[4];

        int utf8BytesUsed =
#if NET6_0_OR_GREATER
            Strings.Utf8.GetBytes(match.ValueSpan, utf8Buffer);
#else
            Strings.Utf8.GetBytes(match.Value, 0, match.Length, utf8Buffer, 0);
#endif

        return string.Join(null, utf8Buffer.Take(utf8BytesUsed).Select(static b => $"%{b:X2}"));
    }

    private static class CharCategories {

        public static readonly Regex URIIllegal            = new(@"[^a-z0-9_\-!.~'()*,;:$&+=?/\[\]@]", RegexOptions.IgnoreCase);
        public static readonly Regex UserInfoIllegal       = new(@"[^a-z0-9_\-!.~'()*,;:$&+=]", RegexOptions.IgnoreCase);
        public static readonly Regex PathSegmentIllegal    = new(@"[^a-z0-9_\-!.~'()*,;:$&+=@]", RegexOptions.IgnoreCase);
        public static readonly Regex QueryParameterIllegal = new(@"[^a-z0-9_\-!.~'()*,;:$+=/\[\]@]", RegexOptions.IgnoreCase);

    }

    public enum Component {

        UserInfo,
        PathSegment,
        QueryParameter,
        Fragment

    }

}