namespace Unfucked.GitHub;

/// <inheritdoc />
public interface IRepositoriesClient: Octokit.IRepositoriesClient {

    /// <inheritdoc cref="Octokit.IRepositoriesClient.Release" />
    new IReleasesClient Release { get; }

}

/// <inheritdoc cref="Octokit.RepositoriesClient" />
public class RepositoriesClient: Octokit.RepositoriesClient, IRepositoriesClient {

    /// <inheritdoc cref="Octokit.RepositoriesClient(Octokit.IApiConnection)" />
    public RepositoriesClient(IApiConnection apiConnection): base(apiConnection) {
        Release = new ReleasesClient(ApiConnection);
    }

    /// <inheritdoc />
    public new IReleasesClient Release { get; }

}