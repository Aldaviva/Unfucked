namespace Unfucked.GitHub.Data;

// The latest Octokit version (14) is 2 years old and still does not include this property, and the only people that updated that library all left Microsoft by 2026-04.
/// <inheritdoc cref="Octokit.RepositoryUpdate" />
public class RepositoryUpdate: Octokit.RepositoryUpdate {

    /// <inheritdoc cref="PullRequestCreationPolicy" />
    public PullRequestCreationPolicy? PullRequestCreationPolicy { get; set; }

}