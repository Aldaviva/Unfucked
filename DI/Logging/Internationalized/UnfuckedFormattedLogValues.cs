using System.Collections;
using System.Collections.Concurrent;

// ReSharper disable All

namespace Unfucked.Logging.Internationalized;

/**
 * Copied from .NET BCL, changed LogValuesFormatter to UnfuckedLogValuesFormatter to allow modifications in that class
 */
/// <summary>
/// LogValues to enable formatting options supported by <see cref="string.Format(IFormatProvider, string, object?)"/>.
/// This also enables using {NamedformatItem} in the format string.
/// </summary>
internal readonly struct UnfuckedFormattedLogValues: IReadOnlyList<KeyValuePair<string, object?>> {

    internal const int    MaxCachedFormatters = 1024;
    private const  string NullFormat          = "[null]";

    private static          int                                                      s_count;
    private static readonly ConcurrentDictionary<string, UnfuckedLogValuesFormatter> s_formatters = new();

    private readonly UnfuckedLogValuesFormatter? _formatter;
    private readonly object?[]?                  _values;
    private readonly string                      _originalMessage;

    // for testing purposes
    internal UnfuckedLogValuesFormatter? Formatter => _formatter;

    public UnfuckedFormattedLogValues(string? format, params object?[]? values) {
        if (values != null && values.Length != 0 && format != null) {
            if (s_count >= MaxCachedFormatters) {
                if (!s_formatters.TryGetValue(format, out _formatter)) {
                    _formatter = new UnfuckedLogValuesFormatter(format);
                }
            } else {
                _formatter = s_formatters.GetOrAdd(format, f => {
                    Interlocked.Increment(ref s_count);
                    return new UnfuckedLogValuesFormatter(f);
                });
            }
        } else {
            _formatter = null;
        }

        _originalMessage = format ?? NullFormat;
        _values          = values;
    }

    public KeyValuePair<string, object?> this[int index] {
        get {
            if (index < 0 || index >= Count) {
                throw new IndexOutOfRangeException(nameof(index));
            }

            if (index == Count - 1) {
                return new KeyValuePair<string, object?>("{OriginalFormat}", _originalMessage);
            }

            return _formatter!.GetValue(_values!, index);
        }
    }

    public int Count {
        get {
            if (_formatter == null) {
                return 1;
            }

            return _formatter.ValueNames.Count + 1;
        }
    }

    public IEnumerator<KeyValuePair<string, object?>> GetEnumerator() {
        for (int i = 0; i < Count; ++i) {
            yield return this[i];
        }
    }

    public override string ToString() {
        if (_formatter == null) {
            return _originalMessage;
        }

        return _formatter.Format(_values);
    }

    IEnumerator IEnumerable.GetEnumerator() {
        return GetEnumerator();
    }

}