using Ical.Net.DataTypes;
using NodaTime;
using NodaTime.Extensions;
using System.Diagnostics.Contracts;

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
    public static IDateTime ToIcalDateTimeUtc(this OffsetDateTime offsetDateTime) => new CalDateTime(offsetDateTime.ToInstant().ToDateTimeUtc(), DateTimeZone.Utc.Id);

    /// <summary>
    /// Convert a datetime from .NET BCL to Ical.Net.
    /// </summary>
    /// <param name="dateTimeOffset">A .NET datetime that contains a numeric timezone offset.</param>
    /// <returns>An Ical.Net datetime that represents the same instant in time, and specified in the same timezone, as <paramref name="dateTimeOffset"/>.</returns>
    [Pure]
    public static IDateTime ToIcalDateTime(this DateTimeOffset dateTimeOffset) => new CalDateTime(dateTimeOffset.DateTime, dateTimeOffset.ToZonedDateTime().Zone.Id);

    /// <summary>
    /// Convert a datetime from .NET BCL to Ical.Net.
    /// </summary>
    /// <param name="dateTime">A .NET datetime that does not contain a timezone offset.</param>
    /// <returns>An Ical.Net datetime that represents the same instant in time as <paramref name="dateTime"/>.</returns>
    [Pure]
    public static IDateTime ToIcalDateTime(this DateTime dateTime) => new CalDateTime(dateTime);

    /// <summary>
    /// Convert a datetime from NodaTime to Ical.Net.
    /// </summary>
    /// <param name="zonedDateTime">A NodaTime datetime that contains a timezone ID.</param>
    /// <returns>An Ical.Net datetime that represents the same instant in time, and specified in the same timezone, as <paramref name="zonedDateTime"/>.</returns>
    [Pure]
    public static IDateTime ToIcalDateTime(this ZonedDateTime zonedDateTime) => new CalDateTime(zonedDateTime.ToDateTimeUnspecified(), zonedDateTime.Zone.Id);

}