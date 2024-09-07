using System.Net;
using System.Net.Sockets;

namespace Unfucked;

/// <summary>
/// Methods that make it easier to work with domain name lookups.
/// </summary>
public static class Dns {

    /// <summary>
    /// Resolve the given domain name to an IP address using the default resolver.
    /// </summary>
    /// <param name="host">Domain name to resolve.</param>
    /// <param name="cancellationToken">Allows you to cancel the task before it finishes. Ignored in .NET &lt; 6.</param>
    /// <returns>First IP address that the domain name points to, or <c>null</c> if resolution failed.</returns>
    public static async Task<IPEndPoint?> Resolve(this DnsEndPoint host, CancellationToken cancellationToken = default) {
        try {
            Task<IPAddress[]> responseTask;
#if NET6_0_OR_GREATER
            responseTask = System.Net.Dns.GetHostAddressesAsync(host.Host, host.AddressFamily, cancellationToken);
#else
            responseTask = System.Net.Dns.GetHostAddressesAsync(host.Host);
#endif
            return await responseTask.ConfigureAwait(false) is { LongLength: > 0 } response ? new IPEndPoint(response[0], host.Port) : null;
        } catch (SocketException) {
            return null;
        }
    }

}