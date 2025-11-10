using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Cli;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.GameLocators.Stores.EGS;
using NexusMods.Abstractions.Games;
using NexusMods.Paths;
using NexusMods.Paths.Extensions;
using NexusMods.Sdk.Hashes;
using NexusMods.Sdk.Jobs;
using NexusMods.Sdk.ProxyConsole;

namespace NexusMods.Networking.EpicGameStore;

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
            .Where(g => g.Value.Game.NexusModsGameId == game.NexusModsGameId)
            .FirstOrDefault(g => g.Value.Game is IEpicGame); 
        
        if (installation.Value == null)
        {
            await renderer.TextLine($"Game {castedGame.DisplayName} is not installed via Epic Game Store.", token);
            return 1;
        }
        
        var location = installation.Value.LocationsRegister[LocationId.Game];

        
        await renderer.TextLine($"Indexing {castedGame.DisplayName} at ({location})", token);

        var hashes = new ConcurrentDictionary<RelativePath, MultiHash>();

        await using (var _ = await renderer.WithProgress())
        {
            await Parallel.ForEachAsync(location.EnumerateFiles(), token, async (file, token) =>
                {
                    var relPath = file.RelativeTo(location);

                    await using var progressTask = await renderer.StartProgressTask(relPath.ToString(), maxValue: file.FileInfo.Size.Value);
                    await using var stream = file.Read();
                    await using var progressWrapper = new StreamProgressWrapper<ProgressTask>(stream, state: progressTask, notifyWritten: static (progressTask, values) =>
                    {
                        var (current, _) = values;
                        var task = progressTask.Increment(current.Value);
                    });

                    var result = await MultiHasher.HashStream(stream, cancellationToken: token);
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
        
        var requiredHashes = new Dictionary<Sha1Value, RelativePath>();

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
                requiredHashes.TryAdd(Sha1Value.FromHex(file.FileHash), RelativePath.FromUnsanitizedInput(file.FileName)); 
            }

            await renderer.TextLine($"Build {build.Id} has {files.Length} files", token);
        }
        
        await renderer.TextLine($"Looking for files to hash", token);
        
        return 0;
    }
}
