using System.Text.Json;
using NexusMods.Abstractions.CLI;
using NexusMods.DataModel.Extensions;
using NexusMods.DataModel.Games;
using NexusMods.Paths;
using NexusMods.Paths.Utilities;

namespace NexusMods.DataModel.Verbs;

/// <summary>
/// Hashes game files and exports them to a folder for use in the Synchronizer methods
/// </summary>
public class GenerateGameFileHashes : AVerb<AbsolutePath>, IRenderingVerb
{
    public IRenderer Renderer { get; set; } = null!;

    private readonly IGame[] _games;
    private readonly FileHashCache _cache;
    private readonly JsonSerializerOptions _options;

    /// <summary>
    /// DI constructor
    /// </summary>
    /// <param name="games"></param>
    /// <param name="cache"></param>
    /// <param name="options"></param>
    public GenerateGameFileHashes(IEnumerable<IGame> games, FileHashCache cache, JsonSerializerOptions options)
    {
        _games = games.ToArray();
        _cache = cache;
        _options = options;
    }

    public static VerbDefinition Definition => new VerbDefinition("generate-game-file-hashes",
        "Generates the hashes for all the files in all the installed games and outputs the data files in the given directory. If the files already exist, they will be merged with the new data",
        new[] { new OptionDefinition<AbsolutePath>("o", "output", "The directory to output the data files to") });
    public async Task<int> Run(AbsolutePath output, CancellationToken token)
    {
        foreach (var g in _games)
        {
            foreach (var installation in g.Installations)
            {
                await Renderer.Render($"Generating hashes for {g.Name} - {installation.Store}");
                foreach (var (id, descriptor) in installation.LocationsRegister.LocationDescriptors)
                {
                    var path = installation.LocationsRegister.GetResolvedPath(id);
                    await Renderer.Render($" - Indexing {id} at {path}");
                    var files = await _cache.IndexFolderAsync(path, token).ToArrayAsync(cancellationToken: token);
                    await Renderer.Render($" - Found {files.Length} files");

                    var converted = files.Select(f => new HashedGameFile()
                    {
                        Path = installation.LocationsRegister.ToGamePath(f.Path),
                        Hash = f.Hash,
                        Size = f.Size,
                    }).ToArray();
                    await SaveData(output, installation, converted);
                }
            }
        }

        return 0;
    }

    private async Task SaveData(AbsolutePath output, GameInstallation installation, HashedGameFile[] files)
    {
        var outputFile = output.Combine(installation.Game.Name.Replace(" ", "_"))
            .AppendExtension(new Extension(".json.brotili"));

        var entries = Array.Empty<HashedGameFile>();
        if (outputFile.FileExists)
        {
            await using var fs = outputFile.Read();
            entries = await fs.ReadBrotiliJson<HashedGameFile[]>(_options);
        }

        entries = entries!.Concat(files).Distinct().ToArray();

        await using var outfs = outputFile.Create();
        await outfs.WriteAsBrotiliJson(entries, _options);
    }
}
