using System.Net;

namespace Unfucked.STUN;

/// <summary>
/// Interface for a factory class that can generate STUN clients which point to one specific STUN server by its FQDN.
/// </summary>
public interface IStunClientFactory {

    /// <summary>
    /// Construct a new single-server, single-threaded STUN client that will make queries against the given STUN server's FQDN.
    /// </summary>
    /// <param name="server">FQDN of the STUN server.</param>
    /// <returns>A non-thread-safe STUN client that will make UDP STUN requests to the given <paramref name="server"/>.</returns>
    Task<IStunClient5389?> CreateStunClient(DnsEndPoint server);

}

/// <summary>
/// Factory class that can generate STUN clients which point to one specific STUN server by its FQDN.
/// </summary>
public class StunClient5389Factory: IStunClientFactory {

    private static readonly IPEndPoint LOCAL_HOST = new(IPAddress.Any, 0);

    /// <inheritdoc />
    public async Task<IStunClient5389?> CreateStunClient(DnsEndPoint server) => await server.Resolve().ConfigureAwait(false) is { } addr ? new StunClient5389UDP(addr, server.Host, LOCAL_HOST) : null;

}