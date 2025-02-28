using System.Net;
using System.Net.Sockets;

namespace Unfucked;

/// <summary>
/// Methods that make it easier to work with domain name lookups.
/// </summary>
public static class DNS {

    /// <summary>
    /// Resolve the given domain name to an IP address using the default resolver.
    /// </summary>
    /// <param name="host">Domain name to resolve.</param>
    /// <param name="cancellationToken">Allows you to cancel the task before it finishes. Ignored in .NET &lt; 6.</param>
    /// <returns>First IP address that the domain name points to, or <c>null</c> if resolution failed.</returns>
    public static async Task<IPEndPoint?> Resolve(this DnsEndPoint host, CancellationToken cancellationToken = default) {
        try {
            IEnumerable<IPAddress> response;
#if NET6_0_OR_GREATER
            response = await Dns.GetHostAddressesAsync(host.Host, host.AddressFamily, cancellationToken).ConfigureAwait(false);
#else
            response = await System.Net.Dns.GetHostAddressesAsync(host.Host).ConfigureAwait(false);
#endif
            return response.FirstOrDefault(addr => host.AddressFamily == AddressFamily.Unspecified || host.AddressFamily == addr.AddressFamily) is { } address
                ? new IPEndPoint(address, host.Port) : null;
        } catch (SocketException) {
            return null;
        }
    }

}