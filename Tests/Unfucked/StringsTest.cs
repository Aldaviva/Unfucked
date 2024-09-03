namespace Tests.Unfucked;

public class StringsTest {

    [Theory]
    [InlineData(new object[0], "")]
    [InlineData(new[] { "AlphaDolphin" }, "AlphaDolphin")]
    [InlineData(new[] { "Konception", "limy" }, "Konception and limy")]
    [InlineData(new[] { "ZephyrGlaze", "WDRM", "chezmix" }, "ZephyrGlaze, WDRM, and chezmix")]
    [InlineData(new[] { "SpacebarS", "shovelclaws", "Themimik", "Yoshipuff", "AlphaDolphin", "Allegro" }, "SpacebarS, shovelclaws, Themimik, Yoshipuff, AlphaDolphin, and Allegro")]
    [InlineData(new[] { "Aurateur", "PangaeaPanga", "TanukiDan", "Shoujo", "Caspur", "Aldwulf", "LilKirbs", "Thabeast721" },
        "Aurateur, PangaeaPanga, TanukiDan, Shoujo, Caspur, Aldwulf, LilKirbs, and Thabeast721")]
    public void JoinHumanized(IEnumerable<object> input, string expected) {
        input.JoinHumanized().Should().Be(expected);
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData("", null)]
    [InlineData(" ", " ")]
    [InlineData("a", "a")]
    [InlineData("abc def", "abc def")]
    public void EmptyToNull(string? input, string? expected) {
        input.EmptyToNull().Should().Be(expected);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("  ")]
    [InlineData("\t")]
    [InlineData("\n")]
    [InlineData("\r\n")]
    public void HasNoText(string? input) {
        input.HasText().Should().BeFalse();
    }

    [Theory]
    [InlineData(".")]
    [InlineData(" .")]
    [InlineData(". ")]
    [InlineData(" . ")]
    [InlineData(". .")]
    public void HasText(string? input) {
        input.HasText().Should().BeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void HasNoLength(string? input) {
        input.HasLength().Should().BeFalse();
    }

    [Theory]
    [InlineData(" ")]
    [InlineData("  ")]
    [InlineData("\t")]
    [InlineData("\n")]
    [InlineData("\r\n")]
    [InlineData(".")]
    [InlineData(" .")]
    [InlineData(". ")]
    [InlineData(" . ")]
    [InlineData(". .")]
    public void HasLength(string? input) {
        input.HasLength().Should().BeTrue();
    }

    [Fact]
    public void Join() {
        string[] strings = { "a", "b", "c" };
        strings.Join().Should().Be("abc");
        strings.Join(", ").Should().Be("a, b, c");
        strings.Join(';').Should().Be("a;b;c");
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData("", "")]
    [InlineData(".", ".")]
    [InlineData("a", "A")]
    [InlineData("A", "A")]
    [InlineData("abc", "Abc")]
    [InlineData("aBC", "ABC")]
    [InlineData("abc def", "Abc def")]
    public void ToUpperFirstLetter(string? input, string? expected) {
        input.ToUpperFirstLetter().Should().Be(expected);
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData("", "")]
    [InlineData(".", ".")]
    [InlineData("a", "a")]
    [InlineData("A", "a")]
    [InlineData("Abc", "abc")]
    [InlineData("ABC", "aBC")]
    [InlineData("Abc def", "abc def")]
    public void ToLowerFirstLetter(string? input, string? expected) {
        input.ToLowerFirstLetter().Should().Be(expected);
    }

    [Theory]
    [InlineData("", "")]
    [InlineData("a", "a")]
    [InlineData("aaa", "aaa")]
    [InlineData("xaaa", "xaaa")]
    [InlineData("yaaa", "yaaa")]
    [InlineData("yxaaa", "yxaaa")]
    [InlineData("xyaaa", "aaa")]
    [InlineData("xyxyaaa", "aaa")]
    [InlineData("xyxyaaaxy", "aaaxy")]
    [InlineData("xyxyaaaxyxy", "aaaxyxy")]
    [InlineData("xyaxyaaaxyxy", "axyaaaxyxy")]
    [InlineData("mnaaa", "aaa")]
    [InlineData("mnxyaaa", "aaa")]
    [InlineData("xymnxyaaa", "aaa")]
    [InlineData("mnxymnxyaaa", "aaa")]
    public void TrimStart(string input, string expected) {
        input.TrimStart("mn", "xy").Should().Be(expected);
    }

    [Theory]
    [InlineData("", "")]
    [InlineData("a", "a")]
    [InlineData("aaa", "aaa")]
    [InlineData("aaax", "aaax")]
    [InlineData("aaay", "aaay")]
    [InlineData("aaayx", "aaayx")]
    [InlineData("aaaxy", "aaa")]
    [InlineData("aaaxyxy", "aaa")]
    [InlineData("xyaaaxyxy", "xyaaa")]
    [InlineData("xyxyaaaxyxy", "xyxyaaa")]
    [InlineData("xyaxyaaaxyxy", "xyaxyaaa")]
    [InlineData("aaamn", "aaa")]
    [InlineData("aaamnxy", "aaa")]
    [InlineData("aaaxymnxy", "aaa")]
    [InlineData("aaamnxymnxy", "aaa")]
    public void TrimEnd(string input, string expected) {
        input.TrimEnd("mn", "xy").Should().Be(expected);
    }

    [Theory]
    [InlineData("", "")]
    [InlineData("aaa", "aaa")]
    [InlineData("xyaaa", "aaa")]
    [InlineData("aaaxy", "aaa")]
    [InlineData("aaaxyxy", "aaa")]
    [InlineData("xyxyaaa", "aaa")]
    [InlineData("xyxyaaaxy", "aaa")]
    [InlineData("xyxyaaaxyxy", "aaa")]
    [InlineData("xymnaaaxymn", "aaa")]
    public void Trim(string input, string expected) {
        input.Trim("mn", "xy").Should().Be(expected);
    }

    [Fact]
    public void StringToStream() {
        Stream actual = "abc".ToStream();
        actual.Should().HaveLength(3);
        byte[] buffer = new byte[3];
        actual.Read(buffer).Should().Be(3);
        buffer.Should().Equal("abc"u8.ToArray());
    }

    [Fact]
    public void StringToBytes() {
        byte[] actual = "abc".ToBytes();
        actual.Should().HaveCount(3);
        actual.Should().Equal("abc"u8.ToArray());
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData("", "")]
    [InlineData("abc", "abc")]
    [InlineData("abc\r\ndef", "abc\ndef")]
    [InlineData("abc\r\n\r\ndef", "abc\n\ndef")]
    [InlineData("abc\r\n\r\ndef\r\n", "abc\n\ndef\n")]
    [InlineData("abc\ndef\r\n", "abc\ndef\n")]
    public void Dos2Unix(string? input, string? expected) {
        input.Dos2Unix().Should().Be(expected);
    }

}