using System.Net;
using System.Net.Sockets;

namespace Tests.Unfucked;

public class DnsTest {

    /*
     * This test requires an Internet connection, unfortunately. There is no way to mock System.Net.Dns.GetHostAddressesAsync without a profiler.
     */
    [Fact]
    public async Task Resolve() {
        DnsEndPoint domainName = new("one.one.one.one", 0, AddressFamily.InterNetwork);

        IPEndPoint? actual = await domainName.Resolve();

        actual.Should().NotBeNull();
        actual!.AddressFamily.Should().Be(AddressFamily.InterNetwork);
        actual.Address.Should().BeOneOf(IPAddress.Parse("1.1.1.1"), IPAddress.Parse("1.0.0.1"));
    }

}