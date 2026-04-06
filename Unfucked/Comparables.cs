namespace Unfucked;

/// <summary>
/// Methods that make it easier to work with comparable values.
/// </summary>
public static class Comparables {

    /// <summary>
    /// Clip/clamp/bound/saturate a value, restricting the range of the value to a given minimum and maximum.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="input">The value to clip.</param>
    /// <param name="min">The minimum value to return, inclusive.</param>
    /// <param name="max">The maximum value to return, inclusive.</param>
    /// <returns>A value in the range [<paramref name="min"/>, <paramref name="max"/>]. If <paramref name="input"/> is inside that range, it will be returned unchanged; otherwise, if it is outside one of the limits, the limit will be returned.</returns>
    public static T Clip<T>(this T input, T min, T max) where T: IComparable<T> {
        if (min.CompareTo(max) > 0) {
            (min, max) = (max, min);
        }

        if (input.CompareTo(max) > 0) {
            return max;
        } else if (input.CompareTo(min) < 0) {
            return min;
        } else {
            return input;
        }
    }

/*#if NET8_0_OR_GREATER
    public static T Clamp<T>(this T input, T min, T max) where T: IComparisonOperators<T, T, bool> {
        if (input > max) {
            return max;
        } else if (input < min) {
            return min;
        } else {
            return input;
        }
    }
#endif*/

}