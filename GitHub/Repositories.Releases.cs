using Release = Unfucked.GitHub.Data.Release;
using ReleaseAsset = Unfucked.GitHub.Data.ReleaseAsset;

namespace Unfucked.GitHub;

/// <inheritdoc />
public interface IReleasesClient: Octokit.IReleasesClient {

    /// <inheritdoc cref="Octokit.IReleasesClient.GetAll(string,string)"/>
    new Task<IReadOnlyList<Release>> GetAll(string owner, string name);

    /// <inheritdoc cref="Octokit.IReleasesClient.GetAll(long)"/>
    new Task<IReadOnlyList<Release>> GetAll(long repositoryId);

    /// <inheritdoc cref="Octokit.IReleasesClient.GetAll(string,string,Octokit.ApiOptions)"/>
    new Task<IReadOnlyList<Release>> GetAll(string owner, string name, ApiOptions options);

    /// <inheritdoc cref="Octokit.IReleasesClient.GetAll(long,ApiOptions)"/>
    new Task<IReadOnlyList<Release>> GetAll(long repositoryId, ApiOptions options);

    /// <inheritdoc cref="Octokit.IReleasesClient.Get(string,string,long)"/>
    new Task<Release> Get(string owner, string name, long releaseId);

    /// <inheritdoc cref="Octokit.IReleasesClient.Get(string,string,string)"/>
    new Task<Release> Get(string owner, string name, string tag);

    /// <inheritdoc cref="Octokit.IReleasesClient.Get(long,long)"/>
    new Task<Release> Get(long repositoryId, long releaseId);

    /// <inheritdoc cref="Octokit.IReleasesClient.Get(long,string)"/>
    new Task<Release> Get(long repositoryId, string tag);

    /// <inheritdoc cref="Octokit.IReleasesClient.GetLatest(string,string)"/>
    new Task<Release> GetLatest(string owner, string name);

    /// <inheritdoc cref="Octokit.IReleasesClient.GetLatest(long)"/>
    new Task<Release> GetLatest(long repositoryId);

    /// <inheritdoc cref="Octokit.IReleasesClient.Create(string,string,NewRelease)"/>
    new Task<Release> Create(string owner, string name, NewRelease data);

    /// <inheritdoc cref="Octokit.IReleasesClient.Create(long,NewRelease)"/>
    new Task<Release> Create(long repositoryId, NewRelease data);

    /// <inheritdoc cref="Octokit.IReleasesClient.Edit(string,string,long,ReleaseUpdate)"/>
    new Task<Release> Edit(string owner, string name, long releaseId, ReleaseUpdate data);

    /// <inheritdoc cref="Octokit.IReleasesClient.Edit(long,long,ReleaseUpdate)"/>
    new Task<Release> Edit(long repositoryId, long releaseId, ReleaseUpdate data);

    /// <inheritdoc cref="Octokit.IReleasesClient.GetAllAssets(string,string,long)"/>
    new Task<IReadOnlyList<ReleaseAsset>> GetAllAssets(string owner, string name, long releaseId);

    /// <inheritdoc cref="Octokit.IReleasesClient.GetAllAssets(long,long)"/>
    new Task<IReadOnlyList<ReleaseAsset>> GetAllAssets(long repositoryId, long releaseId);

    /// <inheritdoc cref="Octokit.IReleasesClient.GetAllAssets(string,string,long,ApiOptions)"/>
    new Task<IReadOnlyList<ReleaseAsset>> GetAllAssets(string owner, string name, long releaseId, ApiOptions options);

    /// <inheritdoc cref="Octokit.IReleasesClient.GetAllAssets(long,long,ApiOptions)"/>
    new Task<IReadOnlyList<ReleaseAsset>> GetAllAssets(long repositoryId, long releaseId, ApiOptions options);

    /// <inheritdoc cref="Octokit.IReleasesClient.UploadAsset(Octokit.Release,ReleaseAssetUpload,CancellationToken)"/>
    new Task<ReleaseAsset> UploadAsset(Octokit.Release release, ReleaseAssetUpload data, CancellationToken cancellationToken = default);

    /// <inheritdoc cref="Octokit.IReleasesClient.GetAsset(string,string,int)"/>
    new Task<ReleaseAsset> GetAsset(string owner, string name, int assetId);

    /// <inheritdoc cref="Octokit.IReleasesClient.GetAsset(long,int)"/>
    new Task<ReleaseAsset> GetAsset(long repositoryId, int assetId);

    /// <inheritdoc cref="Octokit.IReleasesClient.EditAsset(string,string,int,ReleaseAssetUpdate)"/>
    new Task<ReleaseAsset> EditAsset(string owner, string name, int assetId, ReleaseAssetUpdate data);

    /// <inheritdoc cref="Octokit.IReleasesClient.EditAsset(long,int,ReleaseAssetUpdate)"/>
    new Task<ReleaseAsset> EditAsset(long repositoryId, int assetId, ReleaseAssetUpdate data);

}

/// <inheritdoc cref="Octokit.ReleasesClient" />
public class ReleasesClient(IApiConnection apiConnection): Octokit.ReleasesClient(apiConnection), IReleasesClient {

    /// <inheritdoc />
    public new Task<IReadOnlyList<Release>> GetAll(string owner, string name) =>
        GetAll(owner, name, ApiOptions.None);

    /// <inheritdoc />
    public new Task<IReadOnlyList<Release>> GetAll(long repositoryId) =>
        GetAll(repositoryId, ApiOptions.None);

