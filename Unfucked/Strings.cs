using System.Globalization;
using System.Text;

// ReSharper disable ReplaceSliceWithRangeIndexer - not in .NET Standard 2.0, which this project targets

namespace Unfucked;

/// <summary>
/// Methods that make it easier to work with strings.
/// </summary>
public static class Strings {

    internal static readonly UTF8Encoding Utf8 = new(false, true);

    /// <summary>
    /// Coerce strings with length 0 to <c>null</c>, which is easier to deal with than empty strings using null coalescing.
    /// </summary>
    /// <param name="str">A string that could be <c>null</c> or empty.</param>
    /// <returns>The input string or <c>null</c>, but never the empty string.</returns>
    /// <seealso cref="string.IsNullOrEmpty"/>
    [Pure]
    public static string? EmptyToNull(this string? str) => string.IsNullOrEmpty(str) ? null : str;

    /// <summary>
    /// Determine if a string contains any non-whitespace characters.
    /// </summary>
    /// <param name="str">A string that could be <c>null</c>, empty, or whitespace only.</param>
    /// <returns><c>true</c> if the input string contains at least one non-whitespace character, or <c>false</c> if it is <c>null</c>, empty, or consists only of whitespace characters.</returns>
    /// <seealso cref="string.IsNullOrWhiteSpace"/>
    [Pure]
    public static bool HasText(
#if NETCOREAPP3_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
        [NotNullWhen(true)]
#endif
        this string? str) => !string.IsNullOrWhiteSpace(str);

    /// <summary>
    /// Determine if a string contains any characters.
    /// </summary>
    /// <param name="str">A string that could be <c>null</c> or empty.</param>
    /// <returns><c>true</c> if the input string contains at least one character, or <c>false</c> if it is <c>null</c> or the empty string.</returns>
    /// <seealso cref="string.IsNullOrEmpty"/>
    [Pure]
    public static bool HasLength(
#if NETCOREAPP3_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
        [NotNullWhen(true)]
#endif
        this string? str) => !string.IsNullOrEmpty(str);

    /// <summary>
    /// Concatenate multiple strings together, with a separator between each one.
    /// </summary>
    /// <param name="strings">Strings to combine</param>
    /// <param name="separator">Optional string to appear between each pair of items in <paramref name="strings"/></param>
    /// <returns>A string that consists of each item in <paramref name="strings"/> concatenated together, with <paramref name="separator"/> between them.</returns>
    /// <seealso cref="string.Join(string?,IEnumerable{string?})"/>
    [Pure]
    public static string Join(this IEnumerable<string?> strings, string? separator = null) => string.Join(separator, strings);

    /// <summary>
    /// Concatenate multiple strings together, with a separator between each one.
    /// </summary>
    /// <param name="strings">Strings to combine</param>
    /// <param name="separator">Character to appear between each pair of items in <paramref name="strings"/></param>
    /// <returns>A string that consists of each item in <paramref name="strings"/> concatenated together, with <paramref name="separator"/> between them.</returns>
    /// <seealso cref="string.Join(string?,IEnumerable{string?})"/>
    [Pure]
    public static string Join(this IEnumerable<string?> strings, char separator) {
#if NETCOREAPP2_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
        return string.Join(separator, strings);
#else
        return string.Join(Convert.ToString(separator), strings);
#endif
    }

    /// <summary>
    /// Concatenate multiple objects together, with a separator between each one.
    /// </summary>
    /// <param name="objects">Objects to convert to strings and combine</param>
    /// <param name="separator">Optional string to appear between each pair of items in <paramref name="objects"/></param>
    /// <returns>A string that consists of each item in <paramref name="objects"/> converted to strings and concatenated together, with <paramref name="separator"/> between them.</returns>
    /// <seealso cref="string.Join{T}(string?,IEnumerable{T})"/>
    [Pure]
    public static string Join<T>(this IEnumerable<T?> objects, string? separator = null) => string.Join(separator, objects);

#pragma warning disable CS1574,CS1580
    /// <summary>
    /// Concatenate multiple strings together, with a separator between each one.
    /// </summary>
    /// <param name="objects">Objects to convert to strings to combine</param>
    /// <param name="separator">Character to appear between each pair of items in <paramref name="objects"/></param>
    /// <returns>A string that consists of each item in <paramref name="objects"/> converted to strings and concatenated together, with <paramref name="separator"/> between them.</returns>
    /// <seealso cref="string.Join{T}(char,IEnumerable{T})"/>
#pragma warning restore CS1574,CS1580
    [Pure]
    public static string Join<T>(this IEnumerable<T?> objects, char separator) {
#if NETCOREAPP2_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
        return string.Join(separator, objects);
#else
        return string.Join(Convert.ToString(separator), objects);
#endif
    }

