using Microsoft.Extensions.DependencyInjection;

namespace NexusMods.Discord;

/// <summary>
/// Functionality related to Dependency Injection.
/// </summary>
public static class Services
{
    /// <summary>
    /// Adds file extraction related services to the provided DI container.
    /// </summary>
    public static IServiceCollection AddDiscordRPC(this IServiceCollection coll)
    {
        coll.AddSingleton<DiscordRpcService>();
        coll.AddHostedService<DiscordRpcService>();
        return coll;
    }
}
