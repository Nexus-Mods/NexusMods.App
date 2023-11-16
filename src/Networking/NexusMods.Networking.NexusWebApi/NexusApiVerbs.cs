using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.CLI;
using NexusMods.Networking.NexusWebApi.Types;
using NexusMods.ProxyConsole.Abstractions;
using NexusMods.ProxyConsole.Abstractions.Implementations;
using NexusMods.ProxyConsole.Abstractions.VerbDefinitions;

namespace NexusMods.Networking.NexusWebApi;

internal static class NexusApiVerbs
{
    internal static IServiceCollection AddNexusApiVerbs(this IServiceCollection collection) =>
        collection.AddVerb(() => NexusApiVerify)
            .AddVerb(() => NexusDownloadLinks)
            .AddVerb(() => NexusGames);


    [Verb("nexus-api-verify", "Verifies the logged in account via the Nexus API")]
    private static async Task<int> NexusApiVerify([Injected] IRenderer renderer,
        [Injected] Client client,
        [Injected] IAuthenticatingMessageFactory messageFactory,
        [Injected] CancellationToken token)
    {
        var userInfo = await messageFactory.Verify(client, token);
        await renderer.Table(new[] { "Name", "Premium" },
            new[]
            {
                new object[]
                {
                    userInfo?.Name ?? "<Not logged in>",
                    userInfo?.IsPremium ?? false,
                }
            });

        return 0;
    }

    [Verb("nexus-download-links", "Generates download links for a given file")]
    private static async Task<int> NexusDownloadLinks([Injected] IRenderer renderer,
        [Option("g", "gameDomain", "Game domain")] string gameDomain,
        [Option("m", "modId", "Mod ID")] ModId modId,
        [Option("f", "fileId", "File ID")] FileId fileId,
        [Injected] Client client,
        [Injected] CancellationToken token)
    {
        var links = await client.DownloadLinksAsync(gameDomain, modId, fileId, token);

        await renderer.Table(new[] { "Source", "Link" },
            links.Data.Select(x => new object[] { x.ShortName, x.Uri }));
        return 0;
    }

    [Verb("nexus-games", "Lists all games available on Nexus Mods")]
    private static async Task<int> NexusGames([Injected] IRenderer renderer,
        [Injected] Client client,
        [Injected] CancellationToken token)
    {
        var results = await client.Games(token);

        await renderer.Table(new[] { "Name", "Domain", "Downloads", "Files" },
            results.Data
                .OrderByDescending(x => x.FileCount)
                .Select(x => new object[] { x.Name, x.DomainName, x.Downloads, x.FileCount }));

        return 0;
    }

}
