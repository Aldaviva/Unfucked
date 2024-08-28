using System.Net;

namespace Unfucked;

public interface IStunClientFactory {

    Task<IStunClient5389?> CreateStunClient(DnsEndPoint server);

}

public class StunClient5389Factory: IStunClientFactory {

    private static readonly IPEndPoint LocalHost = new(IPAddress.Any, 0);

    /// <inheritdoc />
    public async Task<IStunClient5389?> CreateStunClient(DnsEndPoint server) => await server.Resolve().ConfigureAwait(false) is { } addr ? new StunClient5389UDP(addr, server.Host, LocalHost) : null;

}