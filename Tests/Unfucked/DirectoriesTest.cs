namespace Tests.Unfucked;

public class DirectoriesTest {

    [Fact]
    public void DeleteQuietlyIfExtant() {
        string tempDirectory = Paths.CreateTempDir();
        Directory.Exists(tempDirectory).Should().BeTrue();

        Directories.TryDelete(tempDirectory);
        Directory.Exists(tempDirectory).Should().BeFalse();
    }

    [Fact]
    public void DeleteQuietlyIfMissing() {
        string tempDirectory = Path.Combine(Path.GetTempPath(), "temp-" + Cryptography.GenerateRandomString(8));
        Directory.Exists(tempDirectory).Should().BeFalse();

        Directories.TryDelete(tempDirectory);
        Directory.Exists(tempDirectory).Should().BeFalse();
    }

}