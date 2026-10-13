using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace Unfucked;

/// <summary>
/// Methods that make it easier to work with processes and arguments.
/// </summary>
public static class Processes {

    /// <summary>Triggered when a process is started using <see cref="Processes.ExecFile(string,IEnumerable{string})"/>. Useful for cross-cutting logging and debugging.</summary>
    public static EventHandler<ProcessStartInfo>? FileExecuted;

    extension(Process) {

        /// <summary>
        /// Combine a sequence of arguments into a single command-line string with escaping using .NET rules.
        /// </summary>
        /// <param name="args">Zero or more command-line arguments</param>
        /// <returns>A single string that contains all the arguments from <paramref name="args"/>, in order, with escaping.</returns>
        [Pure]
        public static string CommandLineToString(params IEnumerable<string> args) {
            StringBuilder lineBuilder = new(), argBuilder = new();

            foreach (string arg in args) {
                bool surroundWithDoubleQuotationMarks = false;
                argBuilder.Length   = 0;
                argBuilder.Capacity = Math.Max(arg.Length + 4, Math.Min(argBuilder.Capacity, 1024));

                if (lineBuilder.Length != 0) {
                    lineBuilder.Append(' ');
                }

                for (int i = 0; i < arg.Length; i++) {
                    char chr = arg[i];
                    switch (chr) {
                        case ' ':
                            surroundWithDoubleQuotationMarks = true;
                            break;
                        case '"':
                            surroundWithDoubleQuotationMarks = true;
                            argBuilder.Append('\\');
                            break;
                        case '\\' when i + 1 < arg.Length && arg[i + 1] == '"':
                            argBuilder.Append('\\');
                            break;
                    }
                    argBuilder.Append(chr);
                }

                if (surroundWithDoubleQuotationMarks) {
                    lineBuilder.Append('"');
                }
                lineBuilder.Append(argBuilder);
                if (surroundWithDoubleQuotationMarks) {
                    lineBuilder.Append('"');
                }
            }

            return lineBuilder.ToString();
        }

        /// <summary>
        /// Run a program, wait for it to exit, and return its exit code, stdout, and stderr.
        /// </summary>
        /// <remarks>Inspired by Node.js' <c>child_process.execFile()</c>: <see href="https://nodejs.org/api/child_process.html#child_processexecfilefile-args-options-callback"/></remarks>
        /// <param name="file">The name or path of the executable file to run</param>
        /// <param name="arguments">Arguments to pass to the child process</param>
        /// <returns>Struct that contains the numeric exit code of the child process, the standard output text, and the standard error text, or <c>null</c> if the child process failed to start.</returns>
#pragma warning disable Ex0100 // No cancellation token is passed, so it can't be canceled.
        public static Task<ProcessResult> ExecFile(string file, params IEnumerable<string> arguments) => Process.ExecFile(file, arguments, extraEnvironment: null);
#pragma warning restore Ex0100

