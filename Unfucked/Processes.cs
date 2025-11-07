using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

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
    public static string CommandLineToString(params IEnumerable<string> args) {
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
    /// Run a program, wait for it to exit, and return its exit code, stdout, and stderr.
    /// </summary>
    /// <remarks>Inspired by Node.js' <c>child_process.execFile()</c>: <see href="https://nodejs.org/api/child_process.html#child_processexecfilefile-args-options-callback"/></remarks>
    /// <param name="file">The name or path of the executable file to run</param>
    /// <param name="arguments">Arguments to pass to the child process</param>
    /// <returns>Tuple that contains the numeric exit code of the child process, the standard output text, and the standard error text, or <c>null</c> if the child process failed to start.</returns>
#pragma warning disable Ex0100 // No cancellation token is passed, so it can't be canceled.
    public static Task<(int exitCode, string stdout, string stderr)> ExecFile(string file, params IEnumerable<string> arguments) => ExecFile(file, arguments, extraEnvironment: null);
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
    /// <returns>Tuple that contains the numeric exit code of the child process, the standard output text, and the standard error text, or <c>(-1, "", "")</c> if the child process failed to start.</returns>
    /// <exception cref="OperationCanceledException"><paramref name="cancellationToken"/> was canceled before the child process exited. The child process will still be running at this point—cancelling this method does not kill it. To get information about the running child process, for example if you want to kill it yourself, you can read the integer <c>pid</c> value from the <see cref="Exception.Data"/> dictionary to pass to <see cref="Process.GetProcessById(int)"/> and call <see cref="Process.Kill()"/>.</exception>
    public static async Task<(int exitCode, string stdout, string stderr)> ExecFile(
        string file,
        IEnumerable<string>? arguments = null,
        IDictionary<string, string?>? extraEnvironment = null,
        string workingDirectory = "",
        bool hideWindow = false,
        CancellationToken cancellationToken = default) {

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
            return (-1, string.Empty, string.Empty);
        } catch (PlatformNotSupportedException) {
            return (-1, string.Empty, string.Empty);
        }

        if (process == null) {
            return (-1, string.Empty, string.Empty);
        }

        using (process) {
            Task<string> stdout;
            Task<string> stderr;

            try {
#if NET8_0_OR_GREATER
                stdout = process.StandardOutput.ReadToEndAsync(cancellationToken);
                stderr = process.StandardError.ReadToEndAsync(cancellationToken);
#else
                stdout = process.StandardOutput.ReadToEndAsync();
                stderr = process.StandardError.ReadToEndAsync();
#endif

#if NET6_0_OR_GREATER
                await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
#else
                TaskCompletionSource<bool> exited = new();
                process.Exited += (_, _) => exited.SetResult(true);
                cancellationToken.Register(() => exited.TrySetCanceled(cancellationToken));
                await exited.Task.ConfigureAwait(false);
#endif
            } catch (TaskCanceledException e) {
                throw new OperationCanceledException(e.Message, e) { Data = { { "pid", process.Id } } };
            } catch (OperationCanceledException e) {
                e.Data["pid"] = process.Id;
                throw;
            }

            string[] std = await Task.WhenAll(stdout, stderr).ConfigureAwait(false);
            return (process.ExitCode, std[0].Trim(), std[1].Trim());
        }
    }

    /// <summary>
    /// Get a program's filename without the <c>.exe</c> file extension if it had one.
    /// </summary>
    /// <param name="processName">The filename of a program with or without a <c>.exe</c> file extension.</param>
    /// <returns><paramref name="processName"/> without the trailing <c>.exe</c> suffix, or <paramref name="processName"/> unmodified if it did not have an <c>.exe</c> suffix.</returns>
    [Pure]
    public static string StripExeSuffix(string processName) => processName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) ? processName.Substring(0, processName.Length - 4) : processName;

    /// <summary>
    /// Determine whether the current process was compiled as a console or Windows GUI program.
    /// </summary>
    /// <remarks>Source: <see href="https://stackoverflow.com/questions/1188658/how-can-a-c-sharp-windows-console-application-tell-if-it-is-run-interactively/8711036#8711036"/></remarks>
    /// <returns><c>true</c> if the current process was compiled as a Windows GUI program, or <c>false</c> if it was compiled either for *nix or as a Windows console program.</returns>
    [Pure]
    public static bool IsWindowsGuiProgram() => Environment.OSVersion.Platform == PlatformID.Win32NT
        && GetModuleHandleW() is var baseAddr && Marshal.ReadInt16(baseAddr, Marshal.ReadInt32(baseAddr, 0x3C) + 0x5C) == 2;

    [DllImport("kernel32.dll")]
    private static extern IntPtr GetModuleHandleW(IntPtr filename = default);

}