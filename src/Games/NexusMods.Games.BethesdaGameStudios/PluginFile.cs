using NexusMods.DataModel;
using NexusMods.DataModel.JsonConverters;
using NexusMods.DataModel.Loadouts;
using NexusMods.DataModel.Sorting;
using NexusMods.DataModel.Sorting.Rules;
using NexusMods.Hashing.xxHash64;
using NexusMods.Paths;
using NexusMods.Paths.Extensions;
using Noggog;

namespace NexusMods.Games.BethesdaGameStudios;

[JsonName("BethesdaGameStudios.PluginFile")]
public record PluginFile : AGeneratedFile
{
    private static RelativePath[] DefaultOrdering = new string[]
    {
        "Skyrim.esm",
        "Update.esm",
        "Dawnguard.esm",
        "HearthFires.esm",
        "Dragonborn.esm",
    }.Select(e => e.ToRelativePath()).ToArray();

    public override async Task GenerateAsync(Stream stream, Loadout loadout, IReadOnlyCollection<(AModFile File, Mod Mod)> flattenedList,
        CancellationToken ct = default)
    {
        var pluginFiles = flattenedList
            .Select(f => f.File)
            .Where(f => SkyrimSpecialEdition.PluginExtensions.Contains(f.To.Extension))
            .ToArray();

        var pluginFileRuleTuples = InitialiseFileRuleTuples(pluginFiles);

        var results = Sorter.Sort<ModRuleTuple, RelativePath, ModRuleTuple[]>(pluginFileRuleTuples,
            i => i.Mod.To.FileName,
            i => i.Rules, //GenerateRules(pluginFiles, i),
            RelativePath.Comparer);

        await stream.WriteAllLinesAsync(results.Select(i => i.Mod.To.FileName.ToString()), token: ct);
    }

    private ModRuleTuple[] InitialiseFileRuleTuples(AModFile[] pluginFiles)
    {
        // Init Mods
        var pluginFileRuleTuples = GC.AllocateUninitializedArray<ModRuleTuple>(pluginFiles.Length);
        for (var x = 0; x < pluginFileRuleTuples.Length; x++)
        {
            ref var entry = ref pluginFileRuleTuples[x];
            entry.Mod = pluginFiles[x];
        }

        // Generate & Cache Rules
        for (var x = 0; x < pluginFileRuleTuples.Length; x++)
        {
            ref var entry = ref pluginFileRuleTuples[x];
            entry.Rules = GenerateRules(pluginFileRuleTuples, entry.Mod).ToArray();
        }

        return pluginFileRuleTuples;
    }

    private IEnumerable<ISortRule<ModRuleTuple, RelativePath>> GenerateRules(ModRuleTuple[] modFiles, AModFile aModFile)
    {
        var defaultIdx = DefaultOrdering.IndexOf(aModFile.To.FileName);
        switch (defaultIdx)
        {
            case 0:
                yield return new First<ModRuleTuple, RelativePath>();
                break;
            case > 1:
                {
                    for (var i = 0; i < defaultIdx; i++)
                    {
                        yield return new After<ModRuleTuple, RelativePath>(DefaultOrdering[i]);
                    }

                    foreach (var itm in modFiles)
                    {
                        if (DefaultOrdering.Contains(itm.Mod.To.FileName)) continue;
                        yield return new Before<ModRuleTuple, RelativePath>(itm.Mod.To.FileName);
                    }

                    break;
                }
            default:
                {
                    foreach (var itm in aModFile.Metadata.OfType<AnalysisSortData>())
                    {
                        foreach (var dep in itm.Masters)
                        {
                            yield return new After<ModRuleTuple, RelativePath>(dep);
                        }
                    }

                    if (aModFile.To.Extension == SkyrimSpecialEdition.ESL)
                    {
                        foreach (var file in modFiles.Where(m => m.Mod.To.Extension == SkyrimSpecialEdition.ESM))
                            yield return new After<ModRuleTuple, RelativePath>(file.Mod.To.FileName);
                    }
                    else if (aModFile.To.Extension == SkyrimSpecialEdition.ESP)
                    {
                        foreach (var file in modFiles.Where(m => m.Mod.To.Extension != SkyrimSpecialEdition.ESP))
                            yield return new After<ModRuleTuple, RelativePath>(file.Mod.To.FileName);
                    }


                    break;
                }
        }
    }

    public override async Task<(Size Size, Hash Hash)> GetMetaData(Loadout loadout, IReadOnlyCollection<(AModFile File, Mod Mod)> flattenedList, CancellationToken ct = default)
    {
        var ms = new MemoryStream();
        await GenerateAsync(ms, loadout, flattenedList, ct);
        ms.Position = 0;
        return (Size.From(ms.Length), await ms.Hash(token: ct));
    }

    private struct ModRuleTuple
    {
        public AModFile Mod;
        public IReadOnlyList<ISortRule<ModRuleTuple, RelativePath>> Rules;
    }
}
