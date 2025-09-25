using System.Net;
using System.Net.Mime;
using System.Text;
using System.Text.Json.Nodes;
using Unfucked.HTTP;
using Unfucked.HTTP.Filters;

namespace Tests.HTTP;

public class UnfuckedHttpClientTests {

    private static readonly Encoding Utf8 = new UTF8Encoding(false, true);

    private readonly UnfuckedHttpClient httpClient = A.Fake<UnfuckedHttpClient>();

    [Fact]
    public async Task Mockable() {
        A.CallTo(() => httpClient.SendAsync(A<HttpRequest>._, A<CancellationToken>._)).Returns(new HttpResponseMessage(HttpStatusCode.OK) {
            Content = new StringContent("""{ "mocked": true }""", Encoding.UTF8, "application/json")
        });

        JsonObject actual = await httpClient.Target("https://httpbin.org/")
            .Path("get")
            .Accept(MediaTypeNames.Application.Json)
            .Get<JsonObject>();

        actual["mocked"]?.GetValue<bool>().Should().BeTrue();
    }

    [Theory]
    [InlineData("", "")]
    [InlineData("\n", "")]
    [InlineData("\r\n", "")]
    [InlineData("a", "a")]
    [InlineData("a\r", "a")]
    [InlineData("a\n", "a")]
    [InlineData("a\r\n", "a")]
    [InlineData("a\r\n\r\n", "a")]
    [InlineData("a\n\n\n\n", "a")]
    [InlineData("a\nb\n", "a\nb")]
    public void TrimTrailingLineEndings(string input, string expected) {
        using MemoryStream stream = new();
        using (StreamWriter streamWriter = new(stream, Utf8, leaveOpen: true)) {
            streamWriter.Write(input);
        }
        WireLoggingFilter.WireLoggingStream.TrimTrailingLineEndings(stream);
        using (StreamReader streamReader = new(stream, Utf8, leaveOpen: true)) {
            string actual = streamReader.ReadToEnd();
            actual.Should().Be(expected);
        }
    }

}