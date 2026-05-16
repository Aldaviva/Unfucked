using NodaTime;
using NodaTime.Extensions;
using NodaTime.TimeZones;
using System.Diagnostics.Contracts;
using Unfucked.DateTime;

namespace Unfucked;

/// <summary>
/// Methods that make it easier to work with NodaTime dates and times
/// </summary>
public static class NodaTimeExtensions {

    internal static ZonedClock localClock {
        get {
            try {
                return SystemClock.Instance.InTzdbSystemDefaultZone();
            } catch (DateTimeZoneNotFoundException) {
                return SystemClock.Instance.InBclSystemDefaultZone();
            }
        }
    }

    /// <param name="input">a duration that may be positive, negative, or zero</param>
    extension(Duration input) {

        /// <summary>
        /// Absolute value of a duration
        /// </summary>
        /// <returns>the nonnegative magnitude of <paramref name="input"/></returns>
        [Pure]
        public Duration Abs => input < Duration.Zero ? Duration.Negate(input) : input;

    }

    /// <param name="input">a duration that may be positive, negative, or zero</param>
    extension(Duration? input) {

        /// <inheritdoc cref="get_Abs(Duration)"/>
        [Pure]
        public Duration? Abs => input != null && input < Duration.Zero ? Duration.Negate(input.Value) : input;

    }

    /// <param name="localTime">time of day</param>
    extension(LocalTime localTime) {

        /// <summary>
        /// Convert a <see cref="LocalTime"/> to a <see cref="Period"/>, representing the hours, minutes, and seconds since midnight.
        /// </summary>
        /// <returns>period of time between <paramref name="localTime"/> and midnight of the same date</returns>
        [Pure]
        public Period PeriodSinceStartOfDay => localTime.Minus(LocalTime.Midnight);

        /// <summary>
        /// Convert a <see cref="LocalTime"/> to a <see cref="Duration"/>, representing the nanoseconds since midnight.
        /// </summary>
        /// <returns>duration of time between <paramref name="localTime"/> and midnight of the same date</returns>
        [Pure]
        public Duration DurationSinceStartOfDay => localTime.PeriodSinceStartOfDay.ToDuration();

    }

    /// <param name="offset">time zone UTC offset, for example, <c>UTC−04:00</c> for <c>America/New_York</c> during Eastern Daylight Time</param>
    extension(Offset offset) {

        /// <summary>
        /// Convert a time zone's UTC offset to fractional hours
        /// </summary>
        /// <returns>floating-point hours that <paramref name="offset"/> is ahead of UTC, which would be <c>-4.0</c> in the above example</returns>
        [Pure]
        public double Hours => offset.ToTimeSpan().TotalHours;

    }

    extension(DateTimeZone zone) {

        /// <summary>Create a clock that can the current time in the given zone.</summary>
        [Pure]
        public ZonedClock Clock => SystemClock.Instance.InZone(zone);

    }

    extension(DateTimeZoneProviders) {

        /// <summary>Create a time zone provider that periodically automatically updates the tzdb online from Noda Time's servers.</summary>
        /// <param name="updateInterval">How often to check for updates.</param>
        /// <returns>A time zone provider that can look up IANA time zones, like <see cref="DateTimeZoneProviders.Tzdb"/>.</returns>
        [Pure]
        public static IDateTimeZoneProvider CreateOnlineUpdatingTzdbProvider(TimeSpan updateInterval) => new OnlineUpdatingTzdbDateTimeZoneProvider(updateInterval);

    }

}

/// <inheritdoc cref="NodaTimeExtensions" />
public static class InstantExtensions {

    extension(Instant) {

        /// <summary>
        /// Gets the current system date and time.
        /// </summary>
        [Pure]
        public static Instant Now => SystemClock.Instance.GetCurrentInstant();

    }

}

/// <inheritdoc cref="NodaTimeExtensions" />
public static class LocalDateTimeExtensions {

    extension(LocalDateTime) {

        /// <summary>
        /// Gets the current system date and time in the system's IANA time zone.
        /// </summary>
        /// <exception cref="NodaTime.TimeZones.DateTimeZoneNotFoundException" accessor="get"></exception>
        [Pure]
        public static LocalDateTime Now => NodaTimeExtensions.localClock.GetCurrentLocalDateTime();

    }

}