        /// <summary>
        /// Run a program, wait for it to exit, and return its exit code, stdout, and stderr.
        /// </summary>
        /// <remarks>Inspired by Node.js' <c>child_process.execFile()</c>: <see href="https://nodejs.org/api/child_process.html#child_processexecfilefile-args-options-callback"/></remarks>
        /// <param name="file">The name or path of the executable file to run</param>
        /// <param name="arguments">Arguments to pass to the child process</param>
        /// <param name="workingDirectory">The working directory that the child process should start executing in, or <c>null</c> to inherit the current working directory from this process.</param>
        /// <param name="extraEnvironment">Environment key-value pairs to be passed to the child process, in addition to the current process' environment. To prevent an environment variable from being inherited by the child, add its name to this dictionary with the value <c>null</c>. To make the child process inherit this process' environment with no changes, leave this argument set to <c>null</c>.</param>
        /// <param name="hideWindow"><c>true</c> on Windows to attempt to hide the child process' window, useful for console applications that force a command prompt window to appear, or <c>false</c> to use the default behavior for the child process. Has no effect on other operating systems.</param>
        /// <param name="cancellationToken">Used to stop waiting if the process is taking too long to exit. Does not kill the process on cancellation.</param>
        /// <returns>Struct that contains the numeric exit code of the child process, the trimmed standard output text, and the trimmed standard error text, or <c>(-1, "", "")</c> if the child process failed to start.</returns>
        /// <exception cref="OperationCanceledException"><paramref name="cancellationToken"/> was canceled before the child process exited. The child process will still be running at this point—cancelling this method does not kill it. To get information about the running child process, for example if you want to kill it yourself, you can read the integer <c>pid</c> value from the <see cref="Exception.Data"/> dictionary to pass to <see cref="Process.GetProcessById(int)"/> and call <see cref="Process.Kill()"/>.</exception>
        public static async Task<ProcessResult> ExecFile(
            string file,
            IEnumerable<string> arguments,
            // ReSharper disable once MethodOverloadWithOptionalParameter - there are subsequent optional parameters here the user might want to pass, otherwise the other overload is fine to use
            string workingDirectory = "",
            IDictionary<string, string?>? extraEnvironment = null,
            bool hideWindow = false,
            CancellationToken cancellationToken = default) {

            Process? process;
            ProcessStartInfo processStartInfo = new(file) {
                UseShellExecute        = false,
                RedirectStandardOutput = true,
                RedirectStandardError  = true,
                WorkingDirectory       = workingDirectory,
                CreateNoWindow         = hideWindow
            };

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP2_1_OR_GREATER
            processStartInfo.ArgumentList.AddAll(arguments);
#else
            processStartInfo.Arguments = Process.CommandLineToString(arguments);
#endif

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

            try {
                FileExecuted?.Invoke(null, processStartInfo);
                process = Process.Start(processStartInfo);
            } catch (Win32Exception) {
                return new ProcessResult { CommandLine = getCommandLine() };
            } catch (PlatformNotSupportedException) {
                return new ProcessResult { CommandLine = getCommandLine() };
            }

            if (process == null) {
                return new ProcessResult { CommandLine = getCommandLine() };
            }

            using (process) {
                Task<string> stdout;
                Task<string> stderr;

                try {
#if NET7_0_OR_GREATER
                    stdout = process.StandardOutput.ReadToEndAsync(cancellationToken);
                    stderr = process.StandardError.ReadToEndAsync(cancellationToken);
#else
                    stdout = process.StandardOutput.ReadToEndAsync();
                    stderr = process.StandardError.ReadToEndAsync();
#endif

#if NET5_0_OR_GREATER
                    await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
#else
                    TaskCompletionSource<bool> exited = new();
                    process.Exited += (_, _) => exited.TrySetResult(true);
                    if (!process.HasExited) {
                        process.EnableRaisingEvents = true;
                        if (!process.HasExited) {
                            cancellationToken.Register(() => exited.TrySetCanceled(cancellationToken));
                            await exited.Task.ConfigureAwait(false);
                        }
                    }
#endif
                } catch (TaskCanceledException e) {
                    throw new OperationCanceledException(e.Message, e) { Data = { { "pid", process.Id } } };
                } catch (OperationCanceledException e) {
                    e.Data["pid"] = process.Id;
                    throw;
                }

                string[] std = await Task.WhenAll(stdout, stderr).ConfigureAwait(false);
                return new ProcessResult(process.ExitCode, std[0].Trim(), std[1].Trim()) { CommandLine = getCommandLine() };

            }

            string getCommandLine() {
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP2_1_OR_GREATER
                return Process.CommandLineToString(processStartInfo.ArgumentList);
#else
                return processStartInfo.Arguments;
#endif
            }
        }

        /// <summary>
        /// Get a program's filename without the <c>.exe</c> file extension if it had one.
        /// </summary>
        /// <param name="processName">The filename of a program with or without a <c>.exe</c> file extension.</param>
        /// <returns><paramref name="processName"/> without the trailing <c>.exe</c> suffix, or <paramref name="processName"/> unmodified if it did not have an <c>.exe</c> suffix.</returns>
        [Pure]
        public static string StripExeSuffix(string processName) => processName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) ? processName.Substring(0, processName.Length - 4) : processName;

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

