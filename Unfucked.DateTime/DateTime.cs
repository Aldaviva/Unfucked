using NodaTime;
using System.Diagnostics.Contracts;

namespace Unfucked;

public static class DateTime {

    /// <summary>
    /// Absolute value of a duration
    /// </summary>
    /// <param name="input">a duration that may be positive, negative, or zero</param>
    /// <returns>the nonnegative magnitude of <paramref name="input"/></returns>
    [Pure]
    public static Duration Abs(this Duration input) {
        return input < Duration.Zero ? Duration.Negate(input) : input;
    }

    /// <inheritdoc cref="Abs(NodaTime.Duration)"/>
    [Pure]
    public static Duration? Abs(this Duration? input) {
        return input != null && input < Duration.Zero ? Duration.Negate(input.Value) : input;
    }

    /// <summary>
    /// Absolute value of a time span
    /// </summary>
    /// <param name="input">a time span that may be positive, negative, or zero</param>
    /// <returns>the nonnegative magnitude of <paramref name="input"/></returns>
    [Pure]
    public static TimeSpan Abs(this TimeSpan input) {
        return input < TimeSpan.Zero ? input.Negate() : input;
    }

    /// <summary>
    /// Get midnight on the same day as a given <see cref="ZonedDateTime"/>.
    /// </summary>
    /// <param name="input">a time</param>
    /// <returns>midnight on the same date, time zone, and calendar as <paramref name="input"/></returns>
    [Pure]
    public static ZonedDateTime AtStartOfDay(this ZonedDateTime input) {
        return input.Zone.AtStartOfDay(input.Date);
    }

    /// <summary>
    /// Convert a <see cref="LocalTime"/> to a <see cref="Period"/>, representing the hours, minutes, and seconds since midnight.
    /// </summary>
    /// <param name="localTime">time of day</param>
    /// <returns>period of time between <paramref name="localTime"/> and midnight of the same date</returns>
    [Pure]
    private static Period ToPeriodSinceStartOfDay(this LocalTime localTime) {
        return localTime.Minus(LocalTime.Midnight);
    }

    /// <summary>
    /// Convert a <see cref="LocalTime"/> to a <see cref="Duration"/>, representing the nanoseconds since midnight.
    /// </summary>
    /// <param name="localTime">time of day</param>
    /// <returns>duration of time between <paramref name="localTime"/> and midnight of the same date</returns>
    [Pure]
    public static Duration ToDurationSinceStartOfDay(this LocalTime localTime) {
        return localTime.ToPeriodSinceStartOfDay().ToDuration();
    }

    /// <summary>
    /// Convert a time zone's UTC offset to fractional hours
    /// </summary>
    /// <param name="offset">time zone UTC offset, for example, <c>UTC−04:00</c> for <c>America/New_York</c> during Eastern Daylight Time</param>
    /// <returns>floating-point hours that <paramref name="offset"/> is ahead of UTC, which would be <c>-4.0</c> in the above example</returns>
    [Pure]
    public static double ToHours(this Offset offset) {
        return offset.ToTimeSpan().TotalHours;
    }

    /// <summary>
    /// Is this time before another?
    /// </summary>
    /// <param name="time">a time</param>
    /// <param name="other">another time</param>
    /// <returns><c>true</c> if this <paramref name="time"/> happens before <paramref name="other"/>, or <c>false</c> if it happens on or after <paramref name="other"/>.</returns>
    [Pure]
    public static bool IsBefore(this OffsetDateTime time, OffsetDateTime other) => time.ToInstant() < other.ToInstant();

    /// <summary>
    /// Is this time after another?
    /// </summary>
    /// <param name="time">a time</param>
    /// <param name="other">another time</param>
    /// <returns><c>true</c> if this <paramref name="time"/> happens after <paramref name="other"/>, or <c>false</c> if it happens on or before <paramref name="other"/>.</returns>
    [Pure]
    public static bool IsAfter(this OffsetDateTime time, OffsetDateTime other) => time.ToInstant() > other.ToInstant();

}