using System.Collections.Specialized;

namespace Tests.Unfucked;

public class UrisTest {

    [Fact]
    public void GetQuery() {
        NameValueCollection query = new Uri("https://auth@sub.sld.tld:13245/p/a/t/h?q=u&e=r&y#hash").GetQuery();

        query["q"].Should().Be("u");
        query["e"].Should().Be("r");
        query["y"].Should().BeNull();
    }

    [Theory]
    [InlineData("https://aldaviva.com")]
    [InlineData("https://west.aldaviva.com/")]
    [InlineData("ssh://aldaviva.com:22")]
    [InlineData("ssh://auth@aldaviva.com:22")]
    [InlineData("http://aldaviva.com/portfolio")]
    [InlineData("tcp://phonecharger.outlets.aldaviva.com")]
    public void BelongsToDomain(string uri) {
        new Uri(uri).BelongsToDomain("aldaviva.com").Should().BeTrue();
    }

    [Theory]
    [InlineData("https://google.com")]
    [InlineData("https://aldaviva.com.evilsite.com")]
    [InlineData("https://aldaviva.comevilsite.com")]
    [InlineData("https://aldaviva.com-evilsite.com")]
    [InlineData("https://evilsite.com/aldaviva.com")]
    [InlineData("https://evilsite.com?aldaviva.com")]
    [InlineData("https://evilsite.com?k=aldaviva.com")]
    [InlineData("https://evilsite.com?aldaviva.com=v")]
    [InlineData("https://evilsite.com#aldaviva.com")]
    public void DoesNotBelongToDomain(string uri) {
        new Uri(uri).BelongsToDomain("aldaviva.com").Should().BeFalse();
    }

}