using ManagedWinapi.Windows;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Runtime.InteropServices;
using System.Security.Claims;
using System.Security.Principal;
using System.Windows;
using Unfucked.Windows;

namespace Unfucked;

/// <summary>
/// Methods that make it easier to work with Managed Windows API (mwinapi, https://mwinapi.sourceforge.net).
/// </summary>
public static class WindowsExtensions {

    /// <param name="window">An mwinapi <see cref="SystemWindow"/> that wraps a Win32 window.</param>
    extension(SystemWindow window) {

        /// <summary>
        /// Get the base filename, without a file extension, of the executable that is running the process for a window.
        /// </summary>
        /// <returns>Executable file basename for the process of <paramref name="window"/>, or <c>null</c> if the process has already exited.</returns>
        [Pure]
        public string? ProcessExecutableBasename {
            get {
                try {
                    if (SystemWindow.GetWindowThreadProcessId(window.HWnd, out int pid) != 0) {
                        try {
                            using Process process = Process.GetProcessById(pid);
                            return process.ProcessName;
                        } catch (InvalidOperationException) {
                            return null;
                        } catch (ArgumentException) {
                            return null;
                        }
                    }
                } catch (ArgumentException) {}

                return null;
            }
        }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern int GetWindowThreadProcessId(IntPtr hWnd, out int lpdwProcessId);

    }

    /// <summary>
    /// Convert a rectangle from mwinapi to Win32 format.
    /// </summary>
    /// <param name="mwinapiRect">A <see cref="RECT"/> from mwinapi.</param>
    /// <returns>A <see cref="Rect"/> with the same coordinates as <paramref name="mwinapiRect"/>.</returns>
    [Pure]
    public static Rect ToWindowsRect(this RECT mwinapiRect) {
        return new Rect(mwinapiRect.Left, mwinapiRect.Top, mwinapiRect.Width, mwinapiRect.Height);
    }

    /// <summary>
    /// Convert a rectangle from Win32 to mwinapi format.
    /// </summary>
    /// <param name="windowsRect">A <see cref="RECT"/> from Win32.</param>
    /// <returns>A <see cref="RECT"/> with the same coordinates as <paramref name="windowsRect"/>.</returns>
    [Pure]
    public static RECT ToMwinapiRect(this Rect windowsRect) {
        return new RECT((int) windowsRect.X, (int) windowsRect.Y, (int) windowsRect.Right, (int) windowsRect.Bottom);
    }

    private const string GROUP_SID     = "http://schemas.microsoft.com/ws/2008/06/identity/claims/groupsid";
    private const string DENY_ONLY_SID = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/denyonlysid";

    private static readonly SecurityIdentifier localAccountAndAdminMember = new("S-1-5-114");
    private static readonly SecurityIdentifier builtinAdministrators      = new(WellKnownSidType.BuiltinAdministratorsSid, null);

    extension(WindowsIdentity identity) {

        /// <summary>
        /// Determine whether the given identity represents a Windows user in the <c>Administrators</c> group, and if they are currently running elevated.
        /// </summary>
        /// <returns>
        /// <para><see cref="AdministratorElevation.ElevatedAdmin"/> if the user is already elevated because they are a member of any administrators group (local, domain, or system), and has all of their admin permissions enabled because the identity's process is running elevated, UAC is off, or Admin Approval Mode is disabled)</para>
        /// <para><see cref="AdministratorElevation.UnelevatedAdmin"/> if the user is not elevated but could if they wanted to, because they are a member of any administrators group, but the process this user identity came from is not running elevated</para>
        /// <para><see cref="AdministratorElevation.NotAdmin"/> if the user is not elevated and couldn't even if they wanted to, because they are a Limited or Standard user since they are not a member of any administrators group, so they cannot elevate without a different, administrator user's credentials</para>
        /// </returns>
        [Pure]
        public AdministratorElevation AdministratorElevation {
            get {
                SecurityIdentifier? domainAdmins = identity.User?.AccountDomainSid is {} domainSid ? new SecurityIdentifier(WellKnownSidType.AccountDomainAdminsSid, domainSid) : null;

                foreach (Claim claim in identity.Claims) {
                    if (claim.Value == localAccountAndAdminMember.Value || claim.Value == builtinAdministrators.Value || (domainAdmins != null && claim.Value == domainAdmins.Value)) {
                        switch (claim.Type) {
                            case GROUP_SID:
                                return AdministratorElevation.ElevatedAdmin;
                            case DENY_ONLY_SID:
                                return AdministratorElevation.UnelevatedAdmin;
                        }
                    }
                }
                return AdministratorElevation.NotAdmin;
            }
        }

    }

    private static readonly IDictionary<string, uint> CLUSTER_SIZE_CACHE = new Dictionary<string, uint>();

    extension(FileInfo file) {

        /// <summary>
        /// Get the size of a file on disk, which can be smaller than the logical <see cref="FileInfo.Length"/> if the file is compressed by NTFS or is sparse (like a partially downloaded file).
        /// </summary>
        /// <remarks>By margnus1: <see href="https://stackoverflow.com/a/3751135/979493"/></remarks>
        public ulong LengthOnDisk {
            get {
                uint clusterSize = CLUSTER_SIZE_CACHE.GetOrAdd(file.Directory!.Root.FullName, () => {
                    if (0 == GetDiskFreeSpaceW(file.Directory.Root.FullName, out uint sectorsPerCluster, out uint bytesPerSector, out _, out _)) throw new Win32Exception();
                    return sectorsPerCluster * bytesPerSector;
                }, out _);

                uint  losize = GetCompressedFileSizeW(file.FullName, out uint hisize);
                ulong size   = ((ulong) hisize << 32) | losize;
                return (size + clusterSize - 1) / clusterSize * clusterSize;
            }
        }

    }

    [DllImport("kernel32.dll")]
    private static extern uint GetCompressedFileSizeW([MarshalAs(UnmanagedType.LPWStr)] string lpFileName, out uint lpFileSizeHigh);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern int GetDiskFreeSpaceW([MarshalAs(UnmanagedType.LPWStr)] string lpRootPathName, out uint lpSectorsPerCluster, out uint lpBytesPerSector, out uint lpNumberOfFreeClusters,
                                                out uint lpTotalNumberOfClusters);

}