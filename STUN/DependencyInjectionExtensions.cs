using Microsoft.Extensions.DependencyInjection;

namespace Unfucked.STUN;

public static class DependencyInjectionExtensions {

    public static IServiceCollection AddStunClient(this IServiceCollection services, Func<IServiceProvider, StunOptions>? options = null) => services
        .AddTransient<IStunClient5389, MultiServerStunClient>()
        .AddSingleton<StunServerList>(ctx => new AlwaysOnlineStunServerList(ctx.GetRequiredService<HttpClient>(), options?.Invoke(ctx).ServerHostnameBlacklist))
        .AddSingleton<ISelfWanAddressClient>(ctx => new ThreadSafeMultiServerStunClient(ctx.GetRequiredService<IStunClient5389>))
        .AddSingleton<IStunClientFactory, StunClient5389Factory>();

}