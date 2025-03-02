using System.Collections.Specialized;

namespace Tests.Unfucked;

public class URITest {

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

    [Theory]
    [InlineData("https://aldaviva.com", "https://aldaviva.com")]
    [InlineData("https://aldaviva.com:443", "https://aldaviva.com")]
    [InlineData("https://foo:bar@aldaviva.com:444/path?query#frag", "https://aldaviva.com:444")]
    public void TheoryMethodName(string uri, string expected) {
        new Uri(uri, UriKind.Absolute).Origin().Should().Be(expected);
    }

}