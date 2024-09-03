using System.Diagnostics;

namespace Tests.Unfucked;

public class ProcessesTest {

    [Fact]
    public async Task ExecFile() {
        (int exitCode, string standardOutput, string standardError)? gitResult = await Processes.ExecFile("git", "--version");

        gitResult.Should().NotBeNull();
        gitResult!.Value.exitCode.Should().Be(0);
        gitResult.Value.standardError.Should().BeEmpty();
        gitResult.Value.standardOutput.Should().StartWith("git version ");
    }

    [Fact]
    public async Task ExecFileWithEnvironment() {

        (int exitCode, string standardOutput, string standardError)? gitResult =
            await Processes.ExecFile("git", ["--version"], new Dictionary<string, string?> { { "abc", "def" }, { "removeme", null } });

        gitResult.Should().NotBeNull();
        gitResult!.Value.exitCode.Should().Be(0);
        gitResult.Value.standardError.Should().BeEmpty();
        gitResult.Value.standardOutput.Should().StartWith("git version ");
    }

    [Fact]
    public async Task ExecMissingFile() {
        (int exitCode, string standardOutput, string standardError)? gitResult = await Processes.ExecFile("missing_file");

        gitResult.Should().BeNull();
    }

    /*
     * On Windows, this is a race to get the self process descendants before 4 pings are sent and received (about 3 seconds total).
     */
    [Fact]
    public void GetDescendantProcesses() {
        using Process self  = Process.GetCurrentProcess();
        using Process child = Process.Start("ping", "127.0.0.1");
        try {
            int childId = child.Id;

            IEnumerable<Process> actual = Processes.GetDescendantProcesses(self).ToList();
            actual.Should().Contain(process => process.Id == childId && "ping".Equals(process.ProcessName, StringComparison.InvariantCultureIgnoreCase));

        } finally {
            child.Kill();
        }
    }

}