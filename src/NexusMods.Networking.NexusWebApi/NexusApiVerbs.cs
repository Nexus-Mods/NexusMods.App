using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Cli;
using NexusMods.Abstractions.NexusWebApi;
using NexusMods.Abstractions.NexusWebApi.Types;
using NexusMods.Abstractions.NexusWebApi.Types.V2;
using NexusMods.ProxyConsole.Abstractions;
using NexusMods.ProxyConsole.Abstractions.VerbDefinitions;

namespace NexusMods.Networking.NexusWebApi;

internal static class NexusApiVerbs
{
    internal static IServiceCollection AddNexusApiVerbs(this IServiceCollection collection) =>
        collection
            .AddModule("nexus", "Commands for interacting with the Nexus Mods API")
            .AddVerb(() => NexusApiVerify)
            .AddVerb(() => NexusDownloadLinks);


    [Verb("nexus verify", "Verifies the logged in account via the Nexus API")]
    private static async Task<int> NexusApiVerify([Injected] IRenderer renderer,
        [Injected] NexusApiClient nexusApiClient,
        [Injected] IAuthenticatingMessageFactory messageFactory,
        [Injected] CancellationToken token)
    {
        var userInfo = await messageFactory.Verify(nexusApiClient, token);
        
        await renderer.Table(["Name", "Premium"],
        [
            [
                userInfo?.Name ?? "<Not logged in>",
                    userInfo?.UserRole == UserRole.Premium,
            ],
        ]);

        return 0;
    }

    [Verb("nexus download-links", "Generates download links for a given file")]
    private static async Task<int> NexusDownloadLinks([Injected] IRenderer renderer,
        [Option("g", "gameDomain", "Game domain")] string gameDomain,
        [Option("m", "modId", "Mod ID")] ModId modId,
        [Option("f", "fileId", "File ID")] FileId fileId,
        [Injected] NexusApiClient nexusApiClient,
        [Injected] CancellationToken token)
    {
        var links = await nexusApiClient.DownloadLinksAsync(gameDomain, modId, fileId, token);

        await renderer.Table(["Source", "Link"],
            links.Data.Select(x => new object[] { x.ShortName, x.Uri }));
        return 0;
    }
}
