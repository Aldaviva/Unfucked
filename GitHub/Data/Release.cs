using System.Diagnostics;
using System.Globalization;

namespace Unfucked.GitHub.Data;

/// <inheritdoc cref="Octokit.Release" />
[DebuggerDisplay("{DebuggerDisplay,nq}")]
public class Release {

    /// <inheritdoc cref="Octokit.Release.Assets" />
    public IReadOnlyList<ReleaseAsset> Assets { get; protected set; } = null!;

    /*
     * Copying instead of subclassing or composing because reflection treats superclass private set fields as unwritable, so the JSON serializer refuses to deserialize superclass fields, and we must move them to the subclass here.
     */

    #region copied from upstream

    #pragma warning disable CS8618

    /// <inheritdoc cref="Octokit.Release()" />
    public Release() {}

    /// <inheritdoc cref="Octokit.Release(string,string,string,string,long,string,string,string,string,string,bool,bool,DateTimeOffset,DateTimeOffset?,Octokit.Author,string,string,IReadOnlyList{Octokit.ReleaseAsset})" />
    public Release(string url, string htmlUrl, string assetsUrl, string uploadUrl, long id, string nodeId, string tagName, string targetCommitish, string name, string body, bool draft,
                   bool prerelease, DateTimeOffset createdAt, DateTimeOffset? publishedAt, Author author, string tarballUrl, string zipballUrl, IReadOnlyList<ReleaseAsset> assets): this() {
        Url             = url;
        HtmlUrl         = htmlUrl;
        AssetsUrl       = assetsUrl;
        UploadUrl       = uploadUrl;
        Id              = id;
        NodeId          = nodeId;
        TagName         = tagName;
        TargetCommitish = targetCommitish;
        Name            = name;
        Body            = body;
        Draft           = draft;
        Prerelease      = prerelease;
        CreatedAt       = createdAt;
        PublishedAt     = publishedAt;
        Author          = author;
        TarballUrl      = tarballUrl;
        ZipballUrl      = zipballUrl;
        Assets          = assets;
    }

    /// <inheritdoc cref="Octokit.Release(string)" />
    public Release(string uploadUrl): this() {
        UploadUrl = uploadUrl;
    }

    /// <inheritdoc cref="Octokit.Release.Url" />
    public string Url { get; protected set; }

    /// <inheritdoc cref="Octokit.Release.HtmlUrl" />
    public string HtmlUrl { get; protected set; }

    /// <inheritdoc cref="Octokit.Release.AssetsUrl" />
    public string AssetsUrl { get; protected set; }

    /// <inheritdoc cref="Octokit.Release.UploadUrl" />
    public string UploadUrl { get; protected set; }

    /// <inheritdoc cref="Octokit.Release.Id" />
    public long Id { get; protected set; }

    /// <inheritdoc cref="Octokit.Release.NodeId" />
    public string NodeId { get; protected set; }

    /// <inheritdoc cref="Octokit.Release.TagName" />
    public string TagName { get; protected set; }

    /// <inheritdoc cref="Octokit.Release.TargetCommitish" />
    public string TargetCommitish { get; protected set; }

    /// <inheritdoc cref="Octokit.Release.Name" />
    public string Name { get; protected set; }

    /// <inheritdoc cref="Octokit.Release.Body" />
    public string Body { get; protected set; }

    /// <inheritdoc cref="Octokit.Release.Draft" />
    public bool Draft { get; protected set; }

    /// <inheritdoc cref="Octokit.Release.Prerelease" />
    public bool Prerelease { get; protected set; }

    /// <inheritdoc cref="Octokit.Release.CreatedAt" />
    public DateTimeOffset CreatedAt { get; protected set; }

    /// <inheritdoc cref="Octokit.Release.PublishedAt" />
    public DateTimeOffset? PublishedAt { get; protected set; }

    /// <inheritdoc cref="Octokit.Release.Author" />
    public Author Author { get; protected set; }

    /// <inheritdoc cref="Octokit.Release.TarballUrl" />
    public string TarballUrl { get; protected set; }

    /// <inheritdoc cref="Octokit.Release.ZipballUrl" />
    public string ZipballUrl { get; protected set; }

    internal string DebuggerDisplay => string.Format(CultureInfo.InvariantCulture, "Name: {0} PublishedAt: {1}", Name, PublishedAt);

    /// <inheritdoc cref="Octokit.Release.ToUpdate" />
    public ReleaseUpdate ToUpdate() {
        return new ReleaseUpdate {
            Body            = Body,
            Draft           = Draft,
            Name            = Name,
            Prerelease      = Prerelease,
            TargetCommitish = TargetCommitish,
            TagName         = TagName
        };
    }

    #pragma warning restore CS8618

    #endregion

}