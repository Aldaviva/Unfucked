using EmbedIO;
using System.Net;
using System.Net.Mime;
using System.Text;
using Unfucked.HTTP;

namespace Tests.HTTP;

public class PartiallyBufferedStreamTest(PartiallyBufferedStreamTest.WebServerFixture webServerFixture): IClassFixture<PartiallyBufferedStreamTest.WebServerFixture> {

    private readonly UnfuckedHttpClient client = new();

    [Fact]
    public async Task Works() {
        Uri baseUrl = webServerFixture.ListenOnRandomPort(server =>
            server.WithAction("/test.json", HttpVerbs.Get, async ctx => { await ctx.SendStringAsync("""{ "Hello": "World" }""", MediaTypeNames.Application.Octet, Encoding.UTF8); }));

        IWebTarget webTarget = client.Target(baseUrl)
            .Path("test.json");

        HelloDto actual = await webTarget.Get<HelloDto>();
        actual.Hello.Should().Be("World");
    }

    private record HelloDto {

        public required string Hello { get; init; }

    }

    public class WebServerFixture: IAsyncDisposable {

        // public readonly WebServer WebServer = new(HttpListenerMode.EmbedIO);
        private WebServer? webServer;
        private ValueTask  shutDown = ValueTask.CompletedTask;

        public Uri ListenOnRandomPort(Action<WebServer> configure) {
            UrlBuilder urlBuilder = new("http", IPAddress.Loopback.ToString());

            Uri? baseUrl = null;
            while (baseUrl is null || webServer is null) {
                ushort port = (ushort) Random.Shared.Next(1024, ushort.MaxValue + 1);
                baseUrl = urlBuilder.Port(port).ToUrl();

                webServer = new WebServer(HttpListenerMode.EmbedIO, baseUrl.ToString());
                configure(webServer);

                try {
                    shutDown = new ValueTask(webServer.RunAsync());
                } catch (Exception e) when (e is not OutOfMemoryException) {
                    webServer.Dispose();
                    // loop and try another random port
                }
            }
            webServer.State.Should().Be(WebServerState.Listening);
            return baseUrl;
        }

        public ValueTask DisposeAsync() {
            webServer?.Dispose();
            return shutDown;
        }

    }

}