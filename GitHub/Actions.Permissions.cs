namespace Unfucked.GitHub;

/// <summary>Like <see cref="IActionsPermissionsClient"/>, but actually implemented instead of empty.</summary>
public interface UnfuckedActionsPermissionsClient: IActionsPermissionsClient {

    /// <summary>
    /// Gets the GitHub Actions permissions policy for a repository, including whether GitHub Actions is enabled and the actions and reusable workflows allowed to run in the repository.
    /// </summary>
    /// <remarks>https://docs.github.com/en/rest/actions/permissions?apiVersion=2022-11-28#get-github-actions-permissions-for-a-repository</remarks>
    /// <param name="owner">The owner of the repository.</param>
    /// <param name="name">The name of the repository.</param>
    Task<ActionsPermissions> Get(string owner, string name);

    /// <summary>
    /// Sets the GitHub Actions permissions policy for enabling GitHub Actions and allowed actions and reusable workflows in the repository.
    /// </summary>
    /// <remarks>https://docs.github.com/en/rest/actions/permissions?apiVersion=2022-11-28#set-github-actions-permissions-for-a-repository</remarks>
    /// <param name="owner">The owner of the repository.</param>
    /// <param name="name">The name of the repository.</param>
    /// <param name="actionsPermissions">The new permissions to set on the repository.</param>
    Task Set(string owner, string name, ActionsPermissions actionsPermissions);

    /// <summary>
    /// Gets the settings for selected actions and reusable workflows that are allowed in a repository. To use this endpoint, the repository policy for <see cref="ActionsPermissions.AllowedActions"/> must be configured to <see cref="AllowedActions.Selected"/>. For more information, see <see href="https://docs.github.com/en/rest/actions/permissions?apiVersion=2022-11-28#set-github-actions-permissions-for-a-repository">Set GitHub Actions permissions for a repository</see>.
    /// </summary>
    /// <remarks>https://docs.github.com/en/rest/actions/permissions?apiVersion=2022-11-28#get-allowed-actions-and-reusable-workflows-for-a-repository</remarks>
    /// <param name="owner">The owner of the repository.</param>
    /// <param name="name">The name of the repository.</param>
    Task<SelectedActions> GetSelectedActions(string owner, string name);

    /// <summary>
    /// Sets the actions and reusable workflows that are allowed in a repository. To use this endpoint, the repository policy for <see cref="ActionsPermissions.AllowedActions"/> must be configured to <see cref="AllowedActions.Selected"/>. For more information, see <see href="https://docs.github.com/en/rest/actions/permissions?apiVersion=2022-11-28#set-github-actions-permissions-for-a-repository">Set GitHub Actions permissions for a repository</see>.
    /// </summary>
    /// <remarks>https://docs.github.com/en/rest/actions/permissions?apiVersion=2022-11-28#set-allowed-actions-and-reusable-workflows-for-a-repository</remarks>
    /// <param name="owner">The owner of the repository.</param>
    /// <param name="name">The name of the repository.</param>
    /// <param name="selectedActions">The new list of selected actions to set on the repository.</param>
    Task SetSelectedActions(string owner, string name, SelectedActions selectedActions);

}

/// <summary>Like <see cref="ActionsPermissionsClient"/>, but actually implemented instead of empty.</summary>
public class UnfuckedActionsPermissionsClientImpl: ActionsPermissionsClient, UnfuckedActionsPermissionsClient {

    /// <inheritdoc cref="ActionsPermissionsClient(IApiConnection)" />
    public UnfuckedActionsPermissionsClientImpl(IApiConnection apiConnection): base(apiConnection) {}

    /// <inheritdoc />
    public Task<ActionsPermissions> Get(string owner, string name) =>
        ApiConnection.Get<ActionsPermissions>(ApiUrls.ActionsPermissions(owner, name));

    /// <inheritdoc />
    public Task Set(string owner, string name, ActionsPermissions actionsPermissions) =>
        ApiConnection.Put(ApiUrls.ActionsPermissions(owner, name), actionsPermissions);

    /// <inheritdoc />
    public Task<SelectedActions> GetSelectedActions(string owner, string name) =>
        ApiConnection.Get<SelectedActions>(ApiUrls.ActionsPermissionsSelectedActions(owner, name));

    /// <inheritdoc />
    public Task SetSelectedActions(string owner, string name, SelectedActions selectedActions) =>
        ApiConnection.Put(ApiUrls.ActionsPermissionsSelectedActions(owner, name), selectedActions);

}