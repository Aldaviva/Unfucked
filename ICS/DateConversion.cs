using Ical.Net.DataTypes;
using NodaTime;
using NodaTime.Extensions;
using System.Diagnostics.Contracts;
using Duration = NodaTime.Duration;

namespace Unfucked;

/// <summary>
/// Methods that make it easier to convert between datetimes from the .NET BCL, Ical.Net, and Noda Time.
/// </summary>
public static class DateConversion {

    /// <summary>
    /// Convert a datetime from Noda Time to Ical.Net in UTC.
    /// </summary>
    /// <param name="offsetDateTime">A Noda Time datetime that contains a numeric timezone offset.</param>
    /// <returns>An Ical.Net datetime that represents that same instant in time as <paramref name="offsetDateTime"/>, but specified in UTC instead of the input offset.</returns>
    [Pure]
    public static CalDateTime ToIcalDateTimeUtc(this OffsetDateTime offsetDateTime) => new(offsetDateTime.ToInstant().ToDateTimeUtc(), DateTimeZone.Utc.Id);

    /// <summary>
    /// Convert a datetime from .NET BCL to Ical.Net.
    /// </summary>
    /// <param name="dateTimeOffset">A .NET datetime that contains a numeric timezone offset.</param>
    /// <returns>An Ical.Net datetime that represents the same instant in time, and specified in the same timezone, as <paramref name="dateTimeOffset"/>.</returns>
    [Pure]
    public static CalDateTime ToIcalDateTime(this DateTimeOffset dateTimeOffset) => new(dateTimeOffset.DateTime, GetOlsenZoneForOffset(dateTimeOffset.Offset).Id);

    private static DateTimeZone GetOlsenZoneForOffset(TimeSpan utcOffset) =>
        (utcOffset is { Minutes: 0, Seconds: 0, Milliseconds: 0 }
            ? DateTimeZoneProviders.Tzdb.GetZoneOrNull($"Etc/GMT{-utcOffset.TotalHours:+0;-0;}") : null)
        ?? DateTimeZone.ForOffset(Offset.FromTimeSpan(utcOffset));

    /// <summary>
    /// Convert a datetime from .NET BCL to Ical.Net.
    /// </summary>
    /// <param name="dateTime">A .NET datetime that does not contain a timezone offset.</param>
    /// <returns>An Ical.Net datetime that represents the same instant in time as <paramref name="dateTime"/>.</returns>
    [Pure]
    public static CalDateTime ToIcalDateTime(this DateTime dateTime) => new(dateTime);

    /// <summary>
    /// Convert a datetime from Noda Time to Ical.Net.
    /// </summary>
    /// <param name="zonedDateTime">A Noda Time datetime that contains a timezone ID.</param>
    /// <returns>An Ical.Net datetime that represents the same instant in time, and specified in the same timezone, as <paramref name="zonedDateTime"/>.</returns>
    [Pure]
    public static CalDateTime ToIcalDateTime(this ZonedDateTime zonedDateTime) => new(zonedDateTime.ToDateTimeUnspecified(), zonedDateTime.Zone.Id);

    /// <summary>Convert a date from .NET BCL to Ical.Net.</summary>
    /// <param name="dateOnly">A .NET date that does not contain a time.</param>
    /// <returns>An Ical.Net date that represents the same instant in time as <paramref name="dateOnly"/>.</returns>
    [Pure]
    public static CalDateTime ToIcalDateTime(this DateOnly dateOnly) => new(dateOnly);

    /// <summary>Convert a date from Noda Time to Ical.Net.</summary>
    /// <param name="localDate">A Noda Time date that does not contain a time.</param>
    /// <returns>An Ical.Net date that represents the same instant in time as <paramref name="localDate"/>.</returns>
    [Pure]
    public static CalDateTime ToIcalDateTime(this LocalDate localDate) => new(localDate.Year, localDate.Month, localDate.Day);

    /// <summary>Convert a datetime from Ical.Net to Noda Time.</summary>
    /// <param name="icalDateTime">An Ical.Net datetime.</param>
    /// <returns>A Noda Time zoned datetime that represents the same instant in time as <paramref name="icalDateTime"/> with the same time zone.</returns>
    /// <exception cref="NodaTime.TimeZones.DateTimeZoneNotFoundException">the time zone ID in <paramref name="icalDateTime"/> does not map to an IANA/Olsen/tzdb time zone ID known by Noda Time</exception>
    [Pure]
    public static ZonedDateTime ToZonedDateTime(this CalDateTime icalDateTime) =>
        icalDateTime.ToInstant().InZone(icalDateTime.TzId is {} tzid ? DateTimeZoneProviders.Tzdb[tzid] : DateTimeZoneProviders.Tzdb.GetSystemDefault());

    /// <summary>Convert a duration from Ical.Net to Noda Time</summary>
    /// <param name="icalDuration">An Ical.Net duration.</param>
    /// <returns>A Noda Time duration that represents that same amount of time as <paramref name="icalDuration"/>.</returns>
    [Pure]
    public static Duration ToDuration(this Ical.Net.DataTypes.Duration icalDuration) => icalDuration.ToTimeSpanUnspecified().ToDuration();

    /// <summary>Convert a duration from Noda Time to Ical.Net.</summary>
    /// <param name="duration">A Noda Time duration.</param>
    /// <returns>An Ical.Net duration that represents that same amount of time as <paramref name="duration"/>.</returns>
    [Pure]
    public static Ical.Net.DataTypes.Duration ToIcalDuration(this Duration duration) => duration.ToTimeSpan().ToIcalDuration();

    /// <summary>Convert a duration/time span from .NET BCL to Ical.Net.</summary>
    /// <param name="timeSpan">A .NET BCL duration/time span.</param>
    /// <returns>An Ical.Net duration that represents that same amount of time as <paramref name="timeSpan"/>.</returns>
    [Pure]
    public static Ical.Net.DataTypes.Duration ToIcalDuration(this TimeSpan timeSpan) => Ical.Net.DataTypes.Duration.FromTimeSpanExact(timeSpan);

    /// <summary>Convert a datetime from Ical.Net to Noda Time.</summary>
    /// <param name="icalDateTime">An Ical.Net datetime.</param>
    /// <returns>A Noda Time instant that represents the same point in time as <paramref name="icalDateTime"/>.</returns>
    [Pure]
    public static Instant ToInstant(this CalDateTime icalDateTime) => icalDateTime.AsUtc.ToInstant();

    [Pure]
    public static bool Equals(this Ical.Net.DataTypes.Duration duration, Ical.Net.DataTypes.Duration? other, bool fucked = true) =>
        fucked ? duration.Equals(other) : duration.ToTimeSpanUnspecified().Equals(other?.ToTimeSpanUnspecified());

}