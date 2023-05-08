using NexusMods.DataModel.Games;
using NexusMods.DataModel.Loadouts;
using NexusMods.DataModel.TransformerHooks.BeforeMakeApplyPlan;
using NexusMods.Hashing.xxHash64;
using NexusMods.Paths;

namespace NexusMods.DataModel.TransformerHooks.Transformers;

/// <summary>
/// Generates files for mods that have a zero hash.
/// </summary>
public class GenerateFiles : IBeforeMakeApplyPlan
{
    private readonly LoadoutManager _manager;
    
    public IEnumerable<GameDomain> GameDomains { get; } = new []{ }
    public IEnumerable<GamePath> Files { get; }

    /// <summary>
    /// DI constructor.
    /// </summary>
    /// <param name="manager"></param>
    public GenerateFiles(LoadoutManager manager)
    {
        _manager = manager;
    }
    
    /// <inheritdoc />
    public async ValueTask<Dictionary<GamePath, (AModFile File, Mod Mod)>> BeforeMakeApplyPlan
    (Dictionary<GamePath, (AModFile File, Mod Mod)> files, Loadout loadout, CancellationToken token)
    {
        var generated = new List<(AModFile, Mod)>();
        foreach (var (key, pair) in files.ToList())
        {
            var (file, mod) = pair;
            if (file is not AGeneratedFile gen || gen.Hash != Hash.Zero)
                continue;
            
            var metaData = await gen.GetMetadataAsync(loadout, files.Values, token);
            var newFile = (gen with { Hash = metaData.Hash, Size = metaData.Size }, mod);
            generated.Add(newFile);
            files[key] = newFile;
        }

        _manager.ReplaceFiles(loadout.LoadoutId, generated, $"Generated {generated.Count} files");
        return files;
    }



    public ValueTask<Result> BeforeMakeApplyPlan(AModFile file, IReadOnlyDictionary<GamePath, (AModFile File, Mod Mod)> flattenedList,
        Loadout loadout, CancellationToken ct)
    {
        throw new NotImplementedException();
    }
}
