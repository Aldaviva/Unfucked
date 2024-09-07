using System.Net;
using System.Net.Sockets;

namespace Tests.Unfucked;

public class DnsTest {

    [Fact]
    public async Task Resolve() {
        DnsEndPoint domainName = new("localhost", 0, AddressFamily.InterNetwork);

        IPEndPoint? actual = await domainName.Resolve();

        actual.Should().NotBeNull();
        actual!.AddressFamily.Should().Be(AddressFamily.InterNetwork);
        actual.Address.Should().BeOneOf(IPAddress.Parse("127.0.0.1"), IPAddress.Parse("127.0.1.1"));
    }

}