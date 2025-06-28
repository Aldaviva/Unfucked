using Ical.Net.DataTypes;
using NodaTime;
using NodaTime.Extensions;
using System.Diagnostics.Contracts;
using Duration = NodaTime.Duration;

namespace Unfucked;

/// <summary>
/// Methods that make it easier to convert between datetimes from the .NET BCL, Ical.Net, and NodaTime.
/// </summary>
public static class DateConversion {

    /// <summary>
    /// Convert a datetime from NodaTime to Ical.Net in UTC.
    /// </summary>
    /// <param name="offsetDateTime">A NodaTime datetime that contains a numeric timezone offset.</param>
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
    /// Convert a datetime from NodaTime to Ical.Net.
    /// </summary>
    /// <param name="zonedDateTime">A NodaTime datetime that contains a timezone ID.</param>
    /// <returns>An Ical.Net datetime that represents the same instant in time, and specified in the same timezone, as <paramref name="zonedDateTime"/>.</returns>
    [Pure]
    public static CalDateTime ToIcalDateTime(this ZonedDateTime zonedDateTime) => new(zonedDateTime.ToDateTimeUnspecified(), zonedDateTime.Zone.Id);

    [Pure]
    public static ZonedDateTime ToZonedDateTime(this CalDateTime icalDateTime) =>
        icalDateTime.ToInstant().InZone(icalDateTime.TzId is { } tzid ? DateTimeZoneProviders.Tzdb[tzid] : DateTimeZoneProviders.Tzdb.GetSystemDefault());

    [Pure]
    public static Duration ToDuration(this Ical.Net.DataTypes.Duration icalDuration) => icalDuration.ToTimeSpanUnspecified().ToDuration();

    [Pure]
    public static Ical.Net.DataTypes.Duration ToIcalDuration(this Duration duration) => ToIcalDuration(duration.ToTimeSpan());

    [Pure]
    public static Ical.Net.DataTypes.Duration ToIcalDuration(this TimeSpan timeSpan) => Ical.Net.DataTypes.Duration.FromTimeSpanExact(timeSpan);

    [Pure]
    public static Instant ToInstant(this CalDateTime icalDateTime) => icalDateTime.AsUtc.ToInstant();

    [Pure]
    public static bool Equals(this Ical.Net.DataTypes.Duration duration, Ical.Net.DataTypes.Duration? other, bool fucked = true) =>
        fucked ? duration.Equals(other) : duration.ToTimeSpanUnspecified().Equals(other?.ToTimeSpanUnspecified());

}