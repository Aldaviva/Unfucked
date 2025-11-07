namespace Tests.Unfucked;

public class ExceptionsTest {

    [Fact]
    public void MessageChain() {
        Exception e = new("a", new ApplicationException("b", new InvalidOperationException("c")));
        e.MessageChain().Should().Be("Exception: a; ApplicationException: b; InvalidOperationException: c");
    }

    [Fact]
    public void MessageChainWithoutClassNames() {
        Exception e = new("a", new ApplicationException("b", new InvalidOperationException("c")));
        e.MessageChain(false).Should().Be("a: b: c");
    }

    [Fact]
    public void CauseChain() {
        InvalidOperationException c      = new("c");
        ApplicationException      b      = new("b", c);
        Exception                 a      = new("a", b);
        IList<Exception>          actual = a.CauseChain().ToList();

        actual.Should().HaveCount(2);
        actual[0].Should().BeSameAs(b);
        actual[1].Should().BeSameAs(c);
    }

    [Fact]
    public void IsCausedByExistingWindowsFile() {
        new IOException().IsCausedByExistingWindowsFile().Should().BeFalse();

        string tempFile = Path.GetTempFileName();
        try {
            Func<FileStream> thrower = () => new FileStream(tempFile, FileMode.CreateNew, FileAccess.Write);
            thrower.Should().Throw<IOException>().And.IsCausedByExistingWindowsFile().Should().BeTrue();
        } finally {
            File.Delete(tempFile);
        }
    }

}