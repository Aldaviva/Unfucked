using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
#if NETSTANDARD2_0
using System.Text;
#endif

namespace Unfucked;

public static class Cryptography {

#if NETSTANDARD2_0
    private static readonly RandomNumberGenerator Rng = new RNGCryptoServiceProvider();
#endif

    [Pure]
    public static bool IsTemporallyValid(this X509Certificate2 cert, TimeSpan safetyMargin = default, DateTime now = default) {
        now = now == default ? DateTime.Now : now;

        try {
            return cert.NotBefore <= now && now + safetyMargin <= cert.NotAfter;
        } catch (CryptographicException) {
            return false;
        }
    }

#if NET8_0_OR_GREATER
    [Pure]
    public static string? Get(this X500DistinguishedName name, string oidFriendlyName) =>
        name.EnumerateRelativeDistinguishedNames()
            .FirstOrDefault(distinguishedName => oidFriendlyName.Equals(distinguishedName.GetSingleElementType().FriendlyName, StringComparison.InvariantCultureIgnoreCase))?
            .GetSingleElementValue();
#endif

    // https://stackoverflow.com/a/73101585/979493
    [Pure]
    public static string GenerateRandomString(uint length, string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789") {
        char[] distinctAlphabet       = alphabet.Distinct().ToArray();
        int    distinctAlphabetLength = distinctAlphabet.Length;

#if NET6_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
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
            result.Append(alphabet[BitConverter.ToInt32(randomBuffer, randomByteIndex * 4) % distinctAlphabetLength]);
        }

        return result.ToString();
#endif
    }

}