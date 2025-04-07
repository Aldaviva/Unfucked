namespace Tests.Unfucked;

public class UrlBuilderTest {

    [Theory]
    [MemberData(nameof(CostructAndSerializeData))]
    public void ConstructAndSerialize(UrlBuilder input, string expected) {
        input.ToString().Should().Be(expected);
        ((string) input).Should().Be(expected);
    }

    public static TheoryData<UrlBuilder, string> CostructAndSerializeData => new() {
        { new UrlBuilder("https", "aldaviva.com"), "https://aldaviva.com/" },
        { new UrlBuilder("https", "aldaviva.com").Path("/"), "https://aldaviva.com/" },
        { new UrlBuilder("https", "aldaviva.com").Path("index.html"), "https://aldaviva.com/index.html" },
        { new UrlBuilder("https", "aldaviva.com").Path("docs").Path("gdq.ics"), "https://aldaviva.com/docs/gdq.ics" },
        { new UrlBuilder("https", "aldaviva.com").Path("a", "b", "c"), "https://aldaviva.com/a/b/c" },
        { new UrlBuilder("https", "aldaviva.com").Path("a", "b", ".", "c"), "https://aldaviva.com/a/b/c" },
        { new UrlBuilder("https", "aldaviva.com").Path("a/b/c"), "https://aldaviva.com/a/b/c" },
        { new UrlBuilder("https", "aldaviva.com").Path("/a/b/c"), "https://aldaviva.com/a/b/c" },
        { new UrlBuilder("https", "aldaviva.com").Path("a", "/b", "c"), "https://aldaviva.com/b/c" },
        { new UrlBuilder("https", "aldaviva.com").Path("a", "..", "b", "c"), "https://aldaviva.com/b/c" },
        { new UrlBuilder("https", "aldaviva.com").Path("a/b", false), "https://aldaviva.com/a%2Fb" },
        { new UrlBuilder("http", "aldaviva.com", 8080), "http://aldaviva.com:8080/" },
        { new UrlBuilder("ftps", "ftp.aldaviva.com", 990), "ftps://ftp.aldaviva.com:990/" }
    };

    [Theory]
    [MemberData(nameof(BasicEscapingData))]
    public void BasicEscaping(UrlBuilder input, string expected) {
        input.ToString().Should().Be(expected);
    }

    public static TheoryData<UrlBuilder, string> BasicEscapingData => new() {
        { new UrlBuilder("https", "aldaviva.com").UserInfo("ben:my password"), "https://ben:my%20password@aldaviva.com/" },
        { new UrlBuilder("https", "aldaviva.com").Path("my path"), "https://aldaviva.com/my%20path" },
        { new UrlBuilder("https", "aldaviva.com").QueryParam("my key", "my value"), "https://aldaviva.com/?my%20key=my%20value" },
        { new UrlBuilder("https", "aldaviva.com").Fragment("my fragment"), "https://aldaviva.com/#my%20fragment" },
    };

    [Theory]
    [MemberData(nameof(TemplateData))]
    public void JaxRsTemplates(UrlBuilder input, string expected) {
        input.ResolveTemplate("placeholder", "hello").ToString().Should().Be(expected);
    }

    public static TheoryData<UrlBuilder, string> TemplateData => new() {
        { new UrlBuilder("{placeholder}", "aldaviva.com", 12345), "hello://aldaviva.com:12345/" },
        { new UrlBuilder("https", "{placeholder}"), "https://hello/" },
        { new UrlBuilder("https", "{placeholder}.aldaviva.com"), "https://hello.aldaviva.com/" },
        { new UrlBuilder("https", "aldaviva.com").Path("{placeholder}.txt"), "https://aldaviva.com/hello.txt" },
        { new UrlBuilder("https", "aldaviva.com").Path("docs/{placeholder}.txt"), "https://aldaviva.com/docs/hello.txt" },
        { new UrlBuilder("https", "aldaviva.com").QueryParam("q", "{placeholder}"), "https://aldaviva.com/?q=hello" },
        { new UrlBuilder("https", "aldaviva.com").Fragment("section-{placeholder}"), "https://aldaviva.com/#section-hello" },
    };

