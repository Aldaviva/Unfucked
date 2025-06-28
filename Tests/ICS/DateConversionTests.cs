using Ical.Net.DataTypes;
using NodaTime;

namespace Tests.ICS;

public class DateConversionTests {

    [Fact]
    public void ToIcalDateTimeUtcFromOffsetDateTime() {
        CalDateTime actual = new LocalDateTime(1988, 8, 17, 16, 30).WithOffset(Offset.FromHours(-4)).ToIcalDateTimeUtc();

        actual.IsUtc.Should().BeTrue();
        actual.IsFloating.Should().BeFalse();
        actual.TzId.Should().Be("UTC");
        actual.Year.Should().Be(1988);
        actual.Month.Should().Be(8);
        actual.Day.Should().Be(17);
        actual.Hour.Should().Be(20);
        actual.Minute.Should().Be(30);
        actual.Second.Should().Be(0);
    }

    [Fact]
    public void ToIcalDateTimeFromDateTimeOffset() {
        CalDateTime actual = new DateTimeOffset(1988, 8, 17, 16, 30, 0, TimeSpan.FromHours(-4)).ToIcalDateTime();

        actual.IsUtc.Should().BeFalse();
        actual.IsFloating.Should().BeFalse();
        actual.TzId.Should().Be("Etc/GMT+4");
        actual.Year.Should().Be(1988);
        actual.Month.Should().Be(8);
        actual.Day.Should().Be(17);
        actual.Hour.Should().Be(16);
        actual.Minute.Should().Be(30);
        actual.Second.Should().Be(0);
    }

    [Fact]
    public void ToIcalDateTimeFromDateTime() {
        CalDateTime actual = new DateTime(1988, 8, 17, 16, 30, 0, DateTimeKind.Unspecified).ToIcalDateTime();

        actual.IsUtc.Should().BeFalse();
        actual.IsFloating.Should().BeTrue();
        actual.TzId.Should().BeNull();
        actual.Year.Should().Be(1988);
        actual.Month.Should().Be(8);
        actual.Day.Should().Be(17);
        actual.Hour.Should().Be(16);
        actual.Minute.Should().Be(30);
        actual.Second.Should().Be(0);
    }

    [Fact]
    public void ToIcalDateTimeFromZonedDateTime() {
        CalDateTime actual = new LocalDateTime(1988, 8, 17, 16, 30).InZoneStrictly(DateTimeZoneProviders.Tzdb["America/New_York"]).ToIcalDateTime();

        actual.IsUtc.Should().BeFalse();
        actual.IsFloating.Should().BeFalse();
        actual.TzId.Should().Be("America/New_York");
        actual.Year.Should().Be(1988);
        actual.Month.Should().Be(8);
        actual.Day.Should().Be(17);
        actual.Hour.Should().Be(16);
        actual.Minute.Should().Be(30);
        actual.Second.Should().Be(0);
    }

    [Fact]
    public void ToZonedDateTimeFromIcalDateTime() {
        ZonedDateTime actual = new CalDateTime(1988, 8, 17, 16, 30, 0, "America/New_York").ToZonedDateTime();

        actual.Zone.Id.Should().Be("America/New_York");
        actual.Year.Should().Be(1988);
        actual.Month.Should().Be(8);
        actual.Day.Should().Be(17);
        actual.Hour.Should().Be(16);
        actual.Minute.Should().Be(30);
        actual.Second.Should().Be(0);
    }

}