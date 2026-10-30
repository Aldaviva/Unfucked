using System.Net.Http.Headers;
using System.Net.Mime;
using System.Net.NetworkInformation;
using System.Text;
using Unfucked.HTTP.Serialization;

namespace Tests.HTTP;

public class GenericParserTest {

    private readonly GenericBodyReader reader = new(new StringBodyReader());

    [Fact]
    public async Task ParseVersion() {
        byte[] httpResponseBody = [.. "2.16.2"u8];

        using HttpResponseMessage httpResponseMessage = new()
            { Content = new ByteArrayContent(httpResponseBody) { Headers = { ContentType = MediaTypeHeaderValue.Parse("text/plain; charset=UTF-8") } } };

        reader.CanRead<Version>(null, null).Should().BeTrue();

        Version actual = await reader.Read<Version>(await httpResponseMessage.Content.ReadAsStreamAsync(), httpResponseMessage.StatusCode, httpResponseMessage.Headers,
            httpResponseMessage.Content.Headers, httpResponseMessage.RequestMessage!, httpResponseMessage, null, null, CancellationToken.None);
        actual.Should().Be(new Version(2, 16, 2));
    }

    [Fact]
    public async Task ParseString() {
        const string responseBody = "true";

        using HttpResponseMessage httpResponseMessage = new() { Content = new StringContent(responseBody) };

        reader.CanRead<bool>(null, null).Should().BeTrue();

        bool actual = await reader.Read<bool>(await httpResponseMessage.Content.ReadAsStreamAsync(), httpResponseMessage.StatusCode, httpResponseMessage.Headers, httpResponseMessage.Content.Headers,
            httpResponseMessage.RequestMessage!, httpResponseMessage, null, null, CancellationToken.None);
        actual.Should().BeTrue();
    }

    [Fact]
    public async Task ConstructorString() {
        const string responseBody = "https://aldaviva.com/";

        using HttpResponseMessage httpResponseMessage = new() { Content = new StringContent(responseBody) };

        reader.CanRead<Uri>(null, null).Should().BeTrue();

        Uri actual = await reader.Read<Uri>(await httpResponseMessage.Content.ReadAsStreamAsync(), httpResponseMessage.StatusCode, httpResponseMessage.Headers, httpResponseMessage.Content.Headers,
            httpResponseMessage.RequestMessage!, httpResponseMessage, null, null, CancellationToken.None);
        actual.Host.Should().Be("aldaviva.com");
    }

    [Fact]
    public async Task ParseMethod() {
        const string responseBody = "11:22:33:44:55:66";

        using HttpResponseMessage httpResponseMessage = new() { Content = new StringContent(responseBody) };

        reader.CanRead<PhysicalAddress>(null, null).Should().BeTrue();

        PhysicalAddress actual = await reader.Read<PhysicalAddress>(await httpResponseMessage.Content.ReadAsStreamAsync(), httpResponseMessage.StatusCode, httpResponseMessage.Headers,
            httpResponseMessage.Content.Headers, httpResponseMessage.RequestMessage!, httpResponseMessage, null, null, CancellationToken.None);
        actual.Should().Be(new PhysicalAddress([0x11, 0x22, 0x33, 0x44, 0x55, 0x66]));
    }

    [Theory, MemberData(nameof(ReadStringNotUtf8Data))]
    public async Task ReadStringNotUtf8(Encoding encoding, string input) {
        var    body   = new ByteArrayContent(encoding.GetBytes(input)) { Headers = { ContentType = new MediaTypeHeaderValue(MediaTypeNames.Text.Plain, encoding.WebName) } };
        string actual = await body.ReadAsStringAsync();
        actual.Should().Be(input);
    }

    public static TheoryData<Encoding, string> ReadStringNotUtf8Data => new() {
        { Encoding.Unicode, "hello" },
        { Encoding.BigEndianUnicode, "hello" },
        { Encoding.Latin1, "éü" }
    };

}