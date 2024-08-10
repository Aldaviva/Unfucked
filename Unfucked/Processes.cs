﻿using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace Unfucked;

public static class Processes {

    /// <summary>
    /// From https://stackoverflow.com/a/2611075/979493
    /// </summary>
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
    /// From https://stackoverflow.com/a/64236441/979493
    /// </summary>
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

    public static async Task<(int exitCode, string standardOutput, string standardError)?> ExecFile(string file, params string[] arguments) {
        Process? process;
        try {
            ProcessStartInfo processStartInfo = new(file) {
                UseShellExecute        = false,
                RedirectStandardOutput = true,
                RedirectStandardError  = true
            };
#if NET8_0_OR_GREATER
            processStartInfo.ArgumentList.AddAll(arguments);
#else
            processStartInfo.Arguments = CommandLineToString(arguments);
#endif

            process = Process.Start(processStartInfo);
        } catch (Win32Exception) {
            return null;
        }

        if (process == null) {
            return null;
        }

        using (process) {
            ConfiguredTaskAwaitable<string> stdout = process.StandardOutput.ReadToEndAsync().ConfigureAwait(false);
            ConfiguredTaskAwaitable<string> stderr = process.StandardError.ReadToEndAsync().ConfigureAwait(false);

#if NET6_0_OR_GREATER
            await process.WaitForExitAsync().ConfigureAwait(false);
#else
            TaskCompletionSource<bool> exited = new();
            process.Exited += (_, _) => exited.SetResult(true);
            await exited.Task.ConfigureAwait(false);
#endif

            return (process.ExitCode, (await stdout).Trim(), (await stderr).Trim());
        }
    }

    /// <summary>Gets the parent process of the current process.</summary>
    /// <returns>An instance of the Process class. Remember to <see cref="IDisposable.Dispose"/> the returned value.</returns>
    public static Process? GetParentProcess() {
        using Process currentProcess = Process.GetCurrentProcess();
        return GetParentProcess(currentProcess);
    }

    /// <summary>Gets the process that started a given child process.</summary>
    /// <param name="id">The child process ID of which you want to find the parent.</param>
    /// <returns>The parent process of the child process that has the given <paramref name="id"/>. Remember to <see cref="IDisposable.Dispose"/> the returned value.</returns>
    public static Process? GetParentProcess(int id) {
        using Process process = Process.GetProcessById(id);
        return GetParentProcess(process);
    }

    /// <summary>Gets the process that started a given child process.</summary>
    /// <param name="child">The child process of which you want to find the parent.</param>
    /// <returns>The parent process of <paramref name="child"/>. Remember to <see cref="IDisposable.Dispose"/> the returned value.</returns>
    public static Process? GetParentProcess(this Process child) {
        ProcessBasicInformation pbi = new();
        if (0 != NtQueryInformationProcess(child.Handle, 0, ref pbi, Marshal.SizeOf(pbi), out int _)) {
            return null;
        }

        try {
            return Process.GetProcessById(pbi.InheritedFromUniqueProcessId.ToInt32());
        } catch (ArgumentException) {
            // not found
            return null;
        }
    }

    [DllImport("ntdll.dll")]
    private static extern int NtQueryInformationProcess(IntPtr  processHandle, int processInfoClass, ref ProcessBasicInformation processInfo, int processInfoLength,
                                                        out int returnLength);

    public static IEnumerable<Process> GetDescendentProcesses(Process parent) {
        Process[] allProcesses = Process.GetProcesses();

        //eagerly find child processes, because once we start killing processes, their parent PIDs won't mean anything anymore
        List<Process> descendents = GetDescendentProcesses(parent, allProcesses).ToList();

        foreach (Process nonDescendant in allProcesses.Except(descendents, ProcessEqualityComparer.Instance)) {
            nonDescendant.Dispose();
        }

        return descendents;
    }

    // ReSharper disable once ParameterTypeCanBeEnumerable.Local (Avoid double enumeration heuristic)
    private static IEnumerable<Process> GetDescendentProcesses(Process parent, Process[] allProcesses) {
        return allProcesses.SelectMany(descendent => {
            bool isDescendentOfParent = false;
            try {
                using Process? descendentParent = GetParentProcess(descendent);
                isDescendentOfParent = descendentParent?.Id == parent.Id;
            } catch (Exception) {
                //leave isDescendentOfParent false
            }

            return isDescendentOfParent
                ? GetDescendentProcesses(descendent, allProcesses).Prepend(descendent)
                : Enumerable.Empty<Process>();
        });
    }

    private class ProcessEqualityComparer: IEqualityComparer<Process> {

        public static readonly ProcessEqualityComparer Instance = new();

        public bool Equals(Process x, Process y) {
            if (ReferenceEquals(x, y)) return true;
            if (ReferenceEquals(x, null)) return false;
            if (ReferenceEquals(y, null)) return false;
            if (x.GetType() != y.GetType()) return false;
            return x.Id == y.Id;
        }

        public int GetHashCode(Process obj) {
            return obj.Id;
        }

    }

    /// <summary>A utility class to determine a process parent.</summary>
    /// <remarks><a href="https://stackoverflow.com/a/3346055/979493">Source</a></remarks>
    [StructLayout(LayoutKind.Sequential)]
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