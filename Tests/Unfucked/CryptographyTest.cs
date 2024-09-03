using System.Security.Cryptography.X509Certificates;
using DateTime = System.DateTime;

namespace Tests.Unfucked;

public class CryptographyTest {

    private static readonly X509Certificate2 Cert = X509Certificate2.CreateFromPemFile("Unfucked/cert.pem", "Unfucked/key.pem");

    [Fact]
    public void IsCertificateTemporallyValid() {
        Cert.IsTemporallyValid(now: new DateTime(2024, 10, 2, 0, 0, 0, DateTimeKind.Utc)).Should().BeTrue();
        Cert.IsTemporallyValid(now: new DateTime(2024, 10, 3, 0, 0, 0, DateTimeKind.Utc)).Should().BeFalse();
        Cert.IsTemporallyValid(TimeSpan.FromDays(1), new DateTime(2024, 10, 2, 0, 0, 0, DateTimeKind.Utc)).Should().BeFalse();
    }

    [Fact]
    public void GetCertField() {
        X500DistinguishedName subjectName = Cert.SubjectName;
        subjectName.Get("CN").Should().Be("Ben Hutchison");
        subjectName.Get("O").Should().Be("Ben Hutchison");
        subjectName.Get("L").Should().Be("Santa Clara");
        subjectName.Get("S").Should().Be("California");
        subjectName.Get("C").Should().Be("US");
    }

    [Fact]
    public void GenerateRandomString() {
        const string alphabet = "0123456789abcdef";
        string       actual   = Cryptography.GenerateRandomString(20, alphabet);
        actual.Should().HaveLength(20);
        foreach (char c in actual) {
            alphabet.Contains(c).Should().BeTrue("generated string should only contain characters from alphabet");
        }
    }

}