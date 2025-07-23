namespace Tests.Unfucked;

public class FilesTest {

    [Fact]
    public void Delete() {
        string filePath = Path.GetTempFileName();
        File.SetAttributes(filePath, File.GetAttributes(filePath) | FileAttributes.ReadOnly);
        File.GetAttributes(filePath).Should().HaveFlag(FileAttributes.ReadOnly);
        ((Action) (() => File.Delete(filePath))).Should().Throw<UnauthorizedAccessException>();
        Files.Delete(filePath);
        File.Exists(filePath).Should().BeFalse("it should have been deleted");
    }

}