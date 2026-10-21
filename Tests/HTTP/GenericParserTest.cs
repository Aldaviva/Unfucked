using System.Net.Http.Headers;
using System.Net.NetworkInformation;
using Unfucked.HTTP.Serialization;

namespace Tests.HTTP;

public class GenericParserTest {

    private readonly GenericBodyReader reader = new();

    [Fact]
    public async Task ParseVersion() {
        byte[] httpResponseBody = [.. "2.16.2"u8];

        using HttpResponseMessage httpResponseMessage = new()
            { Content = new ByteArrayContent(httpResponseBody) { Headers = { ContentType = MediaTypeHeaderValue.Parse("text/plain; charset=UTF-8") } } };

        reader.CanRead<Version>(null, null).Should().BeTrue();

        Version actual = await reader.Read<Version>(httpResponseMessage, null, null, CancellationToken.None);
        actual.Should().Be(new Version(2, 16, 2));
    }

    [Fact]
    public async Task ParseString() {
        const string responseBody = "true";

        using HttpResponseMessage httpResponseMessage = new() { Content = new StringContent(responseBody) };

        reader.CanRead<bool>(null, null).Should().BeTrue();

        bool actual = await reader.Read<bool>(httpResponseMessage, null, null, CancellationToken.None);
        actual.Should().BeTrue();
    }

    [Fact]
    public async Task ConstructorString() {
        const string responseBody = "https://aldaviva.com/";

        using HttpResponseMessage httpResponseMessage = new() { Content = new StringContent(responseBody) };

        reader.CanRead<Uri>(null, null).Should().BeTrue();

        Uri actual = await reader.Read<Uri>(httpResponseMessage, null, null, CancellationToken.None);
        actual.Host.Should().Be("aldaviva.com");
    }

    [Fact]
    public async Task ParseMethod() {
        const string responseBody = "11:22:33:44:55:66";

        using HttpResponseMessage httpResponseMessage = new() { Content = new StringContent(responseBody) };

        reader.CanRead<PhysicalAddress>(null, null).Should().BeTrue();

        PhysicalAddress actual = await reader.Read<PhysicalAddress>(httpResponseMessage, null, null, CancellationToken.None);
        actual.Should().Be(new PhysicalAddress([0x11, 0x22, 0x33, 0x44, 0x55, 0x66]));
    }

}