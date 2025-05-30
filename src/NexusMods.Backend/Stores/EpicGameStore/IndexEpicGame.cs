using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Cli;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.GameLocators.Stores.EGS;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.NexusWebApi.Types.V2;
using NexusMods.Paths;
using NexusMods.Paths.Extensions;
using NexusMods.Sdk.Hashes;
using NexusMods.Sdk.ProxyConsole;

namespace NexusMods.Backend.Stores.EpicGameStore;

public static class IndexEpicGame
{
    internal static IServiceCollection AddEGSVerbs(this IServiceCollection collection) =>
        collection
            .AddModule("egs", "Verbs for interacting with Epic Game Store games")
            .AddModule("egs app", "Verbs for querying Epic Game Store app data")
            .AddVerb(() => Index)
            .AddVerb(() => HashGameFiles);

    [Verb("egs app hash", "Hashes the files of the game currently installed and writes the hash variants to the given output folder")]
    private static async Task<int> HashGameFiles(
        [Injected] IRenderer renderer,
        [Option("o", "output", "Path to the cloned GitHub hashes repo (with the `json` postfix)")] AbsolutePath output,
        [Option("g", "game", "Game to index")] ILocatableGame game,
        [Injected] JsonSerializerOptions jsonSerializerOptions,
        [Injected] TemporaryFileManager temporaryFileManager,
        [Injected] IGameRegistry gameRegistry,
        [Injected] IServiceProvider serviceProvider,
        [Injected] EgDataClient egDataClient,
        [Injected] CancellationToken token)
    {
        var indentedOptions = new JsonSerializerOptions(jsonSerializerOptions)
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.Never,
        };
        
        var castedGame = (IGame)game;

        var installation = gameRegistry.Installations
            .Where(g => g.Value.Game.GameId == game.GameId)
            .FirstOrDefault(g => g.Value.Game is IEpicGame); 
        
        if (installation.Value == null)
        {
            await renderer.TextLine($"Game {castedGame.Name} is not installed via Epic Game Store.", token);
            return 1;
        }
        
        var location = installation.Value.LocationsRegister[LocationId.Game];

        
        await renderer.TextLine($"Indexing {castedGame.Name} at ({location})", token);

        var hashes = new ConcurrentDictionary<RelativePath, MultiHash>();

        await using (var _ = await renderer.WithProgress())
        {
            await Parallel.ForEachAsync(location.EnumerateFiles(), token, async (file, token) =>
                {
                    var relPath = file.RelativeTo(location);
                    await using var tsk = await renderer.StartProgressTask(relPath.ToString(), maxValue: file.FileInfo.Size.Value);
                    var multiHasher = new MultiHasher();
                    await using var stream = file.Read();
                    var result = await multiHasher.HashStream(stream, token, async s => await tsk.Increment(s.Value));
                    hashes[relPath] = result;
                }
            );
        }
        
        await renderer.TextLine($"Indexed {hashes.Count} files, writing hashes", token);
        var hashPathRoot = output / "hashes";
        hashPathRoot.CreateDirectory();

        
        foreach (var (relPath, multiHash) in hashes)
        {
            var hashStr = multiHash.XxHash3.ToString()[2..];
            var path = hashPathRoot / $"{hashStr[..2]}" / (hashStr.ToRelativePath() + ".json");
            path.Parent.CreateDirectory();

            await using var outputStream = path.Create();
            await JsonSerializer.SerializeAsync(outputStream, multiHash, indentedOptions, token);
        }
        
        await renderer.TextLine($"Finished writing {hashes.Count} files", token);
        return 0;
    }

    [Verb("egs app index", "Builds the game hashes database from the given github path")]
    private static async Task<int> Index(
        [Injected] IRenderer renderer,
        [Option("o", "output", "Path to the cloned GitHub hashes repo (with the `json` postfix)")] AbsolutePath output,
        [Option("g", "game", "Game to index")] GameId gameId,
        [Option("a", "appId", "Output path for the built database")] string appId,
        [Injected] JsonSerializerOptions jsonSerializerOptions,
        [Injected] TemporaryFileManager temporaryFileManager,
        [Injected] IGameRegistry gameRegistry,
        [Injected] IServiceProvider serviceProvider,
        [Injected] EgDataClient egDataClient,
        [Injected] CancellationToken token)
    {
                
        var indentedOptions = new JsonSerializerOptions(jsonSerializerOptions)
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault,
        };
        
        await renderer.TextLine($"Getting build information", token);
        
        var requiredHashes = new Dictionary<Sha1, RelativePath>();

        var builds = await egDataClient.GetBuilds(appId, token);
        foreach (var build in builds)
        {
            var buildPath = output / "stores" / "egs" / "builds" / $"{appId}" / $"{build.Id}_metadata.json";
            buildPath.Parent.CreateDirectory();
            {
                await using var outputStream = buildPath.Create();
                await JsonSerializer.SerializeAsync(outputStream, build, indentedOptions, token);
            }
            
            var files = await egDataClient.GetFiles(build.Id, token);
            var filesPath = output / "stores" / "egs" / "builds" / $"{appId}" / $"{build.Id}_files.json";
            {
                await using var outputStream = filesPath.Create();
                await JsonSerializer.SerializeAsync(outputStream, files, indentedOptions, token);
            }

            foreach (var file in files)
            {
                requiredHashes.TryAdd(Sha1.ParseFromHex(file.FileHash), RelativePath.FromUnsanitizedInput(file.FileName)); 
            }
            
            await renderer.TextLine($"Build {build.Id} has {files.Length} files", token);
        }
        
        await renderer.TextLine($"Looking for files to hash", token);
        
        return 0;
    }
}