    // ReSharper disable ReplaceSubstringWithRangeIndexer
    /// <summary>
    /// Converts the first character of a string to lowercase.
    /// </summary>
    /// <param name="str">A string that could contain an uppercase first letter</param>
    /// <param name="culture">The language information to use for case conversion</param>
    /// <returns>A string with the same characters as <paramref name="str"/> except the first character is lowercase, or <c>null</c> if <paramref name="str"/> is <c>null</c></returns>
    [Pure]
#if NETCOREAPP3_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
    [return: NotNullIfNotNull(nameof(str))]
#endif
    public static string? ToLowerFirstLetter(this string? str, CultureInfo? culture = null) =>
        string.IsNullOrEmpty(str) ? str : char.ToLower(str![0], culture ?? CultureInfo.InvariantCulture) + str.Substring(1);

    /// <summary>
    /// Converts the first character of a string to uppercase.
    /// </summary>
    /// <param name="str">A string that could contain an lowercase first letter</param>
    /// <param name="culture">The language information to use for case conversion</param>
    /// <returns>A string with the same characters as <paramref name="str"/> except the first character is uppercase, or <c>null</c> if <paramref name="str"/> is <c>null</c></returns>
    [Pure]
#if NETCOREAPP3_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
    [return: NotNullIfNotNull(nameof(str))]
#endif
    public static string? ToUpperFirstLetter(this string? str, CultureInfo? culture = null) =>
        string.IsNullOrEmpty(str) ? str : char.ToUpper(str![0], culture ?? CultureInfo.InvariantCulture) + str.Substring(1);
    // ReSharper restore ReplaceSubstringWithRangeIndexer

    /// <summary>
    /// Remove all occurrences of any of a given array of prefixes from the beginning of a string.
    /// </summary>
    /// <param name="str">The string to remove prefixes from.</param>
    /// <param name="prefixesToTrim">Array of prefixes, all of which will be removed from <paramref name="str"/>.</param>
    /// <returns>A substring of <paramref name="str"/> that does not start with any of the <paramref name="prefixesToTrim"/>.</returns>
    [Pure]
    public static string TrimStart(this string str, params IEnumerable<string> prefixesToTrim) => TrimStart(str.AsSpan(), -1, prefixesToTrim);

    /// <inheritdoc cref="TrimStart(string,int,IEnumerable{string})" />
    [Pure]
    public static string TrimStart(this string str, int limit = -1, params IEnumerable<string> prefixesToTrim) => TrimStart(str.AsSpan(), limit, prefixesToTrim);

    [Pure]
    public static string TrimStart(this string str, int limit = -1, params IEnumerable<char> prefixesToTrim) => TrimStart(str.AsSpan(), limit, prefixesToTrim.Select(c => c.ToString()));

    /// <inheritdoc cref="TrimStart(string,IEnumerable{string})" />
    [Pure]
    public static string TrimStart(this ReadOnlySpan<char> str, params IEnumerable<string> prefixesToTrim) => TrimStart(str, -1, prefixesToTrim);

    /// <summary>
    /// Remove some occurrences of any of a given array of prefixes from the beginning of a string.
    /// </summary>
    /// <param name="str">The string to remove prefixes from.</param>
    /// <param name="limit">The maximum number of occurrences to remove, or <c>-1</c> to remove all occurrences.</param>
    /// <param name="prefixesToTrim">Array of prefixes, all of which will be removed from <paramref name="str"/>.</param>
    /// <returns>A substring of <paramref name="str"/> that does not start with any of the <paramref name="prefixesToTrim"/>.</returns>
    [Pure]
    public static string TrimStart(this ReadOnlySpan<char> str, int limit = -1, params IEnumerable<string> prefixesToTrim) {
        int          startIndex    = 0;
        List<string> prefixToTrims = prefixesToTrim.ToList();
        uint         occurrences   = 0;
        while (limit == -1 || occurrences < limit) {
            bool found = false;
            foreach (string prefixToTrim in prefixToTrims) {
                int prefixLength = prefixToTrim.Length;
                if (prefixLength != 0 && startIndex + prefixLength <= str.Length && str.Slice(startIndex, prefixLength).SequenceEqual(prefixToTrim.AsSpan())) {
                    startIndex += prefixLength;
                    found      =  true;
                    occurrences++;
                    break;
                }
            }

            if (!found) {
                break;
            }
        }

        return str.Slice(startIndex).ToString();
    }

