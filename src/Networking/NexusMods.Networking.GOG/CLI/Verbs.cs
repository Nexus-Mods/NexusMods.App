using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.GOG.Values;
using NexusMods.Abstractions.OAuth;
using NexusMods.ProxyConsole.Abstractions.VerbDefinitions;

namespace NexusMods.Networking.GOG.CLI;

public static class Verbs
{
    internal static IServiceCollection AddGOGVerbs(this IServiceCollection collection) =>
        collection
            .AddVerb(() => Login)
            .AddVerb(() => GetBuilds);

    [Verb("gog", "Indexes a Steam app and updates the given output folder")]
    private static async Task<int> Login([Injected] Client client)
    {
        await client.Login(CancellationToken.None);
        return 0;
    }
    
    [Verb("gog-builds", "Gets the product info of a GOG product")]
    private static async Task<int> GetBuilds(
        [Injected] Client client, 
        [Option("p", "productId", "The GOG product ID to get the product info of")] ProductId productId,
        [Injected] CancellationToken token)
    {
        await client.GetBuilds(productId, OS.windows, token);
        return 0;
    }
}