    [Theory]
    [MemberData(nameof(UriTemplatesData))]
    public void UriTemplates(string template, string expected) {
        UrlBuilder.FromTemplate(template).ResolveTemplate("placeholder", "hello").ToString().Should().Be(expected);
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
        new UrlBuilder("https", "aldaviva.com", 444).Path("a/b", "c").Path("d").QueryParam("e", "f").Fragment("g").ToString()
            .Should().Be("https://aldaviva.com:444/a/b/c/d?e=f#g", "lets you add individual query parameters and path segments");
    }

    [Fact]
    public void BetterThanFluentUriBuilder() {
        new UrlBuilder("https://aldaviva.com").Path("a").Path("b").ToString()
            .Should().Be("https://aldaviva.com/a/b", "it allows multiple path segments to be appended");
    }

    [Fact]
    public void BetterThanJaytwoFluentUri() {
        new UrlBuilder("https://aldaviva.com").Path("hello world").ToString()
            .Should().Be("https://aldaviva.com/hello%20world", "it doesn't double-escape path segments");
    }

    [Fact]
    public void BetterThanUriBuilderFluent() {
        new UrlBuilder("https://aldaviva.com").Path("hello world").ToString()
            .Should().Be("https://aldaviva.com/hello%20world", "it escapes path segments");
        new UrlBuilder("https://aldaviva.com?q=removeme").QueryParam("q", (object?) null).ToString()
            .Should().Be("https://aldaviva.com/", "it can remove existing query parameters");
        new UrlBuilder("https://aldaviva.com?q=removeme").QueryParam("q", (object?) null).QueryParam("q", "hello").ToString()
            .Should().Be("https://aldaviva.com/?q=hello", "it can replace existing query parameters");
    }

    [Fact]
    public void BetterThanTavisUriTemplates() {
        new UrlBuilder("https://aldaviva.com").QueryParam("q", "a").QueryParam("q", "b").ToString()
            .Should().Be("https://aldaviva.com/?q=a&q=b", "supports multiple values for a query parameter");
        new UrlBuilder("https://aldaviva.com").QueryParam("q[]", "a").QueryParam("q[]", "b").ToString()
            .Should().Be("https://aldaviva.com/?q[]=a&q[]=b", "supports multiple values for an array-style query parameter");

        UrlBuilder baseUrl = new("https://aldaviva.com/api");
        baseUrl.Path("service1").ToString().Should().Be("https://aldaviva.com/api/service1");
        baseUrl.Path("service2").ToString().Should().Be("https://aldaviva.com/api/service2", "it is immutable and therefore thread-safe");
    }

    [Theory]
    [MemberData(nameof(PreserveTrailingPathSlashesData))]
    public void PreserveTrailingPathSlashes(UrlBuilder input, string expected) {
        input.ToString().Should().Be(expected);
        ((string) input).Should().Be(expected);
    }

    public static TheoryData<UrlBuilder, string> PreserveTrailingPathSlashesData => new() {
        { new UrlBuilder("https", "aldaviva.com"), "https://aldaviva.com/" },
        { new UrlBuilder("https", "aldaviva.com").Path((string?) null), "https://aldaviva.com/" },
        { new UrlBuilder("https", "aldaviva.com").Path("/"), "https://aldaviva.com/" },
        { new UrlBuilder("https", "aldaviva.com").Path("docs"), "https://aldaviva.com/docs" },
        { new UrlBuilder("https", "aldaviva.com").Path("docs/"), "https://aldaviva.com/docs/" },
        { new UrlBuilder("https", "aldaviva.com").Path("docs//"), "https://aldaviva.com/docs/" },
        { new UrlBuilder("https", "aldaviva.com").Path("a", "b", "c"), "https://aldaviva.com/a/b/c" },
        { new UrlBuilder("https", "aldaviva.com").Path("a", "b", "c/"), "https://aldaviva.com/a/b/c/" },
        { new UrlBuilder("https", "aldaviva.com").Path("a/b/c"), "https://aldaviva.com/a/b/c" },
        { new UrlBuilder("https", "aldaviva.com").Path("a/b/c/"), "https://aldaviva.com/a/b/c/" }
    };

}