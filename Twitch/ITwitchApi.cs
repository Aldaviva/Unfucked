using Twitch.Net;
using Twitch.Net.Interfaces;

namespace Unfucked.Twitch;

/// <summary>
/// Interface for the <see cref="TwitchApi"/> class to allow implementation substitution, especially when mocking in tests.
/// </summary>
public interface ITwitchApi {

    /// <summary>
    /// Clips lets Twitch viewers share interesting moments from broadcasts while letting broadcasters grow their channels through social sharing!
    /// </summary>
    IClipActions Clips { get; }

    /// <summary>
    /// Gets information about specified categories or games.
    /// </summary>
    IGameActions Games { get; }

    /// <summary>
    /// Gets a list of all streams. The list is in descending order by the number of viewers watching the stream. Because viewers come and go during a stream, it’s possible to find duplicate or missing streams in the list as you page through the results.
    /// </summary>
    IStreamActions Streams { get; }

    /// <summary>
    /// Gets information about one or more users.
    /// </summary>
    IUserActions Users { get; }

    /// <summary>
    /// Gets information about one or more published videos. You may get videos by ID, by user, or by game/category.
    /// </summary>
    IVideoActions Videos { get; }

}