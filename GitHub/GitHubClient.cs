namespace Unfucked.GitHub;

/// <inheritdoc />
public interface IGitHubClient: Octokit.IGitHubClient {

    /// <inheritdoc cref="Octokit.GitHubClient.Actions" />
    public new UnfuckedActionsClient Actions { get; }

    /// <inheritdoc cref="Octokit.GitHubClient.Repository" />
    public new IRepositoriesClient Repository { get; }

}

/// <inheritdoc cref="Octokit.GitHubClient" />
public class GitHubClient: Octokit.GitHubClient, IGitHubClient {

    /// <inheritdoc />
    public GitHubClient(ProductHeaderValue productInformation): base(productInformation) => Initialize();

    /// <inheritdoc />
    public GitHubClient(ProductHeaderValue productInformation, ICredentialStore credentialStore): base(productInformation, credentialStore) => Initialize();

    /// <inheritdoc />
    public GitHubClient(ProductHeaderValue productInformation, Uri baseAddress): base(productInformation, baseAddress) => Initialize();

    /// <inheritdoc />
    public GitHubClient(ProductHeaderValue productInformation, ICredentialStore credentialStore, Uri baseAddress): base(productInformation, credentialStore, baseAddress) => Initialize();

    /// <inheritdoc />
    public GitHubClient(IConnection connection): base(connection) => Initialize();

    private void Initialize() {
        ApiConnection apiConnection = new(Connection);
        Actions    = new UnfuckedActionsClientImpl(apiConnection);
        Repository = new RepositoriesClient(apiConnection);

        // TODO check if these are necessary
        typeof(Octokit.GitHubClient).GetProperty(nameof(Actions))!.SetValue(this, Actions);
        typeof(Octokit.GitHubClient).GetProperty(nameof(Repository))!.SetValue(this, Repository);
    }

    /// <inheritdoc />
    public new UnfuckedActionsClient Actions { get; private set; } = null!;

    /// <inheritdoc />
    public new IRepositoriesClient Repository { get; private set; } = null!;

}