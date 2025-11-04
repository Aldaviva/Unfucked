#if !NET48
using System.Security.Cryptography.X509Certificates;
#endif

namespace Tests.Unfucked;

public class CryptographyTest {

#if !NET48
    private static readonly X509Certificate2 CERT = X509Certificate2.CreateFromPemFile("Unfucked/cert.pem", "Unfucked/key.pem");

    [Fact]
    public void IsCertificateTemporallyValid() {
        CERT.IsTemporallyValid(now: new DateTime(2024, 10, 2, 0, 0, 0, DateTimeKind.Utc)).Should().BeTrue();
        CERT.IsTemporallyValid(now: new DateTime(2024, 10, 3, 0, 0, 0, DateTimeKind.Utc)).Should().BeFalse();
        CERT.IsTemporallyValid(TimeSpan.FromDays(1), new DateTime(2024, 10, 2, 0, 0, 0, DateTimeKind.Utc)).Should().BeFalse();
    }

    [Fact]
    public void GetCertField() {
        X500DistinguishedName subjectName = CERT.SubjectName;
        subjectName.Get("CN").Should().Be("Ben Hutchison");
        subjectName.Get("O").Should().Be("Ben Hutchison");
        subjectName.Get("L").Should().Be("Santa Clara");
        subjectName.Get("S").Should().Be("California");
        subjectName.Get("C").Should().Be("US");
    }
#endif

    [Fact]
    public void GenerateRandomString() {
        const string alphabet = "0123456789abcdef";
        string       actual   = Cryptography.GenerateRandomString(20, alphabet);
        actual.Should().HaveLength(20);
        foreach (char c in actual) {
            alphabet.Contains(c).Should().BeTrue("generated string should only contain characters from alphabet");
        }
    }

    [Fact]
    public void GenerateRandomString2() {
        string actual = Cryptography.GenerateRandomString(8);
        actual.Should().HaveLength(8);
    }

}