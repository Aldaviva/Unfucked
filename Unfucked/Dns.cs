using System.Net;
using System.Net.Sockets;

namespace Unfucked;

public static class Dns {

    public static async Task<IPEndPoint?> Resolve(this DnsEndPoint host, CancellationToken ct = default) {
        try {
            Task<IPAddress[]> responseTask;
#if NET6_0_OR_GREATER
            responseTask = System.Net.Dns.GetHostAddressesAsync(host.Host, host.AddressFamily, ct);
#else
            responseTask = System.Net.Dns.GetHostAddressesAsync(host.Host);
#endif
            return await responseTask.ConfigureAwait(false) is { Length: > 0 } response ? new IPEndPoint(response[0], host.Port) : null;
        } catch (SocketException) {
            return null;
        }
    }

}