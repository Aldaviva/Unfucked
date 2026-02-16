using Microsoft.Win32.SafeHandles;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;

namespace Unfucked.Windows;

/// <summary>
/// Methods that make it easier to work with processes and arguments.
/// </summary>
public static class WindowsProcesses {

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
    /// <para>Gets the process that started a given child process.</para>
    /// <para>Windows only.</para>
    /// </summary>
    /// <param name="pid">The child process ID of which you want to find the parent, or <c>null</c> to get the parent of the current process.</param>
    /// <returns>The parent process of the child process that has the given <paramref name="pid"/>, or <c>null</c> if the process cannot be found (possibly because it already exited). Remember to <see cref="IDisposable.Dispose"/> the returned value.</returns>
    /// <inheritdoc cref="GetParentProcess(Process)" path="/remarks" />
    [ExcludeFromCodeCoverage]
    [Pure]
    public static Process? GetParentProcess(int? pid = null) {
        using Process process = pid.HasValue ? Process.GetProcessById(pid.Value) : Process.GetCurrentProcess();
        return process.GetParentProcess();
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
    [ExcludeFromCodeCoverage]
    [Pure]
    public static Process? GetParentProcess(this Process child) {
        try {
            if (0 != NtQueryInformationProcess(child.Handle, 0, out ProcessBasicInformation basicInfo, Marshal.SizeOf<ProcessBasicInformation>(), out int _)) {
                return null;
            }

            return Process.GetProcessById((int) basicInfo.InheritedFromUniqueProcessId.ToUInt32());
        } catch (ArgumentException) {
            // not found
            return null;
        } catch (InvalidOperationException) {
            // child process already exited
            return null;
        }
    }

    /// <summary>
    /// <para>List all currently running processes that were started by the given <paramref name="ancestor"/> process, including transitively to an unlimited depth.</para>
    /// <para>Windows only.</para>
    /// </summary>
    /// <param name="ancestor">The parent, grandparent, or further process that started the processes to return</param>
    /// <returns>List of processes which were started by either <paramref name="ancestor"/>, one of its children, grandchildren, or further to an unlimited depth. Remember to <see cref="IDisposable.Dispose"/> all of these <see cref="Process"/> instances.</returns>
    [Pure]
    public static IEnumerable<Process> GetDescendantProcesses(this Process ancestor) {
        Process[] allProcesses = Process.GetProcesses();

        //eagerly find child processes, because once we start killing processes, their parent PIDs won't mean anything anymore
        List<Process> descendants = GetDescendantProcesses(ancestor, allProcesses).ToList();

        foreach (Process nonDescendant in allProcesses.Except(descendants, ProcessIdEqualityComparer.INSTANCE)) {
            nonDescendant.Dispose();
        }

        return descendants;
    }

    [Pure]
    // ReSharper disable once ParameterTypeCanBeEnumerable.Local (Avoid double enumeration heuristic)
    private static IEnumerable<Process> GetDescendantProcesses(Process ancestor, Process[] allProcesses) =>
        allProcesses.SelectMany(descendant => {
            bool isDescendantOfParent = false;
            try {
                using Process? descendantParent = descendant.GetParentProcess();
                isDescendantOfParent = descendantParent?.Id == ancestor.Id;
            } catch (Exception e) when (e is not OutOfMemoryException) {
                //leave isDescendentOfParent false
            }

            return isDescendantOfParent ? new[] { descendant }.Concat(GetDescendantProcesses(descendant, allProcesses)) : [];
        });

    private class ProcessIdEqualityComparer: IEqualityComparer<Process> {

        public static readonly ProcessIdEqualityComparer INSTANCE = new();

        public bool Equals(Process? x, Process? y) => ReferenceEquals(x, y) || (x is not null && y is not null && x.GetType() == y.GetType() && x.Id == y.Id);

        public int GetHashCode(Process obj) => obj.Id;

    }

    /// <summary>
    /// Determine whether a process is suspended or not.
    /// </summary>
    /// <param name="process">The process to check, such as <see cref="Process.GetCurrentProcess"/>.</param>
    /// <returns><c>true</c> if <paramref name="process"/> is suspended, or <c>false</c> if it is running normally</returns>
    [Pure]
    public static bool IsProcessSuspended(this Process process) {
        uint returnCode = NtQueryInformationProcess(process.Handle, ProcessInfoClass.PROCESS_BASIC_INFORMATION, out ProcessExtendedBasicInformation info,
            Marshal.SizeOf<ProcessExtendedBasicInformation>(), out int _);
        return returnCode == 0 && (info.Flags & ProcessExtendedBasicInformation.ProcessFlags.IS_FROZEN) != 0;
    }

    [DllImport("ntdll.dll", SetLastError = true)]
    private static extern uint NtQueryInformationProcess(IntPtr process, ProcessInfoClass query, out ProcessExtendedBasicInformation result, int inputSize, out int resultSize);

    [DllImport("ntdll.dll", SetLastError = true)]
    private static extern uint NtQueryInformationProcess(IntPtr process, ProcessInfoClass query, out ProcessBasicInformation result, int inputSize, out int resultSize);

    [StructLayout(LayoutKind.Sequential)]
    private struct ProcessExtendedBasicInformation {

        public UIntPtr                 Size;
        public ProcessBasicInformation BasicInfo;
        public ProcessFlags            Flags;

        [Flags]
        public enum ProcessFlags: uint {

            IS_PROTECTED_PROCESS    = 1 << 0,
            IS_WOW64_PROCESS        = 1 << 1,
            IS_PROCESS_DELETING     = 1 << 2,
            IS_CROSS_SESSION_CREATE = 1 << 3,
            IS_FROZEN               = 1 << 4,
            IS_BACKGROUND           = 1 << 5,
            IS_STRONGLY_NAMED       = 1 << 6,
            IS_SECURE_PROCESS       = 1 << 7,
            IS_SUBSYSTEM_PROCESS    = 1 << 8,
            SPARE_BITS              = 1 << 9

        }

    }

    [StructLayout(LayoutKind.Sequential)]
    private struct ProcessBasicInformation {

        public uint    ExitStatus;
        public IntPtr  PebBaseAddress;
        public UIntPtr AffinityMask;
        public int     BasePriority;
        public UIntPtr UniqueProcessId;
        public UIntPtr InheritedFromUniqueProcessId;

    }

    private enum ProcessInfoClass: uint {

        PROCESS_BASIC_INFORMATION                          = 0x00,
        PROCESS_QUOTA_LIMITS                               = 0x01,
        PROCESS_IO_COUNTERS                                = 0x02,
        PROCESS_VM_COUNTERS                                = 0x03,
        PROCESS_TIMES                                      = 0x04,
        PROCESS_BASE_PRIORITY                              = 0x05,
        PROCESS_RAISE_PRIORITY                             = 0x06,
        PROCESS_DEBUG_PORT                                 = 0x07,
        PROCESS_EXCEPTION_PORT                             = 0x08,
        PROCESS_ACCESS_TOKEN                               = 0x09,
        PROCESS_LDT_INFORMATION                            = 0x0A,
        PROCESS_LDT_SIZE                                   = 0x0B,
        PROCESS_DEFAULT_HARD_ERROR_MODE                    = 0x0C,
        PROCESS_IO_PORT_HANDLERS                           = 0x0D,
        PROCESS_POOLED_USAGE_AND_LIMITS                    = 0x0E,
        PROCESS_WORKING_SET_WATCH                          = 0x0F,
        PROCESS_USER_MODE_IOPL                             = 0x10,
        PROCESS_ENABLE_ALIGNMENT_FAULT_FIXUP               = 0x11,
        PROCESS_PRIORITY_CLASS                             = 0x12,
        PROCESS_WX86_INFORMATION                           = 0x13,
        PROCESS_HANDLE_COUNT                               = 0x14,
        PROCESS_AFFINITY_MASK                              = 0x15,
        PROCESS_PRIORITY_BOOST                             = 0x16,
        PROCESS_DEVICE_MAP                                 = 0x17,
        PROCESS_SESSION_INFORMATION                        = 0x18,
        PROCESS_FOREGROUND_INFORMATION                     = 0x19,
        PROCESS_WOW64_INFORMATION                          = 0x1A,
        PROCESS_IMAGE_FILE_NAME                            = 0x1B,
        PROCESS_LUID_DEVICE_MAPS_ENABLED                   = 0x1C,
        PROCESS_BREAK_ON_TERMINATION                       = 0x1D,
        PROCESS_DEBUG_OBJECT_HANDLE                        = 0x1E,
        PROCESS_DEBUG_FLAGS                                = 0x1F,
        PROCESS_HANDLE_TRACING                             = 0x20,
        PROCESS_IO_PRIORITY                                = 0x21,
        PROCESS_EXECUTE_FLAGS                              = 0x22,
        PROCESS_RESOURCE_MANAGEMENT                        = 0x23,
        PROCESS_COOKIE                                     = 0x24,
        PROCESS_IMAGE_INFORMATION                          = 0x25,
        PROCESS_CYCLE_TIME                                 = 0x26,
        PROCESS_PAGE_PRIORITY                              = 0x27,
        PROCESS_INSTRUMENTATION_CALLBACK                   = 0x28,
        PROCESS_THREAD_STACK_ALLOCATION                    = 0x29,
        PROCESS_WORKING_SET_WATCH_EX                       = 0x2A,
        PROCESS_IMAGE_FILE_NAME_WIN32                      = 0x2B,
        PROCESS_IMAGE_FILE_MAPPING                         = 0x2C,
        PROCESS_AFFINITY_UPDATE_MODE                       = 0x2D,
        PROCESS_MEMORY_ALLOCATION_MODE                     = 0x2E,
        PROCESS_GROUP_INFORMATION                          = 0x2F,
        PROCESS_TOKEN_VIRTUALIZATION_ENABLED               = 0x30,
        PROCESS_CONSOLE_HOST_PROCESS                       = 0x31,
        PROCESS_WINDOW_INFORMATION                         = 0x32,
        PROCESS_HANDLE_INFORMATION                         = 0x33,
        PROCESS_MITIGATION_POLICY                          = 0x34,
        PROCESS_DYNAMIC_FUNCTION_TABLE_INFORMATION         = 0x35,
        PROCESS_HANDLE_CHECKING_MODE                       = 0x36,
        PROCESS_KEEP_ALIVE_COUNT                           = 0x37,
        PROCESS_REVOKE_FILE_HANDLES                        = 0x38,
        PROCESS_WORKING_SET_CONTROL                        = 0x39,
        PROCESS_HANDLE_TABLE                               = 0x3A,
        PROCESS_CHECK_STACK_EXTENTS_MODE                   = 0x3B,
        PROCESS_COMMAND_LINE_INFORMATION                   = 0x3C,
        PROCESS_PROTECTION_INFORMATION                     = 0x3D,
        PROCESS_MEMORY_EXHAUSTION                          = 0x3E,
        PROCESS_FAULT_INFORMATION                          = 0x3F,
        PROCESS_TELEMETRY_ID_INFORMATION                   = 0x40,
        PROCESS_COMMIT_RELEASE_INFORMATION                 = 0x41,
        PROCESS_DEFAULT_CPU_SETS_INFORMATION               = 0x42,
        PROCESS_ALLOWED_CPU_SETS_INFORMATION               = 0x43,
        PROCESS_SUBSYSTEM_PROCESS                          = 0x44,
        PROCESS_JOB_MEMORY_INFORMATION                     = 0x45,
        PROCESS_IN_PRIVATE                                 = 0x46,
        PROCESS_RAISE_UM_EXCEPTION_ON_INVALID_HANDLE_CLOSE = 0x47,
        PROCESS_IUM_CHALLENGE_RESPONSE                     = 0x48,
        PROCESS_CHILD_PROCESS_INFORMATION                  = 0x49,
        PROCESS_HIGH_GRAPHICS_PRIORITY_INFORMATION         = 0x4A,
        PROCESS_SUBSYSTEM_INFORMATION                      = 0x4B,
        PROCESS_ENERGY_VALUES                              = 0x4C,
        PROCESS_ACTIVITY_THROTTLE_STATE                    = 0x4D,
        PROCESS_ACTIVITY_THROTTLE_POLICY                   = 0x4E,
        PROCESS_WIN32_K_SYSCALL_FILTER_INFORMATION         = 0x4F,
        PROCESS_DISABLE_SYSTEM_ALLOWED_CPU_SETS            = 0x50,
        PROCESS_WAKE_INFORMATION                           = 0x51,
        PROCESS_ENERGY_TRACKING_STATE                      = 0x52,
        PROCESS_MANAGE_WRITES_TO_EXECUTABLE_MEMORY         = 0x53,
        PROCESS_CAPTURE_TRUSTLET_LIVE_DUMP                 = 0x54,
        PROCESS_TELEMETRY_COVERAGE                         = 0x55,
        PROCESS_ENCLAVE_INFORMATION                        = 0x56,
        PROCESS_ENABLE_READ_WRITE_VM_LOGGING               = 0x57,
        PROCESS_UPTIME_INFORMATION                         = 0x58,
        PROCESS_IMAGE_SECTION                              = 0x59,
        PROCESS_DEBUG_AUTH_INFORMATION                     = 0x5A,
        PROCESS_SYSTEM_RESOURCE_MANAGEMENT                 = 0x5B,
        PROCESS_SEQUENCE_NUMBER                            = 0x5C,
        PROCESS_LOADER_DETOUR                              = 0x5D,
        PROCESS_SECURITY_DOMAIN_INFORMATION                = 0x5E,
        PROCESS_COMBINE_SECURITY_DOMAINS_INFORMATION       = 0x5F,
        PROCESS_ENABLE_LOGGING                             = 0x60,
        PROCESS_LEAP_SECOND_INFORMATION                    = 0x61,
        PROCESS_FIBER_SHADOW_STACK_ALLOCATION              = 0x62,
        PROCESS_FREE_FIBER_SHADOW_STACK_ALLOCATION         = 0x63,
        MAX_PROCESS_INFO_CLASS                             = 0x64

    }

    /// <summary>
    /// <para>Call this on a child process if you want it to detach from the console and ignore Ctrl+C, because your parent console process will handle that signal.</para>
    /// <para>Strangely, in Windows console applications, pressing Ctrl+C in the terminal will send the signal to every attached descendant in the terminal's process tree, not just the top-most child running directly in the terminal.</para>
    /// <para>This is necessary if you have custom Ctrl+C handling in your parent (using <see cref="Console.CancelKeyPress"/>), and don't want the child to ignore that and exit on the first Ctrl+C anyway.</para>
    /// <para>The best way to solve this is by the child not attaching to the console in the first place, or detaching with <c>FreeConsole()</c>, but this is not possible if you can't make code changes to the child program. The second-best way to solve this is by specifying a <c>dwCreationFlags</c> of <c>DETACHED_PROCESS (0x8)</c> when calling <c>CreateProcess</c>, but these flags are insufficiently customizable when wrapped by .NET's <see cref="ProcessStartInfo"/> (API cliff).</para>
    /// <para>This technique injects a thread into the child process that calls <c>FreeConsole</c>, which is better than copying and reimplementing all of <see cref="Process.Start()"/> from the .NET BCL repository.</para>
    /// </summary>
    /// <param name="process"></param>
    public static void DetachFromConsole(this Process process) {
        int targetPid = process.Id;
        int selfPid;
#if NET5_0_OR_GREATER
        selfPid = Environment.ProcessId;
#else
        using Process selfProcess = Process.GetCurrentProcess();
        selfPid = selfProcess.Id;
#endif

        if (targetPid == selfPid) {
            FreeConsole();
        } else {
            // https://codingvision.net/c-inject-a-dll-into-a-process-w-createremotethread
            using SafeProcessHandle safeProcessHandle = OpenProcess(ProcessSecurityAndAccessRight.PROCESS_CREATE_THREAD, false, targetPid);

            IntPtr methodAddr = GetProcAddress(GetModuleHandle("kernel32.dll"), "FreeConsole");
            CreateRemoteThread(safeProcessHandle, IntPtr.Zero, 0, methodAddr, IntPtr.Zero, 0, IntPtr.Zero);
        }
    }

    [Flags]
    private enum ProcessSecurityAndAccessRight: uint {

        PROCESS_CREATE_THREAD     = 0x2,
        PROCESS_QUERY_INFORMATION = 0x400,
        PROCESS_VM_OPERATION      = 0x8,
        PROCESS_VM_WRITE          = 0x20,
        PROCESS_VM_READ           = 0x10

    }

    [DllImport("kernel32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool FreeConsole();

    [DllImport("kernel32.dll")]
    private static extern SafeProcessHandle OpenProcess(ProcessSecurityAndAccessRight dwDesiredAccess, bool bInheritHandle, int dwProcessId);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
    private static extern IntPtr GetModuleHandle(string lpModuleName);

    [DllImport("kernel32.dll", CharSet = CharSet.Ansi, ExactSpelling = true, SetLastError = true)]
    private static extern IntPtr GetProcAddress(IntPtr hModule, string procName);

    [DllImport("kernel32.dll")]
    private static extern IntPtr CreateRemoteThread(SafeProcessHandle hProcess, IntPtr lpThreadAttributes, uint dwStackSize, IntPtr lpStartAddress, IntPtr lpParameter, uint dwCreationFlags,
                                                    IntPtr lpThreadId);

    /// <summary>
    /// Determine whether a process is running elevated (as Administrator) or not.
    /// </summary>
    /// <param name="process">The process to check, such as <see cref="Process.GetCurrentProcess"/>.</param>
    /// <returns><c>true</c> if <paramref name="process"/> is running elevated, or <c>false</c> if it is unelevated</returns>
    /// <exception cref="Win32Exception">failed to open handle to <paramref name="process"/>, possibly due to privileges.</exception>
    /// <remarks>
    /// By John Smith: <see href="https://stackoverflow.com/a/55079599/979493"/>
    /// </remarks>
    [Pure]
    public static bool IsProcessElevated(this Process process) {
        const uint maximumAllowed = 0x2000000;

        if (!OpenProcessToken(process.Handle, maximumAllowed, out nint token)) {
            throw new Win32Exception(Marshal.GetLastWin32Error(), "OpenProcessToken failed");
        }

        try {
            using WindowsIdentity identity  = new(token);
            WindowsPrincipal      principal = new(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator)
                || principal.IsInRole(0x200); //Domain Administrator
        } finally {
            CloseHandle(token);
        }
    }

    [DllImport("advapi32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool OpenProcessToken(nint processHandle, uint desiredAccess, out nint tokenHandle);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CloseHandle(nint hObject);

}