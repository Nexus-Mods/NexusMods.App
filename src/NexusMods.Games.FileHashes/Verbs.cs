using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Cli;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Games.FileHashes;
using NexusMods.Abstractions.Games.FileHashes.Models;
using NexusMods.Games.FileHashes.VerbImpls;
using NexusMods.Paths;
using NexusMods.ProxyConsole.Abstractions;
using NexusMods.ProxyConsole.Abstractions.VerbDefinitions;

namespace NexusMods.Games.FileHashes;

public static class Verbs
{
    internal static IServiceCollection AddFileHashesVerbs(this IServiceCollection collection) =>
        collection
            .AddModule("game-hashes-db", "Verbs for interacting with and creating the game hashes database")
            .AddVerb(() => UpdateDb)
            .AddVerb(() => BuildDb);
    
    [Verb("game-hashes-db build", "Builds the game hashes database from the given github path")]
    private static async Task<int> BuildDb([Injected] IRenderer renderer,
        [Option("p", "path", "Path to the cloned GitHub hashes repo")] AbsolutePath path,
        [Option("o", "output", "Output path for the built database")] AbsolutePath output,
        [Injected] TemporaryFileManager temporaryFileManager,
        [Injected] IGameRegistry gameRegistry,
        [Injected] IServiceProvider serviceProvider,
        [Injected] CancellationToken token)
    {
        await using var builder = new BuildHashesDb(renderer, serviceProvider, temporaryFileManager, gameRegistry);
        
        await builder.BuildFrom(path, output);
        return 0;
    }
    
    [Verb("game-hashes-db update", "Checks for updates to the game hashes database")]
    private static async Task<int> UpdateDb([Injected] IRenderer renderer,
        [Injected] IFileHashesService fileHashesService,
        [Injected] CancellationToken token)
    {
        await fileHashesService.CheckForUpdate(true);

        var db = await fileHashesService.GetFileHashesDb();

        await new (string, int)[]
        {
            ("Hash Relations", HashRelation.All(db).Count),
            ("Path Hash Relations", PathHashRelation.All(db).Count),
            ("Gog Builds", GogBuild.All(db).Count),
            ("Steam Manifests", SteamManifest.All(db).Count),
        }.RenderTable(renderer, "Statistic", "Value");
        
        return 0;
    }
}
