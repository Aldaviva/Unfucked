using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Mime;
using System.Net.NetworkInformation;
using System.Text;
using Unfucked.HTTP.Serialization;

namespace Tests.HTTP;

public class GenericBodyReaderTest {

    private readonly GenericBodyReader reader = new();

    [Fact]
    public async Task IUtf8SpanParsableTest() {
        byte[] httpResponseBody = [.. "2.16.2"u8];

        using HttpResponseMessage httpResponseMessage = new()
            { Content = new ByteArrayContent(httpResponseBody) { Headers = { ContentType = MediaTypeHeaderValue.Parse("text/plain; charset=UTF-8") } } };

        reader.CanRead<Version>(null).Should().BeTrue();
        Version actual = await reader.Read<Version>(httpResponseMessage, null, null, CancellationToken.None);
        actual.Should().Be(new Version(2, 16, 2));
    }

    [Fact]
    public async Task IParsableTest() {
        const string responseBody = "true";

        using HttpResponseMessage httpResponseMessage = new() { Content = new StringContent(responseBody) };

        reader.CanRead<bool>(null).Should().BeTrue();

        bool actual = await reader.Read<bool>(httpResponseMessage, null, null, CancellationToken.None);
        actual.Should().BeTrue();
    }

    [Fact]
    public async Task ConstructorStringTest() {
        const string responseBody = "https://aldaviva.com/";

        using HttpResponseMessage httpResponseMessage = new() { Content = new StringContent(responseBody) };

        reader.CanRead<Uri>(null).Should().BeTrue();

        Uri actual = await reader.Read<Uri>(httpResponseMessage, null, null, CancellationToken.None);
        actual.Host.Should().Be("aldaviva.com");
    }

    [Fact]
    public async Task ParseMethod() {
        const string responseBody = "11:22:33:44:55:66";

        using HttpResponseMessage httpResponseMessage = new() { Content = new StringContent(responseBody) };

        reader.CanRead<PhysicalAddress>(null).Should().BeTrue();

        PhysicalAddress actual = await reader.Read<PhysicalAddress>(httpResponseMessage, null, null, CancellationToken.None);
        actual.Should().Be(new PhysicalAddress([0x11, 0x22, 0x33, 0x44, 0x55, 0x66]));
    }

    [Fact]
    public async Task TryParseMethod() {
        const string responseBody = "one";

        using HttpResponseMessage httpResponseMessage = new() { Content = new StringContent(responseBody) };

        reader.CanRead<EnglishNumber>(null).Should().BeTrue();

        EnglishNumber actual = await reader.Read<EnglishNumber>(httpResponseMessage, null, null, CancellationToken.None);
        actual.number.Should().Be(1);
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

    internal class EnglishNumber {

        public int number { get; private init; }

        public static bool TryParse(string str, out EnglishNumber? result) {
            result = (int?) (str.ToLower(CultureInfo.GetCultureInfo("en")) switch {
                "one"   => 1,
                "two"   => 2,
                "three" => 3,
                _       => null
            }) is {} number ? new EnglishNumber { number = number } : null;
            return result is not null;
        }

    }

}