using System.Globalization;
using System.Runtime.InteropServices;

namespace Unfucked;

/// <summary>
/// Extra methods missing from <see cref="CultureInfo"/>.
/// </summary>
public static class CultureInfo2 {

    private const uint MuiLanguageName = 8;

    private static readonly Lazy<CultureInfo> MachineCulture = new(GetMachineCulture, LazyThreadSafetyMode.PublicationOnly);

    /// <summary>
    /// <para>Get the locale of the computer, which affects the welcome screen and system accounts such as <c>NT AUTHORITY\NETWORK SERVICE</c>. This is like <see cref="CultureInfo.CurrentCulture"/>, but for the entire operating system instead of the current user.</para>
    /// <para>This value is controlled by <c>intl.cpl</c> › Administrative › Copy settings… › Welcome screen › Display language.</para>
    /// </summary>
    public static CultureInfo CurrentMachineCulture => MachineCulture.Value;

    private static unsafe CultureInfo GetMachineCulture() {
        int bufferSize = 0;
        GetSystemPreferredUILanguages(MuiLanguageName, out _, null, ref bufferSize);
        char[] buffer = new char[bufferSize];
        fixed (char* bufferStart = &buffer[0]) {
            GetSystemPreferredUILanguages(MuiLanguageName, out _, bufferStart, ref bufferSize);
            return CultureInfo.GetCultureInfo(new string(bufferStart));
        }
    }

    /// <summary>
    /// <para><see href="https://learn.microsoft.com/en-us/windows/win32/intl/user-interface-language-management#system-ui-language"/></para>
    /// <para><see href="https://learn.microsoft.com/en-us/windows/win32/api/winnls/nf-winnls-getsystempreferreduilanguages"/></para>
    /// </summary>
    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern unsafe bool GetSystemPreferredUILanguages(uint flags, out uint languageCount, char* resultBuffer, ref int resultBufferLength);

}