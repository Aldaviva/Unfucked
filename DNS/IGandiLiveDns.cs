using G6.GandiLiveDns;
using System.CodeDom.Compiler;

// ReSharper disable InconsistentNaming - these names must match a third-party library

namespace Unfucked.DNS;

/// <summary>
/// <para>Gandi LiveDNS Management API</para>
/// <para>Documentation: <see href="https://api.gandi.net/docs/livedns/" /></para>
/// <para>Marketing page: <see href="https://www.gandi.net/en-US/domain/dns" /></para>
/// </summary>
[GeneratedCode("G6.GandiLiveDns", "1.0.0")]
public interface IGandiLiveDns: IDisposable {

    /// <summary>
    /// <para>Defaults to <c>https://api.gandi.net/v5/livedns</c> for production.</para>
    /// <para>To use the Gandi Public API Sandbox, set this to <c>https://api.sandbox.gandi.net/v5/livedns</c>, create a sandbox account at <see href="https://id.sandbox.gandi.net/"/>, and create a sandbox API key at <see href="https://account.sandbox.gandi.net/"/> to set in <see cref="ApiKey"/>.</para>
    /// </summary>
    string BaseUrl { get; set; }

    /// <summary>
    /// <para>The API Key is the previous mechanism used to do public api calls. They cannot be scoped, they have the same set of permission than the owner of the API Key and users can't have two api keys at the same time.</para>
    /// <para>You can generate or delete your production API key from the API Key Page (<see href="https://account.gandi.net/"/>, in the Security section).</para>
    /// <para>Documentation: <see href="https://api.gandi.net/docs/authentication/#:~:text=API%20Key%20(Deprecated)"/></para>
    /// </summary>
    string ApiKey { get; set; }

    /// <summary>
    /// <para>Documentation: <see href="https://api.gandi.net/docs/livedns/#get-v5-livedns-domains-fqdn-records" /></para>
    /// <para>List records associated with a domain</para>
    /// </summary>
    /// <param name="domain">Domain name</param>
    /// <param name="cancellationToken">Allows the request can be canceled</param>
    /// <returns>List of DNS records for the given <paramref name="domain"/></returns>
    /// <exception cref="ApiException">if the request was unauthorized (401) or forbidden (403)</exception>
    Task<IEnumerable<GandiLiveDnsListRecord>> GetDomainRecords(string domain, CancellationToken cancellationToken);

    /// <summary>
    /// <para>Creates a new record. Will raise a 409 conflict if the record already exists, and return a 200 OK if the record already exists WITH THE SAME VALUES.</para>
    /// <para>Documentation: <see href="https://api.gandi.net/docs/livedns/#post-v5-livedns-domains-fqdn-records" /></para>
    /// </summary>
    /// <param name="domain">Domain name</param>
    /// <param name="name">Name of the record</param>
    /// <param name="type">One of: <c>A</c>, <c>AAAA</c>, <c>ALIAS</c>, <c>CAA</c>, <c>CDS</c>, <c>CNAME</c>, <c>DNAME</c>, <c>DS</c>, <c>KEY</c>, <c>LOC</c>, <c>MX</c>, <c>NAPTR</c>, <c>NS</c>, <c>OPENPGPKEY</c>, <c>PTR</c>, <c>RP</c>, <c>SPF</c>, <c>SRV</c>, <c>SSHFP</c>, <c>TLSA</c>, <c>TXT</c>, <c>WKS</c></param>
    /// <param name="values">A list of values for this record</param>
    /// <param name="ttl">Minimum: 300, maximum: 2592000. The time in seconds that DNS resolvers should cache this record.</param>
    /// <param name="cancellationToken">Allows the request can be canceled</param>
    /// <returns><c>true</c> if the record was created (otherwise, an <see cref="ApiException"/> is thrown, <c>false</c> is never returned)</returns>
    /// <exception cref="ApiException">if the request was unauthorized (401), or forbidden (403), or the DNS record already existed (409)</exception>
    Task<bool> PostDomainRecord(string domain, string name, string type, string[] values, int ttl, CancellationToken cancellationToken);

    /// <summary>
    /// <para>Overwrites a single record with <paramref name="name"/> and <paramref name="type"/></para>
    /// <para>Documentation: <see href="https://api.gandi.net/docs/livedns/#put-v5-livedns-domains-fqdn-records-rrset_name-rrset_type" /></para>
    /// </summary>
    /// <param name="domain">Domain name</param>
    /// <param name="name">Name of the record</param>
    /// <param name="type">One of: <c>A</c>, <c>AAAA</c>, <c>ALIAS</c>, <c>CAA</c>, <c>CDS</c>, <c>CNAME</c>, <c>DNAME</c>, <c>DS</c>, <c>KEY</c>, <c>LOC</c>, <c>MX</c>, <c>NAPTR</c>, <c>NS</c>, <c>OPENPGPKEY</c>, <c>PTR</c>, <c>RP</c>, <c>SPF</c>, <c>SRV</c>, <c>SSHFP</c>, <c>TLSA</c>, <c>TXT</c>, <c>WKS</c></param>
    /// <param name="values">A list of values for this record</param>
    /// <param name="ttl">Minimum: 300, maximum: 2592000. The time in seconds that DNS resolvers should cache this record.</param>
    /// <param name="cancellationToken">Allows the request can be canceled</param>
    /// <returns><c>true</c> if the record was created (otherwise, an <see cref="ApiException"/> is thrown, <c>false</c> is never returned)</returns>
    /// <exception cref="ApiException">if the request was unauthorized (401) or forbidden (403)</exception>
    Task<bool> PutDomainRecord(string domain, string name, string type, string[] values, int ttl, CancellationToken cancellationToken);

    /// <summary>
    /// <para>Delete record with <paramref name="name"/> and <paramref name="type"/></para>
    /// <para>Documentation: <see href="https://api.gandi.net/docs/livedns/#delete-v5-livedns-domains-fqdn-records-rrset_name-rrset_type" /></para>
    /// </summary>
    /// <param name="domain">Domain name</param>
    /// <param name="name">Name of the record</param>
    /// <param name="type">One of: <c>A</c>, <c>AAAA</c>, <c>ALIAS</c>, <c>CAA</c>, <c>CDS</c>, <c>CNAME</c>, <c>DNAME</c>, <c>DS</c>, <c>KEY</c>, <c>LOC</c>, <c>MX</c>, <c>NAPTR</c>, <c>NS</c>, <c>OPENPGPKEY</c>, <c>PTR</c>, <c>RP</c>, <c>SPF</c>, <c>SRV</c>, <c>SSHFP</c>, <c>TLSA</c>, <c>TXT</c>, <c>WKS</c></param>
    /// <param name="cancellationToken">Allows the request can be canceled</param>
    /// <returns><c>true</c> if the record was deleted or could not be found (otherwise, an <see cref="ApiException"/> is thrown, <c>false</c> is never returned)</returns>
    /// <exception cref="ApiException">if the request was unauthorized (401) or forbidden (403)</exception>
    Task<bool> DeleteDomainRecord(string domain, string name, string type, CancellationToken cancellationToken);

}