namespace Tests.Unfucked;

public class CryptographyTest {

    [Fact]
    public void GenerateRandomString2() {
        string actual = Cryptography.GenerateRandomString(8);
        actual.Should().HaveLength(8);
    }

}