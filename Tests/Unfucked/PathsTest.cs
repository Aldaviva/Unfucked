using System.Collections.Frozen;

namespace Tests.Unfucked;

public class PathsTest {

    [Theory]
    [InlineData(null, null)]
    [InlineData(@"", @"")]
    [InlineData(@"abc", @"abc")]
    [InlineData(@"abc/", @"abc")]
    [InlineData(@"abc//", @"abc")]
    [InlineData(@"abc///", @"abc")]
    [InlineData(@"abc\", @"abc")]
    [InlineData(@"abc\\", @"abc")]
    [InlineData(@"abc\\\", @"abc")]
    [InlineData(@"abc/\", @"abc")]
    [InlineData(@"abc\/", @"abc")]
    [InlineData(@"abc/def/", @"abc/def")]
    [InlineData(@"abc\def\", @"abc\def")]
    [InlineData(@"/abc", @"/abc")]
    [InlineData(@"\abc", @"\abc")]
    public void TrimSlashes(string? input, string? expected) {
        Paths.TrimTrailingSlashes(input).Should().Be(expected);
    }

    [Fact]
    public void GetTempDirectory() {
        string actual = Paths.CreateTempDir();
        try {
            Directory.Exists(actual).Should().BeTrue();
            Path.GetDirectoryName(actual).Should().Be(Paths.TrimTrailingSlashes(Path.GetTempPath()));
        } finally {
            Directory.Delete(actual);
        }
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData(@"", @"")]
    [InlineData(@"abc", @"abc")]
    [InlineData(@"abc/def", @"abc/def")]
    [InlineData(@"abc\def", @"abc/def")]
    [InlineData(@"abc\\def", @"abc//def")]
    [InlineData(@"a\b/c", @"a/b/c")]
    public void Dos2UnixSlashes(string? input, string? expected) {
        Paths.Dos2UnixSlashes(input).Should().Be(expected);
    }

    private static readonly FrozenSet<string> FILE_EXTENSIONS = new HashSet<string> { ".jpg", ".jpeg", ".png", ".gif" }.ToFrozenSet();

    [Theory]
    [InlineData("photo.jpg")]
    [InlineData(@"C:\fakepath\photo.jpeg")]
    [InlineData("img.PNG")]
    [InlineData("anim.2.Gif")]
    public void MatchesExtensions(string filename) {
        Paths.MatchesExtensions(filename, FILE_EXTENSIONS).Should().BeTrue();
    }

    [Theory]
    [InlineData("photo.vbs")]
    [InlineData(@"C:\fakepath\photo.exe")]
    [InlineData("img.SCR")]
    [InlineData("anim.2.Ocx")]
    [InlineData("jpg")]
    public void DoesNotMatchExtensions(string filename) {
        Paths.MatchesExtensions(filename, FILE_EXTENSIONS).Should().BeFalse();
    }

}