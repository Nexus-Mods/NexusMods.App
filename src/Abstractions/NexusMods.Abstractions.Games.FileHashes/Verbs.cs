using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Games.FileHashes.VerbImpls;
using NexusMods.Paths;
using NexusMods.ProxyConsole.Abstractions;
using NexusMods.ProxyConsole.Abstractions.VerbDefinitions;

namespace NexusMods.Abstractions.Games.FileHashes;

public static class Verbs
{
    internal static IServiceCollection AddFileHashesVerbs(this IServiceCollection collection) =>
        collection
            .AddModule("game-hashes-db", "Verbs for interacting with and creating the game hashes database")    
            .AddVerb(() => InstallCollection);
    
    [Verb("game-hashes-db build", "Builds the game hashes database from the given github path")]
    private static async Task<int> InstallCollection([Injected] IRenderer renderer,
        [Option("p", "path", "Path to the cloned GitHub hashes repo")] AbsolutePath path,
        [Option("o", "output", "Output path for the built database")] AbsolutePath output,
        [Injected] TemporaryFileManager temporaryFileManager,
        [Injected] IServiceProvider serviceProvider,
        [Injected] CancellationToken token)
    {
        await using var builder = new Build(renderer, serviceProvider, temporaryFileManager);


        await builder.BuildFrom(path);
        
        
        return 0;
    }
}
