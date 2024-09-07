using STUN.Proxy;
using System.Net;

// ReSharper disable InconsistentNaming

namespace Unfucked.STUN;

/// <summary>
/// Like <see cref="global::STUN.Client.IStunClient5389"/> but it allows consumers to introspect the STUN server that the instance was configured with, without restoring to reflection on private fields.
/// </summary>
public interface IStunClient5389: global::STUN.Client.IStunClient5389 {

    /// <summary>
    /// The STUN server IP address and port that this instance was configured with.
    /// </summary>
    IPEndPoint ServerAddress { get; }

    /// <summary>
    /// The STUN server hostname and port that this instance was configured with.
    /// </summary>
    DnsEndPoint Server { get; }

}

/// <inheritdoc cref="IStunClient5389" />
public class StunClient5389UDP(IPEndPoint server, string serverName, IPEndPoint local, IUdpProxy? proxy = null): global::STUN.Client.StunClient5389UDP(server, local, proxy), IStunClient5389 {

    /// <inheritdoc />
    public IPEndPoint ServerAddress { get; } = server;

    /// <inheritdoc />
    public DnsEndPoint Server { get; } = new(serverName, server.Port);

}