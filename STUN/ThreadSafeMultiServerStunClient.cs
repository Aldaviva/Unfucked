using STUN.Enums;
using STUN.StunResult;

namespace Unfucked.STUN;

/// <summary>
/// A STUN client where each instance is safe to use from multiple callers concurrently without locking. Each client makes requests to a pool of servers, falling back to other servers in case any of them are not responding.
/// </summary>
/// <param name="stunProvider">Factory method that constructs STUN clients.</param>
public class ThreadSafeMultiServerStunClient(Func<IStunClient5389> stunProvider): ISelfWanAddressClient {

    /// <inheritdoc />
    public async Task<SelfWanAddressResponse> GetSelfWanAddress(CancellationToken cancellationToken = default) {
        using IStunClient5389 stun     = stunProvider();
        StunResult5389        response = await stun.BindingTestAsync(cancellationToken).ConfigureAwait(false);
        return new SelfWanAddressResponse(response.BindingTestResult == BindingTestResult.Success ? response.PublicEndPoint!.Address : null, stun.Server, stun.ServerAddress);
    }

}