namespace Tests;

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

}