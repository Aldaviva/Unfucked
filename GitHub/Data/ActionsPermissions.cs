namespace Unfucked.GitHub.Data;

/// <summary>
/// Define whether actions are enabled, and coarse-grained ownership criteria of actions to allow.
/// </summary>
public sealed record ActionsPermissions {

    private ActionsPermissions() {}

    /// <inheritdoc cref="ActionsPermissions" />
    /// <param name="enabled">If actions are enabled at all.</param>
    /// <param name="allowedActions">High-level filtering for which actions are allowed.</param>
    /// <exception cref="ArgumentException"><paramref name="enabled"/> is <c>false</c> and <paramref name="allowedActions"/> is non-null.</exception>
    public ActionsPermissions(bool enabled, AllowedActions? allowedActions): this() {
        if (!enabled && allowedActions is not null) {
            throw new ArgumentException($"When {nameof(enabled)} is {enabled}, {nameof(allowedActions)} must be null", nameof(allowedActions));
        }

        Enabled        = enabled;
        AllowedActions = allowedActions;
    }

    /// <summary>
    /// If actions are enabled at all.
    /// </summary>
    public bool Enabled { get; init; }

    /// <summary>
    /// High-level filtering for which actions are allowed.
    /// </summary>
    public AllowedActions? AllowedActions { get; init; }

}