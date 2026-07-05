using System.Globalization;
using System.Runtime.InteropServices;

namespace Unfucked;

/// <summary>
/// Extra methods missing from <see cref="CultureInfo"/>.
/// </summary>
public static class Cultures {

    private const uint MUI_LANGUAGE_NAME = 8;

    private static readonly Lazy<CultureInfo> MACHINE_CULTURE = new(GetMachineCulture, LazyThreadSafetyMode.PublicationOnly);

    extension(CultureInfo) {

        /// <summary>
        /// <para>Get the locale of the computer, which affects the welcome screen and system accounts such as <c>NT AUTHORITY\NETWORK SERVICE</c>. This is like <see cref="CultureInfo.CurrentCulture"/>, but for the entire operating system instead of the current user.</para>
        /// <para>This value is controlled by <c>intl.cpl</c> › Administrative › Copy settings… › Welcome screen › Display language.</para>
        /// </summary>
        public static CultureInfo CurrentMachineCulture => MACHINE_CULTURE.Value;

    }

    private static CultureInfo GetMachineCulture() {
        int bufferSize = 0;
        GetSystemPreferredUILanguages(MUI_LANGUAGE_NAME, out _, [], ref bufferSize);
        char[] buffer = new char[bufferSize];
        GetSystemPreferredUILanguages(MUI_LANGUAGE_NAME, out _, buffer, ref bufferSize);
        return CultureInfo.GetCultureInfo(new string(buffer));
    }

    /// <summary>
    /// <para><see href="https://learn.microsoft.com/en-us/windows/win32/intl/user-interface-language-management#system-ui-language"/></para>
    /// <para><see href="https://learn.microsoft.com/en-us/windows/win32/api/winnls/nf-winnls-getsystempreferreduilanguages"/></para>
    /// </summary>
    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetSystemPreferredUILanguages(uint flags, out uint languageCount, char[] resultBuffer, ref int resultBufferLength);

}