    extension(Environment) {

        /// <summary>
        /// Determine whether the current process was compiled as a console or Windows GUI program.
        /// </summary>
        /// <remarks>Source: <see href="https://stackoverflow.com/questions/1188658/how-can-a-c-sharp-windows-console-application-tell-if-it-is-run-interactively/8711036#8711036"/></remarks>
        /// <returns><c>true</c> if the current process was compiled as a Windows GUI program, or <c>false</c> if it was compiled either for *nix or as a Windows console program.</returns>
        [Pure]
        public static bool IsWindowsGuiProgram => Environment.OSVersion.Platform == PlatformID.Win32NT
            && GetModuleHandleW() is var baseAddr && Marshal.ReadInt16(baseAddr, Marshal.ReadInt32(baseAddr, 0x3C) + 0x5C) == 2;

    }

    [DllImport("kernel32.dll")]
    private static extern IntPtr GetModuleHandleW(IntPtr filename = default);

}

/// <summary>
/// The result of a process exiting.
/// </summary>
public readonly record struct ProcessResult {

    /// <inheritdoc cref="ProcessResult" />
    /// <param name="exitCode">Status code that the process returned when it exited, or -1 if the process failed to start.</param>
    /// <param name="stdOut">Trimmed text the process printed to its standard output stream, or <see cref="string.Empty"/> if the process failed to start.</param>
    /// <param name="stdErr">Trimmed text the process printed to its standard error stream, or <see cref="string.Empty"/> if the process failed to start.</param>
    public ProcessResult(int exitCode, string stdOut, string stdErr) {
        ExitCode = exitCode;
        StdOut   = stdOut;
        StdErr   = stdErr;
    }

    /// <summary>The result of a process that failed to start, for example because the program's file was not found. Equivalent to <c>default(<see cref="ProcessResult"/>)</c>.</summary>
    public ProcessResult(): this(-1, string.Empty, string.Empty) {}

    /// <summary>The program filename and arguments used to start the process.</summary>
    public string? CommandLine { get; init; }

    /// <summary>The offset is used to make <c>default(<see cref="ProcessResult"/>)</c> represent a failed execution with <see cref="ExitCode"/> of -1.</summary>
    private readonly long exitCodePlusOne = 0;

    /// <summary>Status code that the process returned when it exited, or -1 if the process failed to start.</summary>
    public int ExitCode {
        get => (int) (exitCodePlusOne - 1L);
        init => exitCodePlusOne = value + 1L;
    }

    /// <summary>Trimmed text the process printed to its standard output stream, or <see cref="string.Empty"/> if the process failed to start.</summary>
    public string StdOut { get; init; } = string.Empty;

    /// <summary>Trimmed text the process printed to its standard error stream, or <see cref="string.Empty"/> if the process failed to start.</summary>
    public string StdErr { get; init; } = string.Empty;

    /// <summary>Whether the process exited with status code 0 (<c>true</c>) or not (<c>false</c>).</summary>
    public bool IsSuccessful => ExitCode == 0;

    /// <summary>Throw a <see cref="ProcessException"/> with a custom message if <see cref="ExitCode"/> is not 0.</summary>
    /// <param name="message">Custom message to include in the exception, or <c>null</c> to generate a default message.</param>
    /// <param name="includeDetails">Zero or more properties to include after the message, defaults to no details.</param>
    /// <returns>This <see cref="ProcessResult"/> instance for chaining.</returns>
    /// <exception cref="ProcessException">The process exited with a non-zero exit code, or failed to start.</exception>
    public ProcessResult EnsureSuccessful(string? message = null, Details includeDetails = default) {
        if (IsSuccessful) {
            return this;
        } else {
            throw new ProcessException(GenerateMessage(message, includeDetails), this);
        }
    }

    /// <summary>Throw a supplied <see cref="Exception"/> instance if <see cref="ExitCode"/> is not 0.</summary>
    /// <param name="exception">Exception instance to throw.</param>
    /// <returns>This <see cref="ProcessResult"/> instance for chaining.</returns>
    /// <exception cref="Exception">The process exited with a non-zero exit code, or failed to start.</exception>
    public ProcessResult EnsureSuccessful(Exception exception) {
        if (IsSuccessful) {
            return this;
        } else {
            throw exception;
        }
    }

    /// <summary>Generate and throw an <see cref="Exception"/> if <see cref="ExitCode"/> is not 0.</summary>
    /// <param name="exceptionGenerator">Generates an <see cref="Exception"/> instance for this <see cref="ProcessResult"/>.</param>
    /// <returns>This <see cref="ProcessResult"/> instance for chaining.</returns>
    /// <exception cref="Exception">The process exited with a non-zero exit code, or failed to start.</exception>
    public ProcessResult EnsureSuccessful(Func<ProcessResult, Exception> exceptionGenerator) {
        if (IsSuccessful) {
            return this;
        } else {
            throw exceptionGenerator(this);
        }
    }

    /// <summary>Construct and throw a <typeparamref name="TException"/> with a custom message if <see cref="ExitCode"/> is not 0.</summary>
    /// <param name="message">Custom message to include in the exception, or <c>null</c> to generate a default message.</param>
    /// <param name="includeDetails">Zero or more properties to include in the message.</param>
    /// <returns>This <see cref="ProcessResult"/> instance for chaining.</returns>
    /// <exception cref="Exception"><typeparamref name="TException"/>: The process exited with a non-zero exit code, or failed to start.</exception>
    public ProcessResult EnsureSuccessful<TException>(string? message, Details includeDetails = default) where TException: Exception {
        ProcessResult self = this;
        return EnsureSuccessful<TException>(_ => self.GenerateMessage(message, includeDetails));
    }

    /// <summary>Construct and throw a <typeparamref name="TException"/> if <see cref="ExitCode"/> is not 0.</summary>
    /// <param name="messageGenerator">Generates a message to pass to the <typeparamref name="TException"/> constructor.</param>
    /// <returns>This <see cref="ProcessResult"/> instance for chaining.</returns>
    /// <exception cref="Exception"><typeparamref name="TException"/>: The process exited with a non-zero exit code, or failed to start.</exception>
    public ProcessResult EnsureSuccessful<TException>(Func<ProcessResult, string?> messageGenerator) where TException: Exception {
        if (IsSuccessful) {
            return this;
        } else {
            throw ConstructException<TException>(messageGenerator(this));
        }
    }

    private TException ConstructException<TException>(string? message) where TException: Exception {
        Type   returnType = typeof(TException);
        object returnValue;
        if (returnType == typeof(Exception)) {
            returnValue = new Exception(message);
        } else if (returnType == typeof(ApplicationException)) {
            returnValue = new ApplicationException(message);
        } else if (returnType == typeof(ProcessException)) {
            returnValue = new ProcessException(message, this);
        } else if (returnType.GetConstructor([typeof(string)]) is {} constructor) {
            returnValue = constructor.Invoke([message]);
        } else if (returnType.GetConstructor([typeof(string), typeof(Exception)]) is {} constructor2) {
            returnValue = constructor2.Invoke([message, null]);
        } else {
            throw new ArgumentException($"{returnType.FullName} does not have a constructor with either of the signatures (string) or (string, Exception)");
        }
        return (TException) returnValue;
    }

    private string GenerateMessage(string? message, Details includeDetails) {
        if (message == null) {
            message =
                $"Process {((includeDetails & Details.CommandLine) != 0 ? CommandLine + " " : "")}exited with {(includeDetails == Details.ExitCode ? $"status {ExitCode}" : "unsuccessful exit code")}{((includeDetails & Details.StdOut) != 0 ? $"\nstdout: {StdOut}" : "")}{((includeDetails & Details.StdErr) != 0 ? $"\nstderr: {StdErr}" : "")}";
        } else if (includeDetails != default) {
            message = new StringBuilder(message).Append(" (").Append(((IEnumerable<string?>) [
                (includeDetails & Details.ExitCode) != 0 ? $" exit code: {ExitCode}" : null,
                (includeDetails & Details.CommandLine) != 0 ? $" command: {CommandLine}" : null,
                (includeDetails & Details.StdOut) != 0 ? $" stdout: {StdOut}" : null,
                (includeDetails & Details.StdErr) != 0 ? $" stderr: {StdErr}" : null
            ]).Compact().Join(", ")).Append(')').ToString();
        }
        return message;
    }

    /// <summary>Explode the struct into individual variables for each of its properties.</summary>
    /// <param name="exitCode">Status code that the process returned when it exited, or -1 if the process failed to start.</param>
    /// <param name="stdOut">Trimmed text the process printed to its standard output stream, or <see cref="string.Empty"/> if the process failed to start.</param>
    public void Deconstruct(out int exitCode, out string stdOut) {
        exitCode = ExitCode;
        stdOut   = StdOut;
    }

    /// <inheritdoc cref="Deconstruct(out int,out string)" />
    /// <param name="stdErr">Trimmed text the process printed to its standard error stream, or <see cref="string.Empty"/> if the process failed to start.</param>
    public void Deconstruct(out int exitCode, out string stdOut, out string stdErr) {
        Deconstruct(out exitCode, out stdOut);
        stdErr = StdErr;
    }

    /// <summary>Which details, if any, should be included in an auto-generated exception message when a process exits with a non-zero exit code. Multiple options can be combined with <c>|</c>.</summary>
    [Flags]
    public enum Details: byte {

        /// <summary>Include <see cref="ProcessResult.ExitCode"/> in the exception message.</summary>
        ExitCode = 1 << 0,

        /// <summary>Include <see cref="ProcessResult.CommandLine"/> in the exception message.</summary>
        CommandLine = 1 << 1,

        /// <summary>Include <see cref="ProcessResult.StdOut"/> in the exception message.</summary>
        StdOut = 1 << 2,

        /// <summary>Include <see cref="ProcessResult.StdErr"/> in the exception message.</summary>
        StdErr = 1 << 3

    }

}

