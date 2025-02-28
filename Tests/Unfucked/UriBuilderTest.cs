namespace Tests.Unfucked;

public class UriBuilderTest {

    [Theory]
    [MemberData(nameof(CostructAndSerializeData))]
    public void ConstructAndSerialize(URIBuilder input, string expected) {
        input.ToString().Should().Be(expected);
        ((string) input).Should().Be(expected);
    }

    public static TheoryData<URIBuilder, string> CostructAndSerializeData => new() {
        { new URIBuilder("https", "aldaviva.com"), "https://aldaviva.com/" },
        { new URIBuilder("https", "aldaviva.com").Path("/"), "https://aldaviva.com/" },
        { new URIBuilder("https", "aldaviva.com").Path("index.html"), "https://aldaviva.com/index.html" },
        { new URIBuilder("https", "aldaviva.com").Path("docs").Path("gdq.ics"), "https://aldaviva.com/docs/gdq.ics" },
        { new URIBuilder("https", "aldaviva.com").Path("a", "b", "c"), "https://aldaviva.com/a/b/c" },
        { new URIBuilder("https", "aldaviva.com").Path("a", "b", ".", "c"), "https://aldaviva.com/a/b/c" },
        { new URIBuilder("https", "aldaviva.com").Path("a/b/c"), "https://aldaviva.com/a/b/c" },
        { new URIBuilder("https", "aldaviva.com").Path("/a/b/c"), "https://aldaviva.com/a/b/c" },
        { new URIBuilder("https", "aldaviva.com").Path("a", "/b", "c"), "https://aldaviva.com/b/c" },
        { new URIBuilder("https", "aldaviva.com").Path("a", "..", "b", "c"), "https://aldaviva.com/b/c" },
        { new URIBuilder("https", "aldaviva.com").Path("a/b", false), "https://aldaviva.com/a%2Fb" },
        { new URIBuilder("http", "aldaviva.com", 8080), "http://aldaviva.com:8080/" },
        { new URIBuilder("mailto", "ben@aldaviva.com", (string?) null), "mailto:ben@aldaviva.com" },
        { new URIBuilder("ftps", "ftp.aldaviva.com", 990), "ftps://ftp.aldaviva.com:990/" }
    };

    [Theory]
    [MemberData(nameof(BasicEscapingData))]
    public void BasicEscaping(URIBuilder input, string expected) {
        input.ToString().Should().Be(expected);
    }

    public static TheoryData<URIBuilder, string> BasicEscapingData => new() {
        { new URIBuilder("https", "aldaviva.com").UserInfo("ben:my password"), "https://ben:my%20password@aldaviva.com/" },
        { new URIBuilder("https", "aldaviva.com").Path("my path"), "https://aldaviva.com/my%20path" },
        { new URIBuilder("https", "aldaviva.com").QueryParam("my key", "my value"), "https://aldaviva.com/?my%20key=my%20value" },
        { new URIBuilder("https", "aldaviva.com").Fragment("my fragment"), "https://aldaviva.com/#my%20fragment" },
    };

    [Theory]
    [MemberData(nameof(TemplateData))]
    public void JaxRsTemplates(URIBuilder input, string expected) {
        input.ResolveTemplate("placeholder", "hello").ToString().Should().Be(expected);
    }

    public static TheoryData<URIBuilder, string> TemplateData => new() {
        { new URIBuilder("{placeholder}", "aldaviva.com", 12345), "hello://aldaviva.com:12345/" },
        { new URIBuilder("https", "{placeholder}"), "https://hello/" },
        { new URIBuilder("hargle", "{placeholder}", (string?) null), "hargle:hello" },
        { new URIBuilder("https", "{placeholder}.aldaviva.com"), "https://hello.aldaviva.com/" },
        { new URIBuilder("https", "aldaviva.com").Path("{placeholder}.txt"), "https://aldaviva.com/hello.txt" },
        { new URIBuilder("https", "aldaviva.com").Path("docs/{placeholder}.txt"), "https://aldaviva.com/docs/hello.txt" },
        { new URIBuilder("https", "aldaviva.com").QueryParam("q", "{placeholder}"), "https://aldaviva.com/?q=hello" },
        { new URIBuilder("https", "aldaviva.com").Fragment("section-{placeholder}"), "https://aldaviva.com/#section-hello" },
    };

    [Theory]
    [MemberData(nameof(UriTemplatesData))]
    public void UriTemplates(string template, string expected) {
        URIBuilder.FromTemplate(template).ResolveTemplate("placeholder", "hello").ToString().Should().Be(expected);
    }

    public static TheoryData<string, string> UriTemplatesData => new() {
        { "{placeholder}://aldaviva.com", "hello://aldaviva.com/" },
        { "https://{placeholder}.aldaviva.com", "https://hello.aldaviva.com/" },
        { "https://aldaviva.com/a/{placeholder}", "https://aldaviva.com/a/hello" },
        { "https://aldaviva.com/a{/placeholder}", "https://aldaviva.com/a/hello" },
        { "https://aldaviva.com/a{?placeholder}", "https://aldaviva.com/a?placeholder=hello" },
        { "https://aldaviva.com/a?b=c{&placeholder}", "https://aldaviva.com/a?b=c&placeholder=hello" },
    };

    [Fact]
    public void BetterThanBclUriBuilder() {
        new URIBuilder("https", "aldaviva.com", 444).Path("a/b", "c").Path("d").QueryParam("e", "f").Fragment("g").ToString()
            .Should().Be("https://aldaviva.com:444/a/b/c/d?e=f#g", "lets you add individual query parameters and path segments");
    }

    [Fact]
    public void BetterThanFluentUriBuilder() {
        new URIBuilder("https://aldaviva.com").Path("a").Path("b").ToString()
            .Should().Be("https://aldaviva.com/a/b", "it allows multiple path segments to be appended");
    }

    [Fact]
    public void BetterThanJaytwoFluentUri() {
        new URIBuilder("https://aldaviva.com").Path("hello world").ToString()
            .Should().Be("https://aldaviva.com/hello%20world", "it doesn't double-escape path segments");
    }

    [Fact]
    public void BetterThanUriBuilderFluent() {
        new URIBuilder("https://aldaviva.com").Path("hello world").ToString()
            .Should().Be("https://aldaviva.com/hello%20world", "it escapes path segments");
        new URIBuilder("https://aldaviva.com?q=removeme").QueryParam("q", (object?) null).ToString()
            .Should().Be("https://aldaviva.com/", "it can remove existing query parameters");
        new URIBuilder("https://aldaviva.com?q=removeme").QueryParam("q", (object?) null).QueryParam("q", "hello").ToString()
            .Should().Be("https://aldaviva.com/?q=hello", "it can replace existing query parameters");
    }

    [Fact]
    public void BetterThanTavisUriTemplates() {
        new URIBuilder("https://aldaviva.com").QueryParam("q", "a").QueryParam("q", "b").ToString()
            .Should().Be("https://aldaviva.com/?q=a&q=b", "supports multiple values for a query parameter");
        new URIBuilder("https://aldaviva.com").QueryParam("q[]", "a").QueryParam("q[]", "b").ToString()
            .Should().Be("https://aldaviva.com/?q[]=a&q[]=b", "supports multiple values for an array-style query parameter");

        URIBuilder baseUrl = new("https://aldaviva.com/api");
        baseUrl.Path("service1").ToString().Should().Be("https://aldaviva.com/api/service1");
        baseUrl.Path("service2").ToString().Should().Be("https://aldaviva.com/api/service2", "it is immutable and therefore thread-safe");
    }

}