using Microsoft.Extensions.DependencyInjection;
using NexusMods.Networking.NexusWebApi.Auth;
using NexusMods.ProxyConsole.Abstractions.VerbDefinitions;

namespace NexusMods.Networking.NexusWebApi;

internal static class LoginVerbs
{
    internal static IServiceCollection AddLoginVerbs(this IServiceCollection collection) =>
        collection.AddVerb(() => NexusLogin)
            .AddVerb(() => NexusLogout)
            .AddVerb(() => SetApiKey);

    [Verb("nexus-login", "Logs into the Nexus Mods API")]
    private static async Task<int> NexusLogin([Injected] LoginManager loginManager,
        [Injected] CancellationToken token)
    {
        await loginManager.LoginAsync(token);
        return 0;
    }

    [Verb("nexus-logout", "Logs out of the Nexus Mods API")]
    private static async Task<int> NexusLogout([Injected] LoginManager loginManager)
    {
        await loginManager.Logout();
        return 0;
    }

    [Verb("nexus-api-key", "Sets the key used in Nexus API calls")]
    private static async Task<int> SetApiKey([Injected] ApiKeyMessageFactory apiKeyMessageFactory,
    [Option("a", "apiey", "Api key to register")] string key)
    {
        await apiKeyMessageFactory.SetApiKey(key);
        return 0;
    }

}
