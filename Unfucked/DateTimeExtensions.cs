namespace Unfucked;

/// <summary>
/// Methods that make it easier to work with BCL dates and times
/// </summary>
public static class DateTimeExtensions {

    /// <summary>
    /// Absolute value of a time span
    /// </summary>
    /// <param name="input">a time span that may be positive, negative, or zero</param>
    /// <returns>the nonnegative magnitude of <paramref name="input"/></returns>
    [Pure]
    public static TimeSpan Abs(this TimeSpan input) => input.Duration();

}