namespace Unfucked.GitHub;

/// <inheritdoc cref="IActionsClient" />
public interface UnfuckedActionsClient: IActionsClient {

    /// <inheritdoc cref="IActionsClient.Permissions" />
    new UnfuckedActionsPermissionsClient Permissions { get; }

}

/// <inheritdoc cref="ActionsClient" />
public class UnfuckedActionsClientImpl: ActionsClient, UnfuckedActionsClient {

    /// <inheritdoc cref="ActionsClient(IApiConnection)" />
    public UnfuckedActionsClientImpl(IApiConnection apiConnection): base(apiConnection) {
        Permissions = new UnfuckedActionsPermissionsClientImpl(apiConnection);
    }

    /// <inheritdoc />
    public new UnfuckedActionsPermissionsClient Permissions { get; }

}