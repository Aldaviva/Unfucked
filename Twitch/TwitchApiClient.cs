using System.CodeDom.Compiler;
using System.Diagnostics.CodeAnalysis;
using Twitch.Net;
using Twitch.Net.Interfaces;

namespace Unfucked.Twitch;

/// <summary>
/// Wrapper class around <see cref="TwitchApi"/> that inherits the <see cref="ITwitchApi"/> interface, so that implementations can be substituted, especially when mocking in tests
/// </summary>
/// <param name="client">The underlying <see cref="TwitchApi"/> client to wrap and delegate to.</param>
[ExcludeFromCodeCoverage]
[GeneratedCode("TwitchApi.Net", "3.2.0")]
public class TwitchApiClient(TwitchApi client): ITwitchApi {

    /// <inheritdoc />
    public IClipActions Clips => client.Clips;

    /// <inheritdoc />
    public IGameActions Games => client.Games;

    /// <inheritdoc />
    public IStreamActions Streams => client.Streams;

    /// <inheritdoc />
    public IUserActions Users => client.Users;

    /// <inheritdoc />
    public IVideoActions Videos => client.Videos;

}