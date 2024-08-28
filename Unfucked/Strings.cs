using System.Globalization;
using System.Text;

namespace Unfucked;

public static class Strings {

    private static readonly UTF8Encoding DefaultEncoding = new(false, true);

    [Pure]
    public static string? EmptyToNull(this string? source) => string.IsNullOrEmpty(source) ? null : source;

    /// <summary>
    /// Indicates whether a specified string is <c>null</c>, empty, or consists only of white-space characters.
    /// </summary>
    /// <param name="source">The string to test</param>
    /// <returns><c>false</c> if the <paramref name="source"/> parameter is <c>null</c> or <see cref="string.Empty"/>, or if <paramref name="source"/> consists exclusively of whitespace characters; <c>true</c> otherwise.</returns>
    /// <seealso cref="string.IsNullOrWhiteSpace"/>
    [Pure]
    public static bool HasText(
#if NETSTANDARD2_1_OR_GREATER || NET6_0_OR_GREATER
        [NotNullWhen(true)]
#endif
        this string? source) => !string.IsNullOrWhiteSpace(source);

    [Pure]
    public static bool HasLength(
#if NETSTANDARD2_1_OR_GREATER || NET6_0_OR_GREATER
        [NotNullWhen(true)]
#endif
        this string? source) => !string.IsNullOrEmpty(source);

    [Pure]
    public static string Join(this IEnumerable<string?> source, string? separator) => string.Join(separator, source);

    [Pure]
    public static string Join(this IEnumerable<string?> source, char separator) {
#if NETSTANDARD2_1_OR_GREATER || NET6_0_OR_GREATER
        return string.Join(separator, source);
#else
        return string.Join(Convert.ToString(separator), source);
#endif
    }

    [Pure]
#if NETSTANDARD2_1_OR_GREATER || NET6_0_OR_GREATER
    [return: NotNullIfNotNull(nameof(source))]
#endif
    public static string? ToLowerFirstLetter(this string? source, CultureInfo? culture = null) =>
        string.IsNullOrEmpty(source) ? source : char.ToLower(source[0], culture ?? CultureInfo.InvariantCulture) + source.Substring(1);

    [Pure]
#if NETSTANDARD2_1_OR_GREATER || NET6_0_OR_GREATER
    [return: NotNullIfNotNull(nameof(source))]
#endif
    public static string? ToUpperFirstLetter(this string? source, CultureInfo? culture = null) =>
        string.IsNullOrEmpty(source) ? source : char.ToUpper(source[0], culture ?? CultureInfo.InvariantCulture) + source.Substring(1);

#if NETSTANDARD2_1_OR_GREATER || NET6_0_OR_GREATER
    [Pure]
    public static string TrimStart(this string source, params string[] preficesToTrim) => TrimStart(source.AsSpan(), preficesToTrim);

    [Pure]
    public static string TrimStart(this ReadOnlySpan<char> source, params string[] preficesToTrim) {
        int startIndex = 0;
        while (true) {
            bool found = false;
            foreach (string prefixToTrim in preficesToTrim) {
                int prefixLength = prefixToTrim.Length;
                if (prefixLength != 0 && startIndex + prefixLength <= source.Length && source[startIndex..(startIndex + prefixLength)].SequenceEqual(prefixToTrim)) {
                    startIndex += prefixLength;
                    found      =  true;
                    break;
                }
            }

            if (!found) {
                break;
            }
        }

        return source[startIndex..].ToString();
    }

    [Pure]
    public static string TrimEnd(this string source, params string[] sufficesToTrim) => TrimEnd(source.AsSpan(), sufficesToTrim);

    [Pure]
    public static string TrimEnd(this ReadOnlySpan<char> source, params string[] sufficesToTrim) {
        int endIndex = source.Length;
        while (true) {
            bool found = false;
            foreach (string suffixToTrim in sufficesToTrim) {
                int suffixLength = suffixToTrim.Length;
                if (suffixLength != 0 && endIndex >= 0 && endIndex - suffixLength >= 0 && source[(endIndex - suffixLength)..endIndex].SequenceEqual(suffixToTrim)) {
                    endIndex -= suffixLength;
                    found    =  true;
                    break;
                }
            }

            if (!found) {
                break;
            }
        }

        return source[..endIndex].ToString();
    }

    [Pure]
    public static string Trim(this string source, params string[] affices) => Trim(source.AsSpan(), affices);

    [Pure]
    public static string Trim(this ReadOnlySpan<char> source, params string[] affices) => TrimEnd(TrimStart(source, affices), affices);
#endif

    [Pure]
    public static string JoinHumanized(this IEnumerable<object> source, string comma = ",", string conjunction = "and", bool oxfordComma = true) {
        using IEnumerator<object> enumerator = source.GetEnumerator();

        if (!enumerator.MoveNext()) {
            return string.Empty;
        }

        object? first = enumerator.Current;
        if (!enumerator.MoveNext()) {
            return first?.ToString() ?? string.Empty;
        }

        object? second = enumerator.Current;
        if (!enumerator.MoveNext()) {
            return $"{first} {conjunction} {second}";
        }

        object?       third         = enumerator.Current;
        const char    space         = ' ';
        StringBuilder stringBuilder = new StringBuilder().Append(first).Append(comma).Append(space);

        while (enumerator.MoveNext()) {
            first  = second;
            second = third;
            third  = enumerator.Current;
            stringBuilder.Append(first).Append(comma).Append(space);
        }

        stringBuilder.Append(second);
        if (oxfordComma) {
            stringBuilder.Append(comma);
        }
        stringBuilder.Append(space).Append(conjunction).Append(space).Append(third);

        return stringBuilder.ToString();
    }

    [Pure]
    public static Stream ToStream(this string source, Encoding? encoding = null) => new MemoryStream((encoding ?? DefaultEncoding).GetBytes(source), false);

    [Pure]
    public static byte[] ToBytes(this string source, Encoding? encoding = null) => (encoding ?? DefaultEncoding).GetBytes(source);

    /// <summary>
    /// Replace Windows-style line endings (CRLF, <c>\r\n</c>, 0x0d 0x0a) with Unix-style line endings (LF, <c>\n</c>, 0x0a)
    /// </summary>
    /// <param name="dosString">String that may contain <c>\r\n</c></param>
    /// <returns>A copy of <paramref name="dosString"/> that has all of its occurrences of <c>\r\n</c> replaced with <c>\n</c></returns>
    [Pure]
#if NETSTANDARD2_1_OR_GREATER || NET6_0_OR_GREATER
    [return: NotNullIfNotNull(nameof(dosString))]
#endif
    public static string? Dos2Unix(this string? dosString) => dosString?.Replace("\r\n", "\n");

}