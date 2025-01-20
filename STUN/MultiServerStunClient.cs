using STUN.Enums;
using STUN.StunResult;
using System.Diagnostics;
using System.Net;

namespace Unfucked.STUN;

/// <summary>
/// A STUN client that makes requests to a constantly-updated pool of online public STUN servers, retrying with different servers if any of them are not responding, including a hardcoded fallback list if the online pool is unavailable.
/// </summary>
public class MultiServerStunClient: IStunClient5389 {

    private readonly IStunClientFactory stunClientFactory;
    private readonly StunServerList     stunServerList;

    /// <summary>
    /// A STUN client that makes requests to a constantly-updated pool of online public STUN servers, retrying with different servers if any of them are not responding, including a hardcoded fallback list if the online pool is unavailable.
    /// </summary>
    /// <param name="stunClientFactory">Factory class for single-server STUN clients.</param>
    /// <param name="stunServerList">list of stun servers to attempt to contact</param>
    public MultiServerStunClient(IStunClientFactory stunClientFactory, StunServerList stunServerList) {
        this.stunClientFactory = stunClientFactory;
        this.stunServerList    = stunServerList;
    }

    /// <summary>
    /// The result of the most recent STUN request that this client instance processed.
    /// </summary>
    public StunResult5389 State { get; private set; } = new();

    /// <summary>
    /// The server hostname that this client instance used for its most recent request.
    /// </summary>
    public DnsEndPoint Server { get; private set; } = null!;

    /// <summary>
    /// The server IP address that this client instance used for its most recent request.
    /// </summary>
    public IPEndPoint ServerAddress { get; private set; } = null!;

    private async IAsyncEnumerable<IStunClient5389> GetStunClients() {
        foreach (DnsEndPoint host in await stunServerList.ListStunServers().ConfigureAwait(false)) {
            if (await stunClientFactory.CreateStunClient(host).ConfigureAwait(false) is { } stunClient) {
                yield return stunClient;
            }
        }
    }

    /// <inheritdoc />
    public async ValueTask QueryAsync(CancellationToken cancellationToken = default) {
        await foreach (IStunClient5389 stun in GetStunClients().WithCancellation(cancellationToken).ConfigureAwait(false)) {
            using (stun) {
                Server        = stun.Server;
                ServerAddress = stun.ServerAddress;
                await stun.QueryAsync(cancellationToken).ConfigureAwait(false);
                State = stun.State;
                if (IsSuccessfulResponse(State)) {
                    break;
                } else {
                    Debug.WriteLine("STUN request to {0} ({1}:{2}) failed, trying another server", stun.Server.Host, stun.ServerAddress.Address, stun.ServerAddress.Port);
                }
            }
        }
    }

    /// <inheritdoc />
    public async ValueTask<StunResult5389> BindingTestAsync(CancellationToken cancellationToken = default) {
        await foreach (IStunClient5389 stun in GetStunClients().WithCancellation(cancellationToken).ConfigureAwait(false)) {
            using (stun) {
                Server        = stun.Server;
                ServerAddress = stun.ServerAddress;
                Debug.WriteLine("Sending UDP STUN request to {0} ({1}:{2})", stun.Server.Host, stun.ServerAddress.Address, stun.ServerAddress.Port);
                State = await stun.BindingTestAsync(cancellationToken).ConfigureAwait(false);
                if (IsSuccessfulResponse(State)) {
                    break;
                } else {
                    Debug.WriteLine("STUN request to {0} ({1}:{2}) failed, trying another server", stun.Server.Host, stun.ServerAddress.Address, stun.ServerAddress.Port);
                }
            }
        }
        return State;
    }

    /// <inheritdoc />
    public async ValueTask MappingBehaviorTestAsync(CancellationToken cancellationToken = default) {
        await foreach (IStunClient5389 stun in GetStunClients().WithCancellation(cancellationToken).ConfigureAwait(false)) {
            using (stun) {
                Server        = stun.Server;
                ServerAddress = stun.ServerAddress;
                await stun.MappingBehaviorTestAsync(cancellationToken).ConfigureAwait(false);
                State = stun.State;
                if (IsSuccessfulResponse(State)) {
                    break;
                } else {
                    Debug.WriteLine("STUN request to {0} ({1}:{2}) failed, trying another server", stun.Server.Host, stun.ServerAddress.Address, stun.ServerAddress.Port);
                }
            }
        }
    }

    /// <inheritdoc />
    public async ValueTask FilteringBehaviorTestAsync(CancellationToken cancellationToken = default) {
        await foreach (IStunClient5389 stun in GetStunClients().WithCancellation(cancellationToken).ConfigureAwait(false)) {
            using (stun) {
                Server        = stun.Server;
                ServerAddress = stun.ServerAddress;
                await stun.FilteringBehaviorTestAsync(cancellationToken).ConfigureAwait(false);
                State = stun.State;
                if (IsSuccessfulResponse(State)) {
                    break;
                } else {
                    Debug.WriteLine("STUN request to {0} ({1}:{2}) failed, trying another server", stun.Server.Host, stun.ServerAddress.Address, stun.ServerAddress.Port);
                }
            }
        }
    }

    private static bool IsSuccessfulResponse(StunResult5389 response) =>
        response.FilteringBehavior != FilteringBehavior.UnsupportedServer &&
        response.MappingBehavior != MappingBehavior.Fail &&
        response.MappingBehavior != MappingBehavior.UnsupportedServer &&
        response.BindingTestResult != BindingTestResult.Fail &&
        response.BindingTestResult != BindingTestResult.UnsupportedServer &&
        (response.BindingTestResult != BindingTestResult.Success || (response.PublicEndPoint?.Address is { } wanAddr && !IsPrivateAddress(wanAddr)));

    private static bool IsPrivateAddress(IPAddress addr) => addr.GetAddressBytes() is [127, _, _, _] or [10, _, _, _] or [192, 168, _, _] or [172, >= 16 and <= 31, _, _];

    /// <inheritdoc />
    public void Dispose() => GC.SuppressFinalize(this);

}