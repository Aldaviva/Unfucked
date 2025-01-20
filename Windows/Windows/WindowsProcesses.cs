using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Runtime.InteropServices;
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

        foreach (Process nonDescendant in allProcesses.Except(descendants, ProcessIdEqualityComparer.Instance)) {
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
                using Process? descendantParent = GetParentProcess(descendant);
                isDescendantOfParent = descendantParent?.Id == ancestor.Id;
            } catch (Exception e) when (e is not OutOfMemoryException) {
                //leave isDescendentOfParent false
            }

            return isDescendantOfParent ? new[] { descendant }.Concat(GetDescendantProcesses(descendant, allProcesses)) : [];
        });

    private class ProcessIdEqualityComparer: IEqualityComparer<Process> {

        public static readonly ProcessIdEqualityComparer Instance = new();

        public bool Equals(Process? x, Process? y) => ReferenceEquals(x, y) || (x is not null && y is not null && x.GetType() == y.GetType() && x.Id == y.Id);

        public int GetHashCode(Process obj) => obj.Id;

    }

    [Pure]
    public static bool IsProcessSuspended(this Process process) {
        uint returnCode = NtQueryInformationProcess(process.Handle, ProcessInfoClass.ProcessBasicInformation, out ProcessExtendedBasicInformation info,
            Marshal.SizeOf<ProcessExtendedBasicInformation>(), out int _);
        return returnCode == 0 && (info.Flags & ProcessExtendedBasicInformation.ProcessFlags.IsFrozen) != 0;
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

            IsProtectedProcess   = 1 << 0,
            IsWow64Process       = 1 << 1,
            IsProcessDeleting    = 1 << 2,
            IsCrossSessionCreate = 1 << 3,
            IsFrozen             = 1 << 4,
            IsBackground         = 1 << 5,
            IsStronglyNamed      = 1 << 6,
            IsSecureProcess      = 1 << 7,
            IsSubsystemProcess   = 1 << 8,
            SpareBits            = 1 << 9

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

        ProcessBasicInformation                     = 0x00,
        ProcessQuotaLimits                          = 0x01,
        ProcessIoCounters                           = 0x02,
        ProcessVMCounters                           = 0x03,
        ProcessTimes                                = 0x04,
        ProcessBasePriority                         = 0x05,
        ProcessRaisePriority                        = 0x06,
        ProcessDebugPort                            = 0x07,
        ProcessExceptionPort                        = 0x08,
        ProcessAccessToken                          = 0x09,
        ProcessLdtInformation                       = 0x0A,
        ProcessLdtSize                              = 0x0B,
        ProcessDefaultHardErrorMode                 = 0x0C,
        ProcessIoPortHandlers                       = 0x0D,
        ProcessPooledUsageAndLimits                 = 0x0E,
        ProcessWorkingSetWatch                      = 0x0F,
        ProcessUserModeIopl                         = 0x10,
        ProcessEnableAlignmentFaultFixup            = 0x11,
        ProcessPriorityClass                        = 0x12,
        ProcessWx86Information                      = 0x13,
        ProcessHandleCount                          = 0x14,
        ProcessAffinityMask                         = 0x15,
        ProcessPriorityBoost                        = 0x16,
        ProcessDeviceMap                            = 0x17,
        ProcessSessionInformation                   = 0x18,
        ProcessForegroundInformation                = 0x19,
        ProcessWow64Information                     = 0x1A,
        ProcessImageFileName                        = 0x1B,
        ProcessLuidDeviceMapsEnabled                = 0x1C,
        ProcessBreakOnTermination                   = 0x1D,
        ProcessDebugObjectHandle                    = 0x1E,
        ProcessDebugFlags                           = 0x1F,
        ProcessHandleTracing                        = 0x20,
        ProcessIoPriority                           = 0x21,
        ProcessExecuteFlags                         = 0x22,
        ProcessResourceManagement                   = 0x23,
        ProcessCookie                               = 0x24,
        ProcessImageInformation                     = 0x25,
        ProcessCycleTime                            = 0x26,
        ProcessPagePriority                         = 0x27,
        ProcessInstrumentationCallback              = 0x28,
        ProcessThreadStackAllocation                = 0x29,
        ProcessWorkingSetWatchEx                    = 0x2A,
        ProcessImageFileNameWin32                   = 0x2B,
        ProcessImageFileMapping                     = 0x2C,
        ProcessAffinityUpdateMode                   = 0x2D,
        ProcessMemoryAllocationMode                 = 0x2E,
        ProcessGroupInformation                     = 0x2F,
        ProcessTokenVirtualizationEnabled           = 0x30,
        ProcessConsoleHostProcess                   = 0x31,
        ProcessWindowInformation                    = 0x32,
        ProcessHandleInformation                    = 0x33,
        ProcessMitigationPolicy                     = 0x34,
        ProcessDynamicFunctionTableInformation      = 0x35,
        ProcessHandleCheckingMode                   = 0x36,
        ProcessKeepAliveCount                       = 0x37,
        ProcessRevokeFileHandles                    = 0x38,
        ProcessWorkingSetControl                    = 0x39,
        ProcessHandleTable                          = 0x3A,
        ProcessCheckStackExtentsMode                = 0x3B,
        ProcessCommandLineInformation               = 0x3C,
        ProcessProtectionInformation                = 0x3D,
        ProcessMemoryExhaustion                     = 0x3E,
        ProcessFaultInformation                     = 0x3F,
        ProcessTelemetryIdInformation               = 0x40,
        ProcessCommitReleaseInformation             = 0x41,
        ProcessDefaultCpuSetsInformation            = 0x42,
        ProcessAllowedCpuSetsInformation            = 0x43,
        ProcessSubsystemProcess                     = 0x44,
        ProcessJobMemoryInformation                 = 0x45,
        ProcessInPrivate                            = 0x46,
        ProcessRaiseUmExceptionOnInvalidHandleClose = 0x47,
        ProcessIumChallengeResponse                 = 0x48,
        ProcessChildProcessInformation              = 0x49,
        ProcessHighGraphicsPriorityInformation      = 0x4A,
        ProcessSubsystemInformation                 = 0x4B,
        ProcessEnergyValues                         = 0x4C,
        ProcessActivityThrottleState                = 0x4D,
        ProcessActivityThrottlePolicy               = 0x4E,
        ProcessWin32KSyscallFilterInformation       = 0x4F,
        ProcessDisableSystemAllowedCpuSets          = 0x50,
        ProcessWakeInformation                      = 0x51,
        ProcessEnergyTrackingState                  = 0x52,
        ProcessManageWritesToExecutableMemory       = 0x53,
        ProcessCaptureTrustletLiveDump              = 0x54,
        ProcessTelemetryCoverage                    = 0x55,
        ProcessEnclaveInformation                   = 0x56,
        ProcessEnableReadWriteVMLogging             = 0x57,
        ProcessUptimeInformation                    = 0x58,
        ProcessImageSection                         = 0x59,
        ProcessDebugAuthInformation                 = 0x5A,
        ProcessSystemResourceManagement             = 0x5B,
        ProcessSequenceNumber                       = 0x5C,
        ProcessLoaderDetour                         = 0x5D,
        ProcessSecurityDomainInformation            = 0x5E,
        ProcessCombineSecurityDomainsInformation    = 0x5F,
        ProcessEnableLogging                        = 0x60,
        ProcessLeapSecondInformation                = 0x61,
        ProcessFiberShadowStackAllocation           = 0x62,
        ProcessFreeFiberShadowStackAllocation       = 0x63,
        MaxProcessInfoClass                         = 0x64

    }

}