using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.OAuth;
using NexusMods.ProxyConsole.Abstractions.VerbDefinitions;

namespace NexusMods.Networking.GOG.CLI;

public static class Verbs
{
    internal static IServiceCollection AddGOGVerbs(this IServiceCollection collection) =>
        collection
            .AddVerb(() => Login);

    [Verb("gog", "Indexes a Steam app and updates the given output folder")]
    private static async Task<int> Login([Injected] Client client)
    {
        await client.Login(CancellationToken.None);
        return 0;
    }
}