/// <inheritdoc cref="NodaTimeExtensions" />
public static class LocalDateExtensions {

    extension(LocalTime) {

        /// <summary>
        /// Gets the current system date in the system's IANA time zone.
        /// </summary>
        /// <exception cref="NodaTime.TimeZones.DateTimeZoneNotFoundException" accessor="get"></exception>
        [Pure]
        public static LocalDate Now => LocalDateTime.Now.Date;

    }

}

/// <inheritdoc cref="NodaTimeExtensions" />
public static class LocalTimeExtensions {

    extension(LocalTime) {

        /// <summary>
        /// Gets the current system time in the system's IANA time zone.
        /// </summary>
        /// <exception cref="NodaTime.TimeZones.DateTimeZoneNotFoundException" accessor="get"></exception>
        [Pure]
        public static LocalTime Now => LocalDateTime.Now.TimeOfDay;

    }

}

/// <inheritdoc cref="NodaTimeExtensions" />
public static class OffsetDateTimeExtensions {

    /// <param name="time">a time</param>
    extension(OffsetDateTime time) {

        /// <summary>
        /// Gets the current system date and time with the system's IANA time zone offset.
        /// </summary>
        /// <exception cref="NodaTime.TimeZones.DateTimeZoneNotFoundException" accessor="get"></exception>
        [Pure]
        public static OffsetDateTime Now => NodaTimeExtensions.localClock.GetCurrentOffsetDateTime();

        /// <summary>
        /// Gets the current system date and time with zero offset from UTC.
        /// </summary>
        [Pure]
        public static OffsetDateTime NowUtc => SystemClock.Instance.InUtc().GetCurrentOffsetDateTime();

        /// <summary>
        /// Is this time before another?
        /// </summary>
        /// <param name="other">another time</param>
        /// <returns><c>true</c> if this <paramref name="time"/> happens before <paramref name="other"/>, or <c>false</c> if it happens on or after <paramref name="other"/>.</returns>
        [Pure]
        public bool IsBefore(OffsetDateTime other) => time.ToInstant() < other.ToInstant();

        /// <summary>
        /// Is this time after another?
        /// </summary>
        /// <param name="other">another time</param>
        /// <returns><c>true</c> if this <paramref name="time"/> happens after <paramref name="other"/>, or <c>false</c> if it happens on or before <paramref name="other"/>.</returns>
        [Pure]
        public bool IsAfter(OffsetDateTime other) => time.ToInstant() > other.ToInstant();

    }

}

/// <inheritdoc cref="NodaTimeExtensions" />
public static class ZonedDateTimeExtensions {

    /// <param name="time">a time</param>
    extension(ZonedDateTime time) {

        /// <summary>
        /// Gets the current system date and time in the system's IANA time zone.
        /// </summary>
        /// <exception cref="NodaTime.TimeZones.DateTimeZoneNotFoundException" accessor="get"></exception>
        [Pure]
        public static ZonedDateTime Now => NodaTimeExtensions.localClock.GetCurrentZonedDateTime();

        /// <summary>
        /// Gets the current system date and time in UTC.
        /// </summary>
        [Pure]
        public static ZonedDateTime NowUtc => SystemClock.Instance.InUtc().GetCurrentZonedDateTime();

        /// <inheritdoc cref="OffsetDateTimeExtensions.IsBefore" />
        [Pure]
        public bool IsBefore(ZonedDateTime other) => time.ToInstant() < other.ToInstant();

        /// <inheritdoc cref="OffsetDateTimeExtensions.IsAfter" />
        [Pure]
        public bool IsAfter(ZonedDateTime other) => time.ToInstant() > other.ToInstant();

        /// <summary>
        /// Get midnight on the same day as a given <see cref="ZonedDateTime"/>.
        /// </summary>
        /// <returns>midnight on the same date, time zone, and calendar as <paramref name="time"/></returns>
        [Pure]
        public ZonedDateTime StartOfDay => time.Zone.AtStartOfDay(time.Date);

    }

}