using G6.GandiLiveDns;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.Reflection;

// ReSharper disable InconsistentNaming - these names must match a third-party library

namespace Unfucked.DNS;

/// <inheritdoc />
[GeneratedCode("G6.GandiLiveDns", "1.0.0")]
public class GandiLiveDns: IGandiLiveDns {

    #region New

    private readonly G6.GandiLiveDns.GandiLiveDns gandi;
    private readonly bool                         shouldDisposeHttpClient;

    private GandiLiveDns(G6.GandiLiveDns.GandiLiveDns gandi, bool shouldDisposeHttpClient) {
        this.gandi                   = gandi;
        this.shouldDisposeHttpClient = shouldDisposeHttpClient;
    }

    /// <summary>
    /// Construct a new instance with the default Gandi API base URL (<c>https://api.gandi.net/v5/livedns</c>) and a privately owned <see cref="HttpClient"/> which will be disposed along with this new instance
    /// </summary>
    public GandiLiveDns(): this(new G6.GandiLiveDns.GandiLiveDns(), true) { }

    /// <summary>
    /// Construct a new instance with a custom Gandi API base URL and a custom <see cref="HttpClient"/> instance, which will not be disposed along with this instance
    /// </summary>
    /// <param name="baseUrl">API base URL for the LiveDNS service. The default is <c>https://api.gandi.net/v5/livedns</c>. The sandbox is <c>https://api.sandbox.gandi.net/v5/livedns</c> (see <see href="https://api.sandbox.gandi.net/docs/sandbox/"/> for setup instructions).</param>
    /// <param name="httpClient">An <see cref="HttpClient"/> instance that you created and will dispose of yourself.</param>
    public GandiLiveDns(string baseUrl, HttpClient httpClient): this(new G6.GandiLiveDns.GandiLiveDns(baseUrl, httpClient), false) { }

    /// <inheritdoc />
    public void Dispose() {
        if (shouldDisposeHttpClient) {
            ((HttpClient) typeof(G6.GandiLiveDns.GandiLiveDns).GetFields(BindingFlags.NonPublic | BindingFlags.Instance).First(f => f.FieldType == typeof(HttpClient)).GetValue(gandi)!).Dispose();
        }
    }

    #endregion

    #region Delegated

    /// <inheritdoc />
    public string BaseUrl {
        [DebuggerStepThrough] get => gandi.BaseUrl;
        [DebuggerStepThrough] set => gandi.BaseUrl = value;
    }

    /// <inheritdoc />
    public string ApiKey {
        [DebuggerStepThrough] get => gandi.Apikey;
        [DebuggerStepThrough] set => gandi.Apikey = value;
    }

    /// <summary>
    /// <c>true</c> to buffer HTTP responses as an entire string in memory before parsing it as JSON, or <c>false</c> (default) to stream it to the JSON parser
    /// </summary>
    public bool ReadResponseAsString {
        [DebuggerStepThrough] get => gandi.ReadResponseAsString;
        [DebuggerStepThrough] set => gandi.ReadResponseAsString = value;
    }

    /// <inheritdoc />
    [DebuggerStepThrough]
    public async Task<IEnumerable<GandiLiveDnsListRecord>> GetDomainRecords(string domain, CancellationToken cancellationToken) {
        return await gandi.GetDomainRecords(domain, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    [DebuggerStepThrough]
    public async Task<bool> PostDomainRecord(string domain, string name, string type, string[] values, int ttl, CancellationToken cancellationToken) {
        return await gandi.PostDomainRecord(domain, name, type, values, ttl, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    [DebuggerStepThrough]
    public async Task<bool> PutDomainRecord(string domain, string name, string type, string[] values, int ttl, CancellationToken cancellationToken) {
        return await gandi.PutDomainRecord(domain, name, type, values, ttl, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    [DebuggerStepThrough]
    public async Task<bool> DeleteDomainRecord(string domain, string name, string type, CancellationToken cancellationToken) {
        return await gandi.DeleteDomainRecord(domain, name, type, cancellationToken).ConfigureAwait(false);
    }

    #endregion

}