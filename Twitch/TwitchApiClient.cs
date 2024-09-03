using System.CodeDom.Compiler;
using System.Diagnostics.CodeAnalysis;
using Twitch.Net;
using Twitch.Net.Interfaces;

namespace Unfucked;

[ExcludeFromCodeCoverage]
[GeneratedCode("TwitchApi.Net", "3.2.0")]
public class TwitchApiClient(TwitchApi client): ITwitchApi {

    public IClipActions Clips => client.Clips;
    public IGameActions Games => client.Games;
    public IStreamActions Streams => client.Streams;
    public IUserActions Users => client.Users;
    public IVideoActions Videos => client.Videos;

}