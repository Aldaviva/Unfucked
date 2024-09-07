using Twitch.Net.Interfaces;

namespace Unfucked.Twitch;

public interface ITwitchApi {

    IClipActions Clips { get; }
    IGameActions Games { get; }
    IStreamActions Streams { get; }
    IUserActions Users { get; }
    IVideoActions Videos { get; }

}