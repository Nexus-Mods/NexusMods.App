
using NexusMods.Abstractions.CLI;
using NexusMods.DataModel.Games;
using NexusMods.Paths;

namespace NexusMods.DataModel.Tests.Verbs;

public class GenerateGameFileHashes : AVerb<AbsolutePath>, IRenderingVerb
{
    public IRenderer Renderer { get; set; }

    private readonly IGame[] _games;
    private readonly FileHashCache _cache;

    public GenerateGameFileHashes(IEnumerable<IGame> games, FileHashCache cache)
    {
        _games = games.ToArray();
        _cache = cache;
    }

    public static VerbDefinition Definition => new VerbDefinition("generate-game-file-hashes",
        "Generates the hashes for all the files in all the installed games and outputs the data files in the given directory. If the files already exist, they will be merged with the new data",
        new[] { new OptionDefinition<AbsolutePath>("o", "output", "The directory to output the data files to") });
    public Task<int> Run(AbsolutePath a, CancellationToken token)
    {
        foreach (var g in _games)
        {
            Renderer.Render($"Generating hashes for {g.Name}");
            foreach (var result in _cache.
        }
    }

}
