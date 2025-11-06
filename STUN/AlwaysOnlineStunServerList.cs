using System.Collections.Frozen;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using Unfucked.Caching;

namespace Unfucked.STUN;

public interface StunServerList: IDisposable {

    Task<IEnumerable<DnsEndPoint>> ListStunServers();

}

public class AlwaysOnlineStunServerList: StunServerList {

    private const string CACHE_KEY = "always-on-stun";

    private static readonly Random                   RANDOM                   = new();
    private static readonly TimeSpan                 STUN_LIST_CACHE_DURATION = TimeSpan.FromDays(1);
    private static readonly Uri                      STUN_SERVER_LIST_URL     = new("https://raw.githubusercontent.com/pradt2/always-online-stun/master/valid_hosts.txt");
    private static readonly IEnumerable<DnsEndPoint> FALLBACK_SERVERS;

    internal readonly Cache<string, IEnumerable<DnsEndPoint>> ServersCache;
    private readonly  IReadOnlySet<string>                    blacklistedServers;
    private readonly  HttpClient                              http;
    private readonly  bool                                    ownsHttpClient;

    static AlwaysOnlineStunServerList() {
        Span<DnsEndPoint> fallbackServers = [
            new("stun.freeswitch.org", 3478, AddressFamily.InterNetwork),
            new("stun.ucsb.edu", 3478, AddressFamily.InterNetwork),
            new("stun.bethesda.net", 3478, AddressFamily.InterNetwork),
            new("stun.nextcloud.com", 3478, AddressFamily.InterNetwork)
        ];
        RANDOM.Shuffle(fallbackServers);
        FALLBACK_SERVERS = fallbackServers.ToArray();
    }

    /// <summary>
    /// A STUN client that makes requests to a constantly-updated pool of online public STUN servers, retrying with different servers if any of them are not responding, including a hardcoded fallback list if the online pool is unavailable.
    /// </summary>
    /// <param name="http">HTTP client used to fetch online pool of public STUN servers.</param>
    /// <param name="serverBlacklist">Set of STUN server hostnames to never send requests to, for example if they are known to return incorrect responses.</param>
    public AlwaysOnlineStunServerList(HttpClient? http, IEnumerable<string>? serverBlacklist = null) {
        this.http          = http ?? new HttpClient();
        ownsHttpClient     = http is null;
        blacklistedServers = (serverBlacklist?.Select(s => s.ToLowerInvariant()) ?? new HashSet<string>(0)).ToFrozenSet();

        ServersCache = new InMemoryCache<string, IEnumerable<DnsEndPoint>>(new CacheOptions { ExpireAfterWrite = STUN_LIST_CACHE_DURATION, InitialCapacity = 1 },
            async _ => await FetchStunServers().ConfigureAwait(false));
    }

    /// <exception cref="HttpRequestException"></exception>
    /// <exception cref="TaskCanceledException"></exception>
    private async Task<IEnumerable<DnsEndPoint>> FetchStunServers() {
        Debug.WriteLine("Fetching list of STUN servers from pradt2/always-online-stun");
        ICollection<DnsEndPoint> servers = (await http.GetStringAsync(STUN_SERVER_LIST_URL).ConfigureAwait(false))
            .TrimEnd()
            .Split('\n')
            .Select(line => {
                string[] columns = line.Split(':', 2);
                try {
                    return new DnsEndPoint(columns[0], columns.ElementAtOrDefault(1) is { } port ? ushort.Parse(port) : 3478, AddressFamily.InterNetwork);
                } catch (FormatException) {
                    return null;
                }
            })
            .Compact()
            .ExceptBy(blacklistedServers, host => host.Host)
            .ToList()
            .AsReadOnly();

        Debug.WriteLine("Fetched {0:N0} STUN servers", servers.Count);
        return servers;
    }

    public async Task<IEnumerable<DnsEndPoint>> ListStunServers() {
        DnsEndPoint[] servers = [];
        try {
            servers = (await ServersCache.Get(CACHE_KEY).ConfigureAwait(false)).ToArray();
        } catch (HttpRequestException) { } catch (TaskCanceledException) { // timeout
        } catch (Exception e) when (e is not OutOfMemoryException) { }

        RANDOM.Shuffle(servers);

        return servers.Concat(FALLBACK_SERVERS);
    }

    protected virtual void Dispose(bool disposing) {
        if (disposing) {
            ServersCache.Dispose();
            if (ownsHttpClient) {
                http.Dispose();
            }
        }
    }

    public void Dispose() {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

}