    /// <inheritdoc />
    public new Task<IReadOnlyList<Release>> GetAll(string owner, string name, ApiOptions options) =>
        ApiConnection.GetAll<Release>(ApiUrls.Releases(owner, name), null, AcceptHeaders.StableVersion, options);

    /// <inheritdoc />
    public new Task<IReadOnlyList<Release>> GetAll(long repositoryId, ApiOptions options) =>
        ApiConnection.GetAll<Release>(ApiUrls.Releases(repositoryId), null, AcceptHeaders.StableVersion, options);

    /// <inheritdoc />
    public new Task<Release> Get(string owner, string name, long releaseId) =>
        ApiConnection.Get<Release>(ApiUrls.Releases(owner, name, releaseId));

    /// <inheritdoc />
    public new Task<Release> Get(string owner, string name, string tag) =>
        ApiConnection.Get<Release>(ApiUrls.Releases(owner, name, tag));

    /// <inheritdoc />
    public new Task<Release> Get(long repositoryId, long releaseId) =>
        ApiConnection.Get<Release>(ApiUrls.Releases(repositoryId, releaseId));

    /// <inheritdoc />
    public new Task<Release> Get(long repositoryId, string tag) =>
        ApiConnection.Get<Release>(ApiUrls.Releases(repositoryId, tag));

    /// <inheritdoc />
    public new Task<Release> GetLatest(string owner, string name) =>
        ApiConnection.Get<Release>(ApiUrls.LatestRelease(owner, name));

    /// <inheritdoc />
    public new Task<Release> GetLatest(long repositoryId) =>
        ApiConnection.Get<Release>(ApiUrls.LatestRelease(repositoryId));

    /// <inheritdoc />
    public new Task<Release> Create(string owner, string name, NewRelease data) =>
        ApiConnection.Post<Release>(ApiUrls.Releases(owner, name), data, AcceptHeaders.StableVersion);

    /// <inheritdoc />
    public new Task<Release> Create(long repositoryId, NewRelease data) =>
        ApiConnection.Post<Release>(ApiUrls.Releases(repositoryId), data, AcceptHeaders.StableVersion);

    /// <inheritdoc />
    public new Task<Release> Edit(string owner, string name, long releaseId, ReleaseUpdate data) =>
        ApiConnection.Patch<Release>(ApiUrls.Releases(owner, name, releaseId), data);

    /// <inheritdoc />
    public new Task<Release> Edit(long repositoryId, long releaseId, ReleaseUpdate data) =>
        ApiConnection.Patch<Release>(ApiUrls.Releases(repositoryId, releaseId), data);

    /// <inheritdoc />
    public new Task<IReadOnlyList<ReleaseAsset>> GetAllAssets(string owner, string name, long releaseId) =>
        GetAllAssets(owner, name, releaseId, ApiOptions.None);

    /// <inheritdoc />
    public new Task<IReadOnlyList<ReleaseAsset>> GetAllAssets(long repositoryId, long releaseId) =>
        GetAllAssets(repositoryId, releaseId, ApiOptions.None);

    /// <inheritdoc />
    public new Task<IReadOnlyList<ReleaseAsset>> GetAllAssets(string owner, string name, long releaseId, ApiOptions options) =>
        ApiConnection.GetAll<ReleaseAsset>(ApiUrls.ReleaseAssets(owner, name, releaseId), null, AcceptHeaders.StableVersion, options);

    /// <inheritdoc />
    public new Task<IReadOnlyList<ReleaseAsset>> GetAllAssets(long repositoryId, long releaseId, ApiOptions options) =>
        ApiConnection.GetAll<ReleaseAsset>(ApiUrls.ReleaseAssets(repositoryId, releaseId), null, AcceptHeaders.StableVersion, options);

    /// <inheritdoc />
    public new Task<ReleaseAsset> UploadAsset(Octokit.Release release, ReleaseAssetUpload data, CancellationToken cancellationToken = default) {
        var endpoint = release.UploadUrl.ExpandUriTemplate(new { name = data.FileName });

        return data.Timeout.HasValue
            ? ApiConnection.Post<ReleaseAsset>(endpoint, data.RawData, AcceptHeaders.StableVersion, data.ContentType, data.Timeout.GetValueOrDefault(), cancellationToken)
            : ApiConnection.Post<ReleaseAsset>(endpoint, data.RawData, AcceptHeaders.StableVersion, data.ContentType, cancellationToken);
    }

    /// <inheritdoc />
    public new Task<ReleaseAsset> GetAsset(string owner, string name, int assetId) =>
        ApiConnection.Get<ReleaseAsset>(ApiUrls.Asset(owner, name, assetId));

    /// <inheritdoc />
    public new Task<ReleaseAsset> GetAsset(long repositoryId, int assetId) =>
        ApiConnection.Get<ReleaseAsset>(ApiUrls.Asset(repositoryId, assetId));

    /// <inheritdoc />
    public new Task<ReleaseAsset> EditAsset(string owner, string name, int assetId, ReleaseAssetUpdate data) =>
        ApiConnection.Patch<ReleaseAsset>(ApiUrls.Asset(owner, name, assetId), data);

    /// <inheritdoc />
    public new Task<ReleaseAsset> EditAsset(long repositoryId, int assetId, ReleaseAssetUpdate data) =>
        ApiConnection.Patch<ReleaseAsset>(ApiUrls.Asset(repositoryId, assetId), data);

}