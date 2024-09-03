using STUN.Enums;
using STUN.StunResult;
using System.Collections.Frozen;
using System.Diagnostics;
using System.Net;
using System.Runtime.CompilerServices;

namespace Unfucked;

public class MultiServerStunClient(HttpClient http, IStunClientFactory stunClientFactory, ISet<string>? serverBlacklist = null): IStunClient5389 {

    private static readonly Random   Random                = new();
    private static readonly TimeSpan StunListCacheDuration = TimeSpan.FromDays(1);

    // ExceptionAdjustment: M:System.Uri.#ctor(System.String) -T:System.UriFormatException
    private static readonly Uri StunServerListUrl = new("https://raw.githubusercontent.com/pradt2/always-online-stun/master/valid_hosts.txt");

    internal static readonly IMemoryCache<IEnumerable<DnsEndPoint>> ServersCache = new MemoryCache<IEnumerable<DnsEndPoint>>($"{nameof(MultiServerStunClient)}.{nameof(ServersCache)}");

    private static readonly IList<DnsEndPoint> FallbackServers = [
        new("stun.ekiga.net", 3478),
        new("stun.freeswitch.org", 3478),
        new("stun1.l.google.com", 19302),
        new("stun2.l.google.com", 19302),
        new("stun3.l.google.com", 19302),
        new("stun4.l.google.com", 19302)
    ];

    private readonly IReadOnlySet<string> _blacklistedServers = (serverBlacklist?.Select(s => s.ToLowerInvariant()) ?? new HashSet<string>(0)).ToFrozenSet();

    public StunResult5389 State { get; private set; } = new();
    public DnsEndPoint Server { get; private set; } = null!;
    public IPEndPoint ServerAddress { get; private set; } = null!;

    private async IAsyncEnumerable<IStunClient5389> GetStunClients([EnumeratorCancellation] CancellationToken ct = default) {
        const string  stunListCacheKey = "always-on-stun";
        DnsEndPoint[] servers          = [];
        try {
            servers = (await ServersCache.GetOrAdd(stunListCacheKey, async () => await FetchStunServers(ct).ConfigureAwait(false), StunListCacheDuration).ConfigureAwait(false)).ToArray();
        } catch (HttpRequestException) { } catch (TaskCanceledException) { /* timeout */
        } catch (Exception e) when (e is not OutOfMemoryException) { }

        Random.Shuffle(servers);

        foreach (DnsEndPoint host in servers.Concat(FallbackServers)) {
            if (await stunClientFactory.CreateStunClient(host).ConfigureAwait(false) is { } stunClient) {
                yield return stunClient;
            }
        }
    }

    /// <exception cref="HttpRequestException"></exception>
    /// <exception cref="TaskCanceledException"></exception>
    private async Task<IEnumerable<DnsEndPoint>> FetchStunServers(CancellationToken ct) {
        Debug.WriteLine("Fetching list of STUN servers from pradt2/always-online-stun");
        ICollection<DnsEndPoint> servers = (await http.GetStringAsync(StunServerListUrl, ct).ConfigureAwait(false))
            .TrimEnd()
            .Split('\n')
            .Select(line => {
                string[] columns = line.Split(':', 2);
                try {
                    return new DnsEndPoint(columns[0], columns.ElementAtOrDefault(1) is { } port ? ushort.Parse(port) : 3478);
                } catch (FormatException) {
                    return null;
                }
            })
            .Compact()
            .ExceptBy(_blacklistedServers, host => host.Host)
            .ToList();

        Debug.WriteLine("Fetched {0:N0} STUN servers", servers.Count);
        return servers;
    }

    /// <inheritdoc />
    public async ValueTask QueryAsync(CancellationToken cancellationToken = default) {
        await foreach (IStunClient5389 stun in GetStunClients(cancellationToken).ConfigureAwait(false)) {
            using (stun) {
                Server        = stun.Server;
                ServerAddress = stun.ServerAddress;
                await stun.QueryAsync(cancellationToken).ConfigureAwait(false);
                State = stun.State;
                if (isSuccessfulResponse(State)) {
                    break;
                } else {
                    Debug.WriteLine("STUN request to {0} ({1}:{2}) failed, trying another server", stun.Server.Host, stun.ServerAddress.Address, stun.ServerAddress.Port);
                }
            }
        }
    }

    /// <inheritdoc />
    public async ValueTask<StunResult5389> BindingTestAsync(CancellationToken cancellationToken = default) {
        await foreach (IStunClient5389 stun in GetStunClients(cancellationToken).ConfigureAwait(false)) {
            using (stun) {
                Server        = stun.Server;
                ServerAddress = stun.ServerAddress;
                Debug.WriteLine("Sending UDP STUN request to {0} ({1}:{2})", stun.Server.Host, stun.ServerAddress.Address, stun.ServerAddress.Port);
                State = await stun.BindingTestAsync(cancellationToken).ConfigureAwait(false);
                if (isSuccessfulResponse(State)) {
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
        await foreach (IStunClient5389 stun in GetStunClients(cancellationToken).ConfigureAwait(false)) {
            using (stun) {
                Server        = stun.Server;
                ServerAddress = stun.ServerAddress;
                await stun.MappingBehaviorTestAsync(cancellationToken).ConfigureAwait(false);
                State = stun.State;
                if (isSuccessfulResponse(State)) {
                    break;
                } else {
                    Debug.WriteLine("STUN request to {0} ({1}:{2}) failed, trying another server", stun.Server.Host, stun.ServerAddress.Address, stun.ServerAddress.Port);
                }
            }
        }
    }

    /// <inheritdoc />
    public async ValueTask FilteringBehaviorTestAsync(CancellationToken cancellationToken = default) {
        await foreach (IStunClient5389 stun in GetStunClients(cancellationToken).ConfigureAwait(false)) {
            using (stun) {
                Server        = stun.Server;
                ServerAddress = stun.ServerAddress;
                await stun.FilteringBehaviorTestAsync(cancellationToken).ConfigureAwait(false);
                State = stun.State;
                if (isSuccessfulResponse(State)) {
                    break;
                } else {
                    Debug.WriteLine("STUN request to {0} ({1}:{2}) failed, trying another server", stun.Server.Host, stun.ServerAddress.Address, stun.ServerAddress.Port);
                }
            }
        }
    }

    private static bool isSuccessfulResponse(StunResult5389 response) =>
        response.BindingTestResult != BindingTestResult.Fail &&
        response.BindingTestResult != BindingTestResult.UnsupportedServer &&
        response.FilteringBehavior != FilteringBehavior.UnsupportedServer &&
        response.MappingBehavior != MappingBehavior.Fail &&
        response.MappingBehavior != MappingBehavior.UnsupportedServer;

    /// <inheritdoc />
    public void Dispose() => GC.SuppressFinalize(this);

}