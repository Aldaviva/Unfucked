namespace Tests.Unfucked;

public class DateTimeTest {

    [Fact]
    public void AbsoluteTimespan() {
        TimeSpan.FromMinutes(-30).Abs.Should().Be(TimeSpan.FromMinutes(30));
        TimeSpan.FromMinutes(30).Abs.Should().Be(TimeSpan.FromMinutes(30));
    }

    [Fact]
    public void ParseIso8601TimeSpan() {
        TimeSpan.ParseIso8601("PT30M").Should().Be(TimeSpan.FromMinutes(30));
    }

}