using STUN.Enums;
using STUN.StunResult;
using System.Net;

namespace Unfucked.STUN;

/// <summary>
/// Interface for any client that can get the WAN IP address for itself using any protocol.
/// </summary>
public interface ISelfWanAddressClient {

    /// <summary>
    /// Get the WAN IP address for the current computer.
    /// </summary>
    /// <param name="cancellationToken">if you want to cancel the task before it finishes</param>
    /// <returns>The current self IP address of this computer if it could be determined, as well as the hostname and IP address of the server used in this request.</returns>
    Task<SelfWanAddressResponse> GetSelfWanAddress(CancellationToken cancellationToken = default);

}

/// <summary>
/// The current self IP address of this computer on the Internet if it could be determined, as well as the hostname and IP address of the server used in this request.
/// </summary>
/// <param name="SelfWanAddress">The current self IP address of this computer on the Internet, or <c>null</c> if it could not be determined.</param>
/// <param name="Server">The hostname of the server used to determine this WAN address.</param>
/// <param name="ServerAddress">The IP address of the server used to determine this WAN address.</param>
public record SelfWanAddressResponse(IPAddress? SelfWanAddress, DnsEndPoint Server, IPEndPoint ServerAddress);

/// <summary>
/// A STUN client where each instance is safe to use from multiple callers concurrently without locking. Each client makes requests to a pool of servers, falling back to other servers in case any of them are not responding.
/// </summary>
/// <param name="stunProvider">Factory method that constructs STUN clients.</param>
public class ThreadSafeMultiServerStunClient(Func<IStunClient5389> stunProvider): ISelfWanAddressClient {

    /// <inheritdoc />
    public async Task<SelfWanAddressResponse> GetSelfWanAddress(CancellationToken cancellationToken = default) {
        using IStunClient5389 stun     = stunProvider();
        StunResult5389        response = await stun.BindingTestAsync(cancellationToken).ConfigureAwait(false);
        return new SelfWanAddressResponse(response.BindingTestResult == BindingTestResult.Success ? response.PublicEndPoint?.Address : null, stun.Server, stun.ServerAddress);
    }

}