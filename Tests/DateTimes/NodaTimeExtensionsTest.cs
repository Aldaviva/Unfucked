using NodaTime;

namespace Tests.DateTimes;

public class NodaTimeExtensionsTest {

    private static readonly DateTimeZone  NEW_YORK        = DateTimeZoneProviders.Tzdb["America/New_York"];
    private static readonly ZonedDateTime ZONED_DATE_TIME = new LocalDateTime(1988, 8, 17, 16, 30).InZoneStrictly(NEW_YORK);

    [Theory]
    [MemberData(nameof(DurationAbsoluteValueData))]
    public void DurationAbsoluteValue(Duration input, Duration expected) {
        input.Abs().Should().Be(expected);
    }

    [Theory]
    [MemberData(nameof(DurationAbsoluteValueData))]
    public void OptionalDurationAbsoluteValue(Duration input, Duration expected) {
        ((Duration?) input).Abs().Should().Be(expected);
        ((Duration?) null).Abs().Should().BeNull();
    }

    public static TheoryData<Duration, Duration> DurationAbsoluteValueData => new() {
        { Duration.FromHours(1), Duration.FromHours(1) },
        { Duration.FromHours(-1), Duration.FromHours(1) },
        { Duration.Zero, Duration.Zero }
    };

    [Theory]
    [MemberData(nameof(AtStartOfDayData))]
    public void AtStartOfDay(ZonedDateTime input, ZonedDateTime expected) {
        input.AtStartOfDay().Should().Be(expected);
    }

    public static TheoryData<ZonedDateTime, ZonedDateTime> AtStartOfDayData => new() {
        { ZONED_DATE_TIME, new LocalDateTime(1988, 8, 17, 0, 0).InZoneStrictly(NEW_YORK) },
        { new LocalDateTime(1988, 8, 17, 0, 0).InZoneStrictly(NEW_YORK), new LocalDateTime(1988, 8, 17, 0, 0).InZoneStrictly(NEW_YORK) }
    };

    [Theory]
    [MemberData(nameof(ToPeriodAndDurationSinceStartOfDayData))]
    public void ToPeriodAndDurationSinceStartOfDay(LocalTime input, Period expectedPeriod, Duration expectedDuration) {
        input.ToPeriodSinceStartOfDay().Should().Be(expectedPeriod);
        input.ToDurationSinceStartOfDay().Should().Be(expectedDuration);
    }

    public static TheoryData<LocalTime, Period, Duration> ToPeriodAndDurationSinceStartOfDayData => new() {
        { ZONED_DATE_TIME.LocalDateTime.TimeOfDay, Period.FromHours(16) + Period.FromMinutes(30), Duration.FromHours(16) + Duration.FromMinutes(30) },
        { new LocalTime(0, 0), Period.Zero, Duration.Zero }
    };

    [Theory]
    [MemberData(nameof(OffsetToHoursData))]
    public void OffsetToHours(Offset input, double expected) {
        input.ToHours().Should().BeApproximately(expected, 0.001);
    }

    public static TheoryData<Offset, double> OffsetToHoursData => new() {
        { NEW_YORK.GetUtcOffset(ZONED_DATE_TIME.ToInstant()), -4 },
        { DateTimeZone.Utc.GetUtcOffset(ZONED_DATE_TIME.ToInstant()), 0 },
    };

    [Theory]
    [MemberData(nameof(OffsetDataTimeComparisonData))]
    public void OffsetDataTimeComparison(OffsetDateTime a, OffsetDateTime b, bool expectedAIsBeforeB, bool expectedAIsAfterB) {
        a.IsBefore(b).Should().Be(expectedAIsBeforeB, "a should be before b");
        a.IsAfter(b).Should().Be(expectedAIsAfterB, "a should be after b");
    }

    public static TheoryData<OffsetDateTime, OffsetDateTime, bool, bool> OffsetDataTimeComparisonData => new() {
        { ZONED_DATE_TIME.ToOffsetDateTime(), ZONED_DATE_TIME.ToOffsetDateTime(), false, false },
        { ZONED_DATE_TIME.AtStartOfDay().ToOffsetDateTime(), ZONED_DATE_TIME.ToOffsetDateTime(), true, false },
        { ZONED_DATE_TIME.ToOffsetDateTime(), ZONED_DATE_TIME.AtStartOfDay().ToOffsetDateTime(), false, true }
    };

    [Theory]
    [MemberData(nameof(ZonedDataTimeComparisonData))]
    public void ZonedDataTimeComparison(ZonedDateTime a, ZonedDateTime b, bool expectedAIsBeforeB, bool expectedAIsAfterB) {
        a.IsBefore(b).Should().Be(expectedAIsBeforeB, "a should be before b");
        a.IsAfter(b).Should().Be(expectedAIsAfterB, "a should be after b");
    }

    public static TheoryData<ZonedDateTime, ZonedDateTime, bool, bool> ZonedDataTimeComparisonData => new() {
        { ZONED_DATE_TIME, ZONED_DATE_TIME, false, false },
        { ZONED_DATE_TIME.AtStartOfDay(), ZONED_DATE_TIME, true, false },
        { ZONED_DATE_TIME, ZONED_DATE_TIME.AtStartOfDay(), false, true }
    };

}