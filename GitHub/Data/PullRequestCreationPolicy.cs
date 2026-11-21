using Octokit.Internal;

namespace Unfucked.GitHub.Data;

/// <summary>Who is allowed to open pull requests in the repository.</summary>
public enum PullRequestCreationPolicy {

    /// <summary>All signed-in GitHub users who can see the repository are allowed to open pull requests in it.</summary>
    [Parameter(Value = "all")]
    All,

    /// <summary>Only GitHub users who are collaborators in the repository are allowed to open pull requests in it.</summary>
    [Parameter(Value = "collaborators_only")]
    CollaboratorsOnly

}