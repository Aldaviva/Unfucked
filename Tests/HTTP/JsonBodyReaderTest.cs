using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Unfucked.HTTP.Serialization;

namespace Tests.HTTP;

public class JsonBodyReaderTest {

    private static readonly Encoding utf8 = Encoding.UTF8;

    private readonly JsonBodyReader reader = new();

    [Fact]
    public async Task ParseNormalType() {
        reader.CanRead<bool>(new MediaTypeHeaderValue("application/json"), null).Should().BeTrue();

        bool actual = await reader.Read<bool>("true".ToByteStream(utf8), HttpStatusCode.OK, null!, null!, null!, null!, utf8, null, CancellationToken.None);

        actual.Should().BeTrue();
    }

    [Fact]
    public async Task ParseAsyncEnumerableArray() {
        Stream jsonStream = "[1,2,3]".ToByteStream(utf8);
        jsonStream.CanRead.Should().BeTrue();

        IReadOnlyList<int> actual = await (await reader.Read<IAsyncEnumerable<int>>(jsonStream, HttpStatusCode.OK, null!, null!, null!, null!, utf8, null, CancellationToken.None))
            .ToListAsync();

        actual.Should().BeEquivalentTo([1, 2, 3]);
    }

    [Fact]
    public async Task ParseAsyncEnumerableMultiRoot() {
        IReadOnlyList<int> actual = await (await reader.Read<IAsyncEnumerable<int>>("1\n2\n3".ToByteStream(utf8), HttpStatusCode.OK, null!, null!, null!, null!, utf8, null, CancellationToken.None))
            .ToListAsync();

        actual.Should().BeEquivalentTo([1, 2, 3]);
    }

}