/// <summary>Process failed to start, or exited with a non-zero status code.</summary>
public sealed class ProcessException: Exception {

    /// <summary>The exit code, command line, stdout, and stderr of the process if it was able to start.</summary>
    public ProcessResult Result { get; }

    /// <inheritdoc cref="Exception(string,Exception)" />
    public ProcessException(string? message, Exception innerException): base(message, innerException) {}

    /// <summary>Instantiate a new object with the given message and process execution result.</summary>
    /// <param name="message">Description of the error.</param>
    /// <param name="result">The exit code, command line, stdout, and stderr of the process if it was able to start.</param>
    public ProcessException(string? message, ProcessResult result): base(message) {
        Result = result;
    }

}

/// <summary>Properties for <see cref="ProcessResult.Details"/>.</summary>
[EditorBrowsable(EditorBrowsableState.Advanced)]
public static class ProcessDetailsExtensions {

    extension(ProcessResult.Details) {

        /// <summary>Show all details in the error message (exit code, cmdline, stdout, and stderr).</summary>
        public static ProcessResult.Details All => ProcessResult.Details.ExitCode | ProcessResult.Details.CommandLine | ProcessResult.Details.StdOut | ProcessResult.Details.StdErr;

        /// <summary>Show all details except for the command line in the error message (exit code, stdout, and stderr).</summary>
        public static ProcessResult.Details NoCommandLine => ProcessResult.Details.ExitCode | ProcessResult.Details.StdOut | ProcessResult.Details.StdErr;

        /// <summary>Show exit code and command line in the error message, but neither stdout nor stderr.</summary>
        public static ProcessResult.Details NoOutput => ProcessResult.Details.ExitCode | ProcessResult.Details.CommandLine;

    }

}