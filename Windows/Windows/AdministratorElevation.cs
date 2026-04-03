namespace Unfucked.Windows;

/// <summary>
/// The elevation state of a Windows process.
/// </summary>
public enum AdministratorElevation {

    /// <summary>
    /// The process is not elevated and the user is not in an administrators group, so they cannot elevate without another user's help.
    /// </summary>
    NOT_ADMIN,

    /// <summary>
    /// The process is not elevated but the user is a member of an administrators group, so they could elevate if they wanted to by being shown a UAC prompt.
    /// </summary>
    UNELEVATED_ADMIN,

    /// <summary>
    /// The process is elevated
    /// </summary>
    ELEVATED_ADMIN

}