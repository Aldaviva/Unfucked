using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
#if NETSTANDARD2_0
using System.Text;
#endif

namespace Unfucked;

/// <summary>
/// Methods that make it easier to work with cryptography and security
/// </summary>
public static class Cryptography {

#if !(NETCOREAPP3_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER)
    private static readonly RandomNumberGenerator Rng = new RNGCryptoServiceProvider();
#endif

    /// <summary>
    /// <para>Determine if a given certificate is valid based on the time of its expiration.</para>
    /// <para>⚠ Does not check for a trusted root certificate authority, revocation, or weak signature algorithms, so a client still may not trust this certificate even if it is temporally valid.</para>
    /// </summary>
    /// <param name="cert">The certificate to check</param>
    /// <param name="safetyMargin">An amount of time before the certificate actually expires to declare it temporally invalid, to give a safety margin for renewing certificates early. Defaults to 0 seconds.</param>
    /// <param name="now">The time to use when evaluating the expiration date of the certificate, or <c>default</c> to automatically use the current time</param>
    /// <returns><c>true</c> if <paramref name="cert"/> has not yet expired as of <paramref name="now"/> plus the <paramref name="safetyMargin"/>, or <c>false</c> if it has already expired or is not yet valid</returns>
    [Pure]
    public static bool IsTemporallyValid(this X509Certificate2 cert, TimeSpan safetyMargin = default, DateTime now = default) {
        now = now == default ? DateTime.Now : now;

        try {
            return cert.NotBefore <= now && now + safetyMargin <= cert.NotAfter;
        } catch (CryptographicException) {
            return false;
        }
    }

#if NET7_0_OR_GREATER
    /// <summary>
    /// Get a named sub-value value of a certificate's <c>Subject</c> or <c>Issuer</c>, such as the Common Name (<c>CN</c>).
    /// </summary>
    /// <param name="name">The <see cref="X509Certificate2.SubjectName"/> or <see cref="X509Certificate2.IssuerName"/> of a certificate</param>
    /// <param name="oidFriendlyName">The abbreviation for the name value, such as <c>CN</c> (Common Name), <c>O</c> (Organization), <c>L</c> (Locality/City), <c>S</c> (State/Province), and <c>C</c> (Country)</param>
    /// <returns>The value of the named part of the issuer or subject, without the leading name or <c>=</c>, or <c>null</c> if the issuer/subject did not contain the given named value.</returns>
    [Pure]
    public static string? Get(this X500DistinguishedName name, string oidFriendlyName) =>
        name.EnumerateRelativeDistinguishedNames()
            .FirstOrDefault(distinguishedName => oidFriendlyName.Equals(distinguishedName.GetSingleElementType().FriendlyName, StringComparison.InvariantCultureIgnoreCase))?
            .GetSingleElementValue();
#endif

    /// <summary>
    /// Generate a string of a specified length made up of characters chosen cryptographically randomly from the given alphabet.
    /// </summary>
    /// <remarks>
    /// By Arad (<see href="https://stackoverflow.com/a/73101585/979493"/>) and Eric Johannsen (<see href="https://stackoverflow.com/a/1344255/979493"/>)
    /// </remarks>
    /// <param name="length">Number of characters in the output string.</param>
    /// <param name="alphabet">String containing all of the characters that are possible to include in the output string.</param>
    /// <returns>A string that is <paramref name="length"/> random characters from <paramref name="alphabet"/>, using a cryptographically-secure pseudorandom number generator.</returns>
    [Pure]
    public static string GenerateRandomString(uint length, string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789") {
#if NET8_0_OR_GREATER
        return RandomNumberGenerator.GetString(alphabet, (int) length);
#else
        char[] distinctAlphabet       = alphabet.Distinct().ToArray();
        int    distinctAlphabetLength = distinctAlphabet.Length;

#if NETCOREAPP3_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
        char[] result = new char[length];
        for (int i = 0; i < length; i++) {
            result[i] = distinctAlphabet[RandomNumberGenerator.GetInt32(distinctAlphabetLength)];
        }

        return new string(result);
#else
        StringBuilder result       = new((int) length);
        byte[]        randomBuffer = new byte[length * 4];
        Rng.GetBytes(randomBuffer);

        for (int randomByteIndex = 0; randomByteIndex < length; randomByteIndex++) {
            result.Append(distinctAlphabet[BitConverter.ToUInt32(randomBuffer, randomByteIndex * 4) % distinctAlphabetLength]);
        }

        return result.ToString();
#endif
#endif
    }

}