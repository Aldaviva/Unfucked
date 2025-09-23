using System.Net;
using System.Net.Mime;
using System.Text;
using System.Text.Json.Nodes;
using Unfucked.HTTP;

namespace Tests.HTTP;

public class UnfuckedHttpClientTests {

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

}