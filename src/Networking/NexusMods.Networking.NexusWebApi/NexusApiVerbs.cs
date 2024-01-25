using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Cli;
using NexusMods.Abstractions.NexusWebApi;
using NexusMods.Abstractions.NexusWebApi.Types;
using NexusMods.ProxyConsole.Abstractions;
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
        [Injected] NexusApiClient nexusApiClient,
        [Injected] IAuthenticatingMessageFactory messageFactory,
        [Injected] CancellationToken token)
    {
        var userInfo = await messageFactory.Verify(nexusApiClient, token);
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
        [Injected] NexusApiClient nexusApiClient,
        [Injected] CancellationToken token)
    {
        var links = await nexusApiClient.DownloadLinksAsync(gameDomain, modId, fileId, token);

        await renderer.Table(new[] { "Source", "Link" },
            links.Data.Select(x => new object[] { x.ShortName, x.Uri }));
        return 0;
    }

    [Verb("nexus-games", "Lists all games available on Nexus Mods")]
    private static async Task<int> NexusGames([Injected] IRenderer renderer,
        [Injected] NexusApiClient nexusApiClient,
        [Injected] CancellationToken token)
    {
        var results = await nexusApiClient.Games(token);

        await renderer.Table(new[] { "Name", "Domain", "Downloads", "Files" },
            results.Data
                .OrderByDescending(x => x.FileCount)
                .Select(x => new object[] { x.Name, x.DomainName, x.Downloads, x.FileCount }));

        return 0;
    }

}
