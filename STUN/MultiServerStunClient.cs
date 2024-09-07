using STUN.Enums;
using STUN.StunResult;
using System.Collections.Frozen;
using System.Diagnostics;
using System.Net;
using System.Runtime.CompilerServices;
using Unfucked.Caching;

namespace Unfucked.STUN;

/// <summary>
/// A STUN client that makes requests to a constantly-updated pool of online public STUN servers, retrying with different servers if any of them are not responding, including a hardcoded fallback list if the online pool is unavailable.
/// </summary>
/// <param name="http">HTTP client used to fetch online pool of public STUN servers.</param>
/// <param name="stunClientFactory">Factory class for single-server STUN clients.</param>
/// <param name="serverBlacklist">Set of STUN server hostnames to never send requests to, for example if they are known to return incorrect responses.</param>
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

    private readonly IReadOnlySet<string> blacklistedServers = (serverBlacklist?.Select(s => s.ToLowerInvariant()) ?? new HashSet<string>(0)).ToFrozenSet();

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

    private async IAsyncEnumerable<IStunClient5389> GetStunClients([EnumeratorCancellation] CancellationToken cancellationToken = default) {
        const string  stunListCacheKey = "always-on-stun";
        DnsEndPoint[] servers          = [];
        try {
            servers =
                (await ServersCache.GetOrAdd(stunListCacheKey, async () => await FetchStunServers(cancellationToken).ConfigureAwait(false), StunListCacheDuration).ConfigureAwait(false)).ToArray();
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
    private async Task<IEnumerable<DnsEndPoint>> FetchStunServers(CancellationToken cancellationToken) {
        Debug.WriteLine("Fetching list of STUN servers from pradt2/always-online-stun");
        ICollection<DnsEndPoint> servers = Enumerable
            .ExceptBy<DnsEndPoint, string>((await http.GetStringAsync(StunServerListUrl, cancellationToken).ConfigureAwait(false))
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
                .Compact(), blacklistedServers, host => host.Host)
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