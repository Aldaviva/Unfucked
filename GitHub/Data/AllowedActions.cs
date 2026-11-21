using Octokit.Internal;

namespace Unfucked.GitHub.Data;

/// <summary>
/// High-level actions permissions.
/// </summary>
public enum AllowedActions {

    /// <summary>
    /// Any action or reusable workflow can be used, regardless of who authored it or where it is defined.
    /// </summary>
    [Parameter(Value = "all")]
    All,

    /// <summary>
    /// Any action or reusable workflow defined in a repository within the owner organization/user can be used.
    /// </summary>
    [Parameter(Value = "local_only")]
    LocalOnly,

    /// <summary>
    /// Any action or reusable workflow that matches the specified criteria, plus those defined in a repository within the owner organization/user, can be used.
    /// </summary>
    [Parameter(Value = "selected")]
    Selected

}