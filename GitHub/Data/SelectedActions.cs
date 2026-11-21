namespace Unfucked.GitHub.Data;

/// <summary>
/// Fine-grained permissions for which actions can be run on a repository.
/// </summary>
/// <param name="GitHubOwnedAllowed">Allow actions created by GitHub.</param>
/// <param name="VerifiedAllowed">Allow actions by Marketplace <see href="https://github.com/marketplace?type=actions&amp;verification=verified_creator">verified creators</see>.</param>
/// <param name="PatternsAllowed">
/// <para>Allow or block specified actions and reusable workflows.</para>
/// <para>Wildcards, tags, and SHAs are allowed. Use <c>!</c> prefix to block.</para>
/// <para>Action examples: <c>octo-org/octo-repo@*</c>, <c>!octo-org/octo-repo@v2</c></para>
/// <para>Reusable workflow example: <c>octo-org/octo-repo/.github/workflows/build.yml@main</c></para>
/// <para>Entire organization or repository examples: <c>octo-org/*</c>, <c>octo-org/octo-repo/*</c></para>
/// </param>
public sealed record SelectedActions(bool GitHubOwnedAllowed, bool VerifiedAllowed, List<string> PatternsAllowed) {

    /// <summary>Create a new instance that disallows all actions</summary>
    public SelectedActions(): this(false, false, []) {}

}