    /// <summary>
    /// Remove all occurrences of any of a given array of suffixes from the end of a string.
    /// </summary>
    /// <param name="str">The string to remove suffixes from.</param>
    /// <param name="suffixesToTrim">Sequence of suffixes, all of which will be removed from <paramref name="str"/>.</param>
    /// <returns>A substring of <paramref name="str"/> that does not end with any of the <paramref name="suffixesToTrim"/>.</returns>
    [Pure]
    public static string TrimEnd(this string str, params IEnumerable<string> suffixesToTrim) => TrimEnd(str.AsSpan(), -1, suffixesToTrim);

    [Pure]
    public static string TrimEnd(this string str, params IEnumerable<char> suffixesToTrim) => TrimEnd(str.AsSpan(), -1, suffixesToTrim.Select(c => c.ToString()));

    /// <inheritdoc cref="TrimEnd(string,int,IEnumerable{string})" />
    [Pure]
    public static string TrimEnd(this string str, int limit = -1, params IEnumerable<string> suffixesToTrim) => TrimEnd(str.AsSpan(), limit, suffixesToTrim);

    /// <inheritdoc cref="TrimEnd(string,IEnumerable{string})" />
    [Pure]
    public static string TrimEnd(this ReadOnlySpan<char> str, params IEnumerable<string> suffixesToTrim) => TrimEnd(str, -1, suffixesToTrim);

    /// <summary>
    /// Remove some occurrences of any of a given array of suffixes from the end of a string.
    /// </summary>
    /// <param name="str">The string to remove suffixes from.</param>
    /// <param name="limit">The maximum number of occurrences to remove, or <c>-1</c> to remove all occurrences.</param>
    /// <param name="suffixesToTrim">Sequence of suffixes, all of which will be removed from <paramref name="str"/>.</param>
    /// <returns>A substring of <paramref name="str"/> that does not end with any of the <paramref name="suffixesToTrim"/>.</returns>
    [Pure]
    public static string TrimEnd(this ReadOnlySpan<char> str, int limit = -1, params IEnumerable<string> suffixesToTrim) {
        int          endIndex      = str.Length;
        List<string> suffixToTrims = suffixesToTrim.ToList();
        uint         occurrences   = 0;
        while (limit == -1 || occurrences < limit) {
            bool found = false;
            foreach (string suffixToTrim in suffixToTrims) {
                int suffixLength = suffixToTrim.Length;
                if (suffixLength != 0 && endIndex >= 0 && endIndex - suffixLength >= 0 && str.Slice(endIndex - suffixLength, suffixLength).SequenceEqual(suffixToTrim.AsSpan())) {
                    endIndex -= suffixLength;
                    found    =  true;
                    occurrences++;
                    break;
                }
            }

            if (!found) {
                break;
            }
        }

        return str.Slice(0, endIndex).ToString();
    }

    /// <summary>
    /// Remove all occurrences of any of a given array of affixes from the beginning and end of a string.
    /// </summary>
    /// <param name="str">The string to remove affixes from.</param>
    /// <param name="affixesToTrim">Array of suffixes, all of which will be removed from <paramref name="str"/>.</param>
    /// <returns>A substring of <paramref name="str"/> that does neither starts nor ends with any of the <paramref name="affixesToTrim"/>.</returns>
    [Pure]
    public static string Trim(this string str, params IEnumerable<string> affixesToTrim) => Trim(str.AsSpan(), affixesToTrim);

    /// <inheritdoc cref="Trim(string,IEnumerable{string})" />
    [Pure]
    public static string Trim(this ReadOnlySpan<char> str, params IEnumerable<string> affixesToTrim) {
        List<string> prefixesToTrim = affixesToTrim.ToList();
        return TrimEnd(TrimStart(str, prefixesToTrim), prefixesToTrim);
    }

