namespace Unfucked;

public static class Comparables {

    public static T Clip<T>(this T input, T min, T max) where T: IComparable<T> {
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