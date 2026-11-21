using System.Diagnostics;
using System.Globalization;

namespace Unfucked.GitHub.Data;

/// <inheritdoc cref="Octokit.ReleaseAsset" />
[DebuggerDisplay("{DebuggerDisplay,nq}")]
public class ReleaseAsset {

    /// <summary>SHA2-256 hash of the asset's content, in lowercase hexadecimal, with the <c>sha256:</c> prefix.</summary>
    /// <seealso cref="DigestBytes"/>
    public string? Digest { get; protected set; }

    /// <summary>SHA2-256 hash of the asset's content.</summary>
    /// <seealso cref="Digest"/>
    public byte[]? DigestBytes {
        get {
            if (Digest?.IndexOf(':') is {} delimiterPosition) {
#if NET5_0_OR_GREATER
                return Convert.FromHexString(Digest.AsSpan(delimiterPosition + 1));
#else
                byte[] result = new byte[(Digest.Length - delimiterPosition - 1) / 2];
                for (int i = 0; i < result.Length; i++) {
                    result[i] = byte.Parse(Digest.Substring(delimiterPosition + 1 + i * 2, 2));
                }
                return result;
#endif
            }
            return null;
        }
    }

    /*
     * Copying instead of subclassing or composing because reflection treats superclass private set fields as unwritable, so the JSON serializer refuses to deserialize superclass fields, and we must move them to the subclass here.
     */

    #region copied from upstream

    #pragma warning disable CS8618

    /// <inheritdoc cref="Octokit.ReleaseAsset()" />
    public ReleaseAsset() {}

    /// <inheritdoc cref="Octokit.ReleaseAsset(string,int,string,string,string,string,string,int,int,DateTimeOffset,DateTimeOffset,string,Author)" />
    public ReleaseAsset(string url, int id, string nodeId, string name, string label, string state, string contentType, int size, int downloadCount, DateTimeOffset createdAt, DateTimeOffset updatedAt,
                        string browserDownloadUrl, Author uploader): this() {
        Url                = url;
        Id                 = id;
        NodeId             = nodeId;
        Name               = name;
        Label              = label;
        State              = state;
        ContentType        = contentType;
        Size               = size;
        DownloadCount      = downloadCount;
        CreatedAt          = createdAt;
        UpdatedAt          = updatedAt;
        BrowserDownloadUrl = browserDownloadUrl;
        Uploader           = uploader;
    }

    /// <inheritdoc cref="Octokit.ReleaseAsset.Url" />
    public string Url { get; protected set; }

    /// <inheritdoc cref="Octokit.ReleaseAsset.Id" />
    public int Id { get; protected set; }

    /// <inheritdoc cref="Octokit.ReleaseAsset.NodeId" />
    public string NodeId { get; protected set; }

    /// <inheritdoc cref="Octokit.ReleaseAsset.Name" />
    public string Name { get; protected set; }

    /// <inheritdoc cref="Octokit.ReleaseAsset.Label" />
    public string Label { get; protected set; }

    /// <inheritdoc cref="Octokit.ReleaseAsset.State" />
    public string State { get; protected set; }

    /// <inheritdoc cref="Octokit.ReleaseAsset.ContentType" />
    public string ContentType { get; protected set; }

    /// <inheritdoc cref="Octokit.ReleaseAsset.Size" />
    public int Size { get; protected set; }

    /// <inheritdoc cref="Octokit.ReleaseAsset.DownloadCount" />
    public int DownloadCount { get; protected set; }

    /// <inheritdoc cref="Octokit.ReleaseAsset.CreatedAt" />
    public DateTimeOffset CreatedAt { get; protected set; }

    /// <inheritdoc cref="Octokit.ReleaseAsset.UpdatedAt" />
    public DateTimeOffset UpdatedAt { get; protected set; }

    /// <inheritdoc cref="Octokit.ReleaseAsset.BrowserDownloadUrl" />
    public string BrowserDownloadUrl { get; protected set; }

    /// <inheritdoc cref="Octokit.ReleaseAsset.Uploader" />
    public Author Uploader { get; protected set; }

    internal string DebuggerDisplay => string.Format(CultureInfo.InvariantCulture, "Name: {0} CreatedAt: {1}", Name, CreatedAt);

    /// <inheritdoc cref="Octokit.ReleaseAsset.ToUpdate" />
    public ReleaseAssetUpdate ToUpdate() => new(Name) { Label = Label };

    #pragma warning restore CS8618

    #endregion

}