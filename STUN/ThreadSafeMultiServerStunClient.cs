using STUN.Enums;
using STUN.StunResult;
using System.Net;

namespace Unfucked;

public interface ISelfWanAddressClient {

    Task<SelfWanAddressResponse> GetSelfWanAddress(CancellationToken ct = default);

}

public record SelfWanAddressResponse(IPAddress? SelfWanAddress, DnsEndPoint Server, IPEndPoint ServerAddress);

public class ThreadSafeMultiServerStunClient(Func<IStunClient5389> stunProvider): ISelfWanAddressClient {

    /// <inheritdoc />
    public async Task<SelfWanAddressResponse> GetSelfWanAddress(CancellationToken ct = default) {
        using IStunClient5389 stun     = stunProvider();
        StunResult5389        response = await stun.BindingTestAsync(ct).ConfigureAwait(false);
        return new SelfWanAddressResponse(response.BindingTestResult == BindingTestResult.Success ? response.PublicEndPoint?.Address : null, stun.Server, stun.ServerAddress);
    }

}