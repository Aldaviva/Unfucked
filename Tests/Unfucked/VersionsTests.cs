namespace Tests.Unfucked;

public class VersionsTests {

    [Theory]
    [InlineData("0.0", 1, 4, "0")]
    [InlineData("0.0", 2, 4, "0.0")]
    [InlineData("0.0", 3, 4, "0.0.0")]
    [InlineData("0.0", 4, 4, "0.0.0.0")]
    [InlineData("1.0", 1, 4, "1")]
    [InlineData("1.0", 2, 4, "1.0")]
    [InlineData("1.0", 3, 4, "1.0.0")]
    [InlineData("1.0", 4, 4, "1.0.0.0")]
    [InlineData("1.0", 1, 1, "1")]
    [InlineData("1.0", 1, 2, "1")]
    [InlineData("1.0", 1, 3, "1")]
    [InlineData("1.2.3.4", 1, 1, "1")]
    [InlineData("1.2.3.4", 1, 2, "1.2")]
    [InlineData("1.2.3.4", 1, 3, "1.2.3")]
    [InlineData("1.2.3.4", 1, 4, "1.2.3.4")]
    public void VersionToString(string input, int min, int max, string expected) {
        string actual = Version.Parse(input).ToString(min, max);
        actual.Should().Be(expected);
    }

}