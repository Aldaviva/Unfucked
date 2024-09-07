using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
#if NET6_0_OR_GREATER
using System.Runtime.Versioning;
#endif

namespace Unfucked;

/// <summary>
/// Methods that make it easier to work with processes and arguments.
/// </summary>
public static class Processes {

    /// <summary>
    /// Combine a sequence of arguments into a single command-line string with quotation marks and escaping using Windows rules.
    /// </summary>
    /// <param name="args">Zero or more command-line arguments</param>
    /// <returns>A single string that contains all the arguments from <paramref name="args"/>, in order, with escaping and quotation marks.</returns>
    /// <remarks>
    /// From MSDN: <see href="https://stackoverflow.com/a/2611075/979493" />
    /// </remarks>
    [ExcludeFromCodeCoverage]
    [Pure]
    public static string CommandLineToString(IEnumerable<string> args) {
        StringBuilder sb = new();
        foreach (string s in args) {
            sb.Append('"');
            // Escape double quotes (") and backslashes (\).
            int searchIndex = 0;
            while (true) {
                // Put this test first to support zero length strings.
                if (searchIndex >= s.Length) {
                    break;
                }

                int quoteIndex = s.IndexOf('"', searchIndex);
                if (quoteIndex < 0) {
                    break;
                }

                sb.Append(s, searchIndex, quoteIndex - searchIndex);
                EscapeBackslashes(sb, s, quoteIndex - 1);
                sb.Append('\\');
                sb.Append('"');
                searchIndex = quoteIndex + 1;
            }

            sb.Append(s, searchIndex, s.Length - searchIndex);
            EscapeBackslashes(sb, s, s.Length - 1);
            sb.Append(@""" ");
        }

        return sb.ToString(0, Math.Max(0, sb.Length - 1));
    }

    [ExcludeFromCodeCoverage]
    private static void EscapeBackslashes(StringBuilder sb, string s, int lastSearchIndex) {
        // Backslashes must be escaped if and only if they precede a double quote.
        for (int i = lastSearchIndex; i >= 0; i--) {
            if (s[i] != '\\') {
                break;
            }

            sb.Append('\\');
        }
    }

    /// <summary>
    /// Split a single command-line string into a sequence of individual arguments using Windows rules.
    /// </summary>
    /// <param name="commandLine">A command-line string, possibly consisting of multiple arguments, escaping, and quotation marks.</param>
    /// <returns>An enumerable of the individual arguments in <paramref name="commandLine"/>, unescaped and unquoted.</returns>
    /// <remarks>
    /// By Mike Schwörer: <see href="https://stackoverflow.com/a/64236441/979493" />
    /// </remarks>
    [ExcludeFromCodeCoverage]
    [Pure]
    public static IEnumerable<string> CommandLineToEnumerable(string commandLine) {
        StringBuilder result = new();

        bool quoted     = false;
        bool escaped    = false;
        bool started    = false;
        bool allowcaret = false;
        for (int i = 0; i < commandLine.Length; i++) {
            char chr = commandLine[i];

            if (chr == '^' && !quoted) {
                if (allowcaret) {
                    result.Append(chr);
                    started    = true;
                    escaped    = false;
                    allowcaret = false;
                } else if (i + 1 < commandLine.Length && commandLine[i + 1] == '^') {
                    allowcaret = true;
                } else if (i + 1 == commandLine.Length) {
                    result.Append(chr);
                    started = true;
                    escaped = false;
                }
            } else if (escaped) {
                result.Append(chr);
                started = true;
                escaped = false;
            } else if (chr == '"') {
                quoted  = !quoted;
                started = true;
            } else if (chr == '\\' && i + 1 < commandLine.Length && commandLine[i + 1] == '"') {
                escaped = true;
            } else if (chr == ' ' && !quoted) {
                if (started) yield return result.ToString();
                result.Clear();
                started = false;
            } else {
                result.Append(chr);
                started = true;
            }
        }

        if (started) yield return result.ToString();
    }

    /// <summary>
    /// Run a program, wait for it to exit, and return its exit code, stdout, and stderr.
    /// </summary>
    /// <remarks>Inspired by Node.js' <c>child_process.execFile()</c>: <see href="https://nodejs.org/api/child_process.html#child_processexecfilefile-args-options-callback"/></remarks>
    /// <param name="file">The name or path of the executable file to run</param>
    /// <param name="arguments">Arguments to pass to the child process</param>
    /// <returns>Tuple that contains the numeric exit code of the child process, the standard output text, and the standard error text, or <c>null</c> if the child process failed to start.</returns>
#pragma warning disable Ex0100 // No cancellation token is passed, so it can't be canceled.
    public static Task<(int exitCode, string standardOutput, string standardError)?> ExecFile(string file, params string[] arguments) => ExecFile(file, arguments.AsEnumerable());
#pragma warning restore Ex0100

    /// <summary>
    /// Run a program, wait for it to exit, and return its exit code, stdout, and stderr.
    /// </summary>
    /// <remarks>Inspired by Node.js' <c>child_process.execFile()</c>: <see href="https://nodejs.org/api/child_process.html#child_processexecfilefile-args-options-callback"/></remarks>
    /// <param name="file">The name or path of the executable file to run</param>
    /// <param name="arguments">Arguments to pass to the child process</param>
    /// <param name="extraEnvironment">Environment key-value pairs to be passed to the child process, in addition to the current process' environment. To prevent an environment variable from being inherited by the child, add its name to this dictionary with the value <c>null</c>. To make the child process inherit this process' environment with no changes, leave this argument set to <c>null</c>.</param>
    /// <param name="workingDirectory">The working directory that the child process should start executing in, or <c>null</c> to inherit the current working directory from this process.</param>
    /// <param name="hideWindow"><c>true</c> on Windows to attempt to hide the child process' window, useful for console applications that force a command prompt window to appear, or <c>false</c> to use the default behavior for the child process. Has no effect on other operating systems.</param>
    /// <param name="cancellationToken">Used to stop waiting if the process is taking too long to exit. Does not kill the process on cancellation.</param>
    /// <returns>Tuple that contains the numeric exit code of the child process, the standard output text, and the standard error text, or <c>null</c> if the child process failed to start.</returns>
    /// <exception cref="TaskCanceledException"><paramref name="cancellationToken"/> was canceled before the child process exited. The child process will still be running at this point—cancelling this method does not kill it. To get information about the running child process, for example if you want to kill it yourself, you can read the integer <c>pid</c> value from the <see cref="Exception.Data"/> dictionary to pass to <see cref="Process.GetProcessById(int)"/>.</exception>
    public static async Task<(int exitCode, string standardOutput, string standardError)?> ExecFile(
        string                        file,
        IEnumerable<string>?          arguments         = null,
        IDictionary<string, string?>? extraEnvironment  = null,
        string                        workingDirectory  = "",
        bool                          hideWindow        = false,
        CancellationToken             cancellationToken = default) {

        Process? process;
        try {
            ProcessStartInfo processStartInfo = new(file) {
                UseShellExecute        = false,
                RedirectStandardOutput = true,
                RedirectStandardError  = true,
                WorkingDirectory       = workingDirectory,
                CreateNoWindow         = hideWindow
            };

            if (extraEnvironment is not null) {
                IDictionary<string, string?> processEnvironment = processStartInfo.Environment;
                foreach (KeyValuePair<string, string?> envVar in extraEnvironment) {
                    if (envVar.Value is null) {
                        processEnvironment.Remove(envVar.Key);
                    } else {
                        processEnvironment.Add(envVar);
                    }
                }
            }

            if (arguments != null) {
#if NET8_0_OR_GREATER
                processStartInfo.ArgumentList.AddAll(arguments);
#else
                processStartInfo.Arguments = CommandLineToString(arguments);
#endif
            }

            process = Process.Start(processStartInfo);
        } catch (Win32Exception) {
            return null;
        } catch (PlatformNotSupportedException) {
            return null;
        }

        if (process == null) {
            return null;
        }

        using (process) {
            ConfiguredTaskAwaitable<string> stdout;
            ConfiguredTaskAwaitable<string> stderr;

#if NET8_0_OR_GREATER
            stdout = process.StandardOutput.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
            stderr = process.StandardError.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
#else
            stdout = process.StandardOutput.ReadToEndAsync().ConfigureAwait(false);
            stderr = process.StandardError.ReadToEndAsync().ConfigureAwait(false);
#endif

            try {
#if NET6_0_OR_GREATER
                await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
#else
                TaskCompletionSource<bool> exited = new();
                process.Exited += (_, _) => exited.SetResult(true);
                cancellationToken.Register(() => exited.TrySetCanceled(cancellationToken));
                await exited.Task.ConfigureAwait(false);
#endif
            } catch (TaskCanceledException e) {
                e.Data["pid"] = process.Id;
                throw;
            }

            return (process.ExitCode, (await stdout).Trim(), (await stderr).Trim());
        }
    }

    /// <summary>
    /// <para>Gets the process that started a given child process.</para>
    /// <para>Windows only.</para>
    /// </summary>
    /// <param name="pid">The child process ID of which you want to find the parent, or <c>null</c> to get the parent of the current process.</param>
    /// <returns>The parent process of the child process that has the given <paramref name="pid"/>, or <c>null</c> if the process cannot be found (possibly because it already exited). Remember to <see cref="IDisposable.Dispose"/> the returned value.</returns>
    /// <inheritdoc cref="GetParentProcess(Process)" path="/remarks" />
#if NET6_0_OR_GREATER
    [SupportedOSPlatform("windows")]
#endif
    [ExcludeFromCodeCoverage]
    [Pure]
    public static Process? GetParentProcess(int? pid = null) {
        using Process process = pid.HasValue ? Process.GetProcessById(pid.Value) : Process.GetCurrentProcess();
        return GetParentProcess(process);
    }

    /// <summary>
    /// <para>Gets the process that started a given child process.</para>
    /// <para>Windows only.</para>
    /// </summary>
    /// <param name="child">The child process of which you want to find the parent.</param>
    /// <returns>The parent process of <paramref name="child"/>. Remember to <see cref="IDisposable.Dispose"/> the returned value.</returns>
    /// <remarks>
    /// <para>By Simon Mourier: <see href="https://stackoverflow.com/a/3346055/979493"/></para>
    /// </remarks>
#if NET6_0_OR_GREATER
    [SupportedOSPlatform("windows")]
#endif
    [ExcludeFromCodeCoverage]
    [Pure]
    public static Process? GetParentProcess(this Process child) {
        ProcessBasicInformation basicInfo = new();
        if (0 != NtQueryInformationProcess(child.Handle, 0, ref basicInfo, Marshal.SizeOf(basicInfo), out int _)) {
            return null;
        }

        try {
            return Process.GetProcessById(basicInfo.InheritedFromUniqueProcessId.ToInt32());
        } catch (ArgumentException) {
            // not found
            return null;
        }
    }

    [DllImport("ntdll.dll")]
    private static extern int NtQueryInformationProcess(IntPtr processHandle, int processInfoClass, ref ProcessBasicInformation processInfo, int processInfoLength, out int returnLength);

    /// <summary>
    /// <para>List all currently running processes that were started by the given <paramref name="ancestor"/> process, including transitively to an unlimited depth.</para>
    /// <para>Windows only.</para>
    /// </summary>
    /// <param name="ancestor">The parent, grandparent, or further process that started the processes to return</param>
    /// <returns>List of processes which were started by either <paramref name="ancestor"/>, one of its children, grandchildren, or further to an unlimited depth. Remember to <see cref="IDisposable.Dispose"/> all of these <see cref="Process"/> instances.</returns>
#if NET6_0_OR_GREATER
    [SupportedOSPlatform("windows")]
#endif
    [Pure]
    public static IEnumerable<Process> GetDescendantProcesses(this Process ancestor) {
        Process[] allProcesses = Process.GetProcesses();

        //eagerly find child processes, because once we start killing processes, their parent PIDs won't mean anything anymore
        List<Process> descendants = GetDescendantProcesses(ancestor, allProcesses).ToList();

        foreach (Process nonDescendant in allProcesses.Except(descendants, ProcessIdEqualityComparer.Instance)) {
            nonDescendant.Dispose();
        }

        return descendants;
    }

#if NET6_0_OR_GREATER
    [SupportedOSPlatform("windows")]
#endif
    [Pure]
    // ReSharper disable once ParameterTypeCanBeEnumerable.Local (Avoid double enumeration heuristic)
    private static IEnumerable<Process> GetDescendantProcesses(Process ancestor, Process[] allProcesses) =>
        allProcesses.SelectMany(descendant => {
            bool isDescendantOfParent = false;
            try {
                using Process? descendantParent = GetParentProcess(descendant);
                isDescendantOfParent = descendantParent?.Id == ancestor.Id;
            } catch (Exception e) when (e is not OutOfMemoryException) {
                //leave isDescendentOfParent false
            }

            return isDescendantOfParent ? GetDescendantProcesses(descendant, allProcesses).Prepend(descendant) : [];
        });

    private class ProcessIdEqualityComparer: IEqualityComparer<Process> {

        public static readonly ProcessIdEqualityComparer Instance = new();

        public bool Equals(Process? x, Process? y) => ReferenceEquals(x, y) || (x is not null && y is not null && x.GetType() == y.GetType() && x.Id == y.Id);

        public int GetHashCode(Process obj) => obj.Id;

    }

    [StructLayout(LayoutKind.Sequential)]
    [ExcludeFromCodeCoverage]
    private readonly struct ProcessBasicInformation {

        // These members must match PROCESS_BASIC_INFORMATION
        private readonly  IntPtr Reserved1;
        internal readonly IntPtr PebBaseAddress;
        private readonly  IntPtr Reserved2_0;
        private readonly  IntPtr Reserved2_1;
        internal readonly IntPtr UniqueProcessId;
        internal readonly IntPtr InheritedFromUniqueProcessId;

    }

}