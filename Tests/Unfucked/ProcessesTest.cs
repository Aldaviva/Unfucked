using System.Diagnostics;

namespace Tests.Unfucked;

public class ProcessesTest {

    [Fact]
    public async Task ExecFile() {
        ProcessResult gitResult = await Process.ExecFile("git", "--version");

        gitResult.Should().NotBeNull();
        gitResult.ExitCode.Should().Be(0);
        gitResult.StdErr.Should().BeEmpty();
        gitResult.StdOut.Should().StartWith("git version ");
    }

    [Fact]
    public async Task ExecFileWithEnvironment() {
        ProcessResult gitResult =
            await Process.ExecFile("git", ["--version"], new Dictionary<string, string?> { { "abc", "def" }, { "removeme", null } });

        gitResult.Should().NotBeNull();
        gitResult.ExitCode.Should().Be(0);
        gitResult.StdErr.Should().BeEmpty();
        gitResult.StdOut.Should().StartWith("git version ");
    }

    [Fact]
    public async Task ExecMissingFile() {
        ProcessResult gitResult = await Process.ExecFile("missing_file");

        gitResult.Should().NotBeNull();
        gitResult.ExitCode.Should().Be(-1);
    }

    [Fact]
    public async Task CancelExecFile() {
        CancellationTokenSource cts  = new();
        Task<ProcessResult>     task = Process.ExecFile("ping", ["127.0.0.1"], cancellationToken: cts.Token);

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

    /*private static string commandLineToArgv(IEnumerable<string> args) {
        /*
         * put backslashes to the left of each existing backslash
         * if an arg has spaces, surround it with a pair of double quotation marks
         * if an arg has double quotation marks, replace each double quotation mark with triple double quotation marks
         #1#
        // return string.Join(" ", args);
        /*return string.Join(" ", args.Select(token => {
            string escaped = token.Replace(@"\", @"\\").Replace("\"", "\"\"\"");
            if (escaped.Contains(' ')) {
                escaped = '"' + escaped + '"';
            }
            return escaped;
        }));#1#

        StringBuilder lineBuilder = new(), argBuilder = new();

        foreach (string token in args) {
            bool needsSurroundingSpaces = false;
            argBuilder.Length   = 0;
            argBuilder.Capacity = Math.Max(token.Length + 4, Math.Min(argBuilder.Capacity, 1024));

            if (lineBuilder.Length != 0) {
                lineBuilder.Append(' ');
            }

            for (int i = 0; i < token.Length; i++) {
                char c = token[i];
                switch (c) {
                    case ' ':
                        needsSurroundingSpaces = true;
                        break;
                    case '"':
                        needsSurroundingSpaces = true;
                        argBuilder.Append('\\');
                        break;
                    case '\\' when i + 1 < token.Length && token[i + 1] == '"':
                        argBuilder.Append('\\');
                        break;
                }
                argBuilder.Append(c);
            }

            if (needsSurroundingSpaces) {
                lineBuilder.Append('"');
            }
            lineBuilder.Append(argBuilder);
            if (needsSurroundingSpaces) {
                lineBuilder.Append('"');
            }
        }

        return lineBuilder.ToString();
    }*/

    [Fact]
    public void CommandLineToArgv() {
        IEnumerable<string> arguments = [
            "a",
            "has spaces",
            "single\\backslash",
            "double\\\\backslash",
            "triple\\\\\\backslash",
            "\"single\"",
            "\"\"double\"\"",
            "\"\"\"triple\"\"\"",
            "single\"double",
            "double\"\"double",
            "triple\"\"\"double",
            "single\'single",
            "double\'\'single",
            "triple\'\'\'single",
            "--outfile=\"C:\\Path To\\file.txt\"",
            "--outdir=\"C:\\Path To\\directory\\\"",
            "foo\\\""
        ];

        const string expected =
            """
            a "has spaces" single\backslash double\\backslash triple\\\backslash "\"single\"" "\"\"double\"\"" "\"\"\"triple\"\"\"" "single\"double" "double\"\"double" "triple\"\"\"double" single'single double''single triple'''single "--outfile=\"C:\Path To\file.txt\"" "--outdir=\"C:\Path To\directory\\\"" "foo\\\""
            """;

        string actual = Process.CommandLineToString(arguments);
        actual.Should().Be(expected);
    }

    /*[Theory, MemberData(nameof(CommandLineToArgvData))]
    public void CommandLineToArgv(IEnumerable<string> arguments, string expected) {
        commandLineToArgv(arguments).Should().Be(expected);
    }

    public static TheoryData<IEnumerable<string>, string> CommandLineToArgvData {
        get {
            var data = new TheoryData<IEnumerable<string>, string>();
            data.Add(["/a"], """
            /a
            """);

            data.Add(["/a", "/b", @"c:\temp"], """
            /a /b c:\\temp
            """);

            data.Add(["/a", "literal string arg"], """
            /a "literal string arg"
            """);

            data.Add(["/a", @"/b:""quoted string"""], """"
            /a /b:"""quoted string"""
            """");

            return data;
        }
    }*/

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