    /// <summary>
    /// Combine a sequence of objects into an English comma-separated list
    /// </summary>
    /// <param name="items">Sequence of objects</param>
    /// <param name="comma">String used to separate items in a list, <c>,</c> by default. A space will be inserted after each usage of this.</param>
    /// <param name="conjunction">A word that appears before the last item in a list of 3 or more items. This is <c>and</c> by default, but you can change it to <c>or</c> for a disjunction, or any other word.</param>
    /// <param name="oxfordComma"><c>true</c> (default) to include a <paramref name="comma"/> after the penultimate item in a list of 3 or more items, or <c>false</c> to omit it from this position.</param>
    /// <returns>English-style list of items, such as <c>A and B</c> or <c>A, B, and C</c></returns>
    [Pure]
    public static string JoinHumanized(this IEnumerable<object> items, string comma = ",", string conjunction = "and", bool oxfordComma = true) {
        using IEnumerator<object> enumerator = items.GetEnumerator();

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

    /// <summary>
    /// Serialize a string to a stream of bytes.
    /// </summary>
    /// <param name="str">The string to convert to bytes.</param>
    /// <param name="encoding">The character encoding to use, or <c>null</c> to use UTF-8.</param>
    /// <returns>A stream of bytes that represent <paramref name="str"/> in the given <paramref name="encoding"/>.</returns>
    [Pure]
    public static Stream ToByteStream(this string str, Encoding? encoding = null) => new MemoryStream((encoding ?? Utf8).GetBytes(str), false);

    /// <summary>
    /// Serialize a string to an array of bytes.
    /// </summary>
    /// <param name="str">The string to convert to bytes.</param>
    /// <param name="encoding">The character encoding to use, or <c>null</c> to use UTF-8.</param>
    /// <returns>An array of bytes that represent <paramref name="str"/> in the given <paramref name="encoding"/>.</returns>
    [Pure]
    public static byte[] ToByteArray(this string str, Encoding? encoding = null) => (encoding ?? Utf8).GetBytes(str);

    /// <summary>
    /// Replace Windows-style line endings (CRLF, <c>\r\n</c>, 0x0d 0x0a) with Unix-style line endings (LF, <c>\n</c>, 0x0a)
    /// </summary>
    /// <param name="dosString">String that may contain <c>\r\n</c></param>
    /// <returns>A copy of <paramref name="dosString"/> that has all of its occurrences of <c>\r\n</c> replaced with <c>\n</c></returns>
    [Pure]
#if NETCOREAPP3_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
    [return: NotNullIfNotNull(nameof(dosString))]
#endif
    public static string? Dos2Unix(this string? dosString) => dosString?.Replace("\r\n", "\n");

    /// <summary>
    /// Repeat the string multiple times.
    /// </summary>
    /// <param name="str">The string to be repeated</param>
    /// <param name="count">The total number of times <paramref name="str"/> should appear in the returned string</param>
    /// <returns>A string that consists of <paramref name="count"/> copies of <paramref name="str"/> concatenated together</returns>
    public static string Repeat(this string str, uint count) {
        switch (count) {
            case 0:
                return string.Empty;
            case 1:
                return str;
            case 2:
                return str + str;
            case 3:
                return str + str + str;
            case 4:
                return str + str + str + str;
            // At count ≥ 5, concatenation is not faster than a pre-allocated StringBuilder
            default:
                StringBuilder stringBuilder = new((int) (str.Length * count));
                for (int i = 0; i < count; i++) {
                    stringBuilder.Append(str);
                }

                return stringBuilder.ToString();
        }
    }

    public static StringBuilder AppendJoin(this StringBuilder builder, char separator, params IEnumerable<object> values) {
#if NETCOREAPP2_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
        return builder.AppendJoin(separator, values);
#else
        bool firstValue = true;
        foreach (object value in values) {
            if (firstValue) {
                firstValue = false;
            } else {
                builder.Append(separator);
            }
            builder.Append(value);
        }
        return builder;
#endif
    }

    public static StringBuilder AppendJoin(this StringBuilder builder, string separator, params IEnumerable<object> values) {
#if NETCOREAPP2_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
        return builder.AppendJoin(separator, values);
#else
        bool firstValue = true;
        foreach (object value in values) {
            if (firstValue) {
                firstValue = false;
            } else {
                builder.Append(separator);
            }
            builder.Append(value);
        }
        return builder;
#endif
    }

    public static bool StartsWith(this string str, char prefix) =>
#if NETCOREAPP2_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
        str.StartsWith(prefix);
#else
        str.StartsWith(prefix.ToString());
#endif

    public static bool EndsWith(this string str, char suffix) =>
#if NETCOREAPP2_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
        str.EndsWith(suffix);
#else
        str.EndsWith(suffix.ToString());
#endif

    public static bool Contains(this string str, string value, StringComparison comparisonType) =>
#if NETCOREAPP2_1_OR_GREATER || NETSTANDARD2_1_OR_GREATER
        str.Contains(value, comparisonType);
#else
        str.IndexOf(value, comparisonType) != -1;
#endif

}