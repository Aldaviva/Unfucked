using System.Collections.Frozen;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using Unfucked.Caching;

namespace Unfucked.STUN;

public interface StunServerList {

    Task<IEnumerable<DnsEndPoint>> ListStunServers();

}

public class AlwaysOnlineStunServerList: StunServerList {

    private const string CacheKey = "always-on-stun";

    private static readonly Random   Random                = new();
    private static readonly TimeSpan StunListCacheDuration = TimeSpan.FromDays(1);

    // ExceptionAdjustment: M:System.Uri.#ctor(System.String) -T:System.UriFormatException
    private static readonly Uri StunServerListUrl = new("https://raw.githubusercontent.com/pradt2/always-online-stun/master/valid_hosts.txt");

    private static readonly DnsEndPoint[] FallbackServers = ((Func<DnsEndPoint[]>) (() => {
        DnsEndPoint[] fallbackServers = [
            new("stun.ekiga.net", 3478),
            new("stun.freeswitch.org", 3478),
            new("stun1.l.google.com", 19302),
            new("stun2.l.google.com", 19302),
            new("stun3.l.google.com", 19302),
            new("stun4.l.google.com", 19302)
        ];
        Random.Shuffle(fallbackServers);
        return fallbackServers;
    }))();

    internal readonly Cache<string, IEnumerable<DnsEndPoint>> ServersCache;
    private readonly  IReadOnlySet<string>                    blacklistedServers;
    private readonly  HttpClient                              http;

    /// <summary>
    /// A STUN client that makes requests to a constantly-updated pool of online public STUN servers, retrying with different servers if any of them are not responding, including a hardcoded fallback list if the online pool is unavailable.
    /// </summary>
    /// <param name="http">HTTP client used to fetch online pool of public STUN servers.</param>
    /// <param name="serverBlacklist">Set of STUN server hostnames to never send requests to, for example if they are known to return incorrect responses.</param>
    public AlwaysOnlineStunServerList(HttpClient http, IEnumerable<string>? serverBlacklist = null) {
        this.http          = http;
        blacklistedServers = (serverBlacklist?.Select(s => s.ToLowerInvariant()) ?? new HashSet<string>(0)).ToFrozenSet();

        ServersCache = new InMemoryCache<string, IEnumerable<DnsEndPoint>>(new CacheOptions { ExpireAfterWrite = StunListCacheDuration, InitialCapacity = 1 },
            _ => FetchStunServers());
    }

    /// <exception cref="HttpRequestException"></exception>
    /// <exception cref="TaskCanceledException"></exception>
    private async Task<IEnumerable<DnsEndPoint>> FetchStunServers() {
        Debug.WriteLine("Fetching list of STUN servers from pradt2/always-online-stun");
        ICollection<DnsEndPoint> servers = (await http.GetStringAsync(StunServerListUrl).ConfigureAwait(false))
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
            .ToList();

        Debug.WriteLine("Fetched {0:N0} STUN servers", servers.Count);
        return servers;
    }

    public async Task<IEnumerable<DnsEndPoint>> ListStunServers() {
        DnsEndPoint[] servers = [];
        try {
            servers = (await ServersCache.Get(CacheKey).ConfigureAwait(false)).ToArray();
        } catch (HttpRequestException) { } catch (TaskCanceledException) { // timeout
        } catch (Exception e) when (e is not OutOfMemoryException) { }

        Random.Shuffle(servers);

        return servers.Concat(FallbackServers);
    }

}