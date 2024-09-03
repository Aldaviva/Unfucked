using Ical.Net.DataTypes;
using NodaTime;
using NodaTime.Extensions;
using System.Diagnostics.Contracts;

namespace Unfucked;

public static class DateConversion {

    [Pure]
    public static IDateTime ToIcalDateTimeUtc(this OffsetDateTime input) => new CalDateTime(input.ToInstant().ToDateTimeUtc(), DateTimeZone.Utc.Id);

    [Pure]
    public static IDateTime ToIcalDateTime(this DateTimeOffset dateTimeOffset) => new CalDateTime(dateTimeOffset.DateTime, dateTimeOffset.ToZonedDateTime().Zone.Id);

    [Pure]
    public static IDateTime ToIcalDateTime(this DateTime dateTime) => new CalDateTime(dateTime);

    [Pure]
    public static IDateTime ToIcalDateTime(this ZonedDateTime zonedDateTime) => new CalDateTime(zonedDateTime.ToDateTimeUnspecified(), zonedDateTime.Zone.Id);

}