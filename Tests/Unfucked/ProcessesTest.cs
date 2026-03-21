using System.Diagnostics;

namespace Tests.Unfucked;

public class ProcessesTest {

    [Fact]
    public async Task ExecFile() {
        ProcessResult gitResult = await Processes.ExecFile("git", "--version");

        gitResult.Should().NotBeNull();
        gitResult.ExitCode.Should().Be(0);
        gitResult.StdErr.Should().BeEmpty();
        gitResult.StdOut.Should().StartWith("git version ");
    }

    [Fact]
    public async Task ExecFileWithEnvironment() {
        ProcessResult gitResult =
            await Processes.ExecFile("git", ["--version"], new Dictionary<string, string?> { { "abc", "def" }, { "removeme", null } });

        gitResult.Should().NotBeNull();
        gitResult.ExitCode.Should().Be(0);
        gitResult.StdErr.Should().BeEmpty();
        gitResult.StdOut.Should().StartWith("git version ");
    }

    [Fact]
    public async Task ExecMissingFile() {
        ProcessResult gitResult = await Processes.ExecFile("missing_file");

        gitResult.Should().NotBeNull();
        gitResult.ExitCode.Should().Be(-1);
    }

    [Fact]
    public async Task CancelExecFile() {
        CancellationTokenSource cts  = new();
        Task<ProcessResult>     task = Processes.ExecFile("ping", ["127.0.0.1"], cancellationToken: cts.Token);

        cts.Cancel();

        try {
            await task;
            Assert.Fail("Task should have asynchronously thrown a TaskCanceledException");
        } catch (OperationCanceledException e) {
            int? pid = e.Data["pid"] as int?;
            pid.Should().BeGreaterThan(0);
            using Process childProcess = Process.GetProcessById(pid.Value);
            childProcess.Kill();
        }
    }

    /*
     * On Windows, this is a race to get the self process descendants before 4 pings are sent and received (about 3 seconds total).
     */
    /*[Fact]
    public void GetDescendantProcesses() {
        using Process self  = Process.GetCurrentProcess();
        using Process child = Process.Start("ping", "127.0.0.1");
        try {
            int childId = child.Id;

            IEnumerable<Process> actual = self.GetDescendantProcesses().ToList();
            actual.Should().Contain(process => process.Id == childId && "ping".Equals(process.ProcessName, StringComparison.InvariantCultureIgnoreCase));

        } finally {
            child.Kill();
        }
    }*/

}