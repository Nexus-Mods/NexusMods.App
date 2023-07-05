using NexusMods.DataModel;
using NexusMods.DataModel.JsonConverters;
using NexusMods.DataModel.Loadouts;
using NexusMods.DataModel.Loadouts.LoadoutSynchronizerDTOs;
using NexusMods.DataModel.Loadouts.ModFiles;
using NexusMods.DataModel.Loadouts.Mods;
using NexusMods.DataModel.Sorting;
using NexusMods.DataModel.Sorting.Rules;
using NexusMods.DataModel.TriggerFilter;
using NexusMods.Hashing.xxHash64;
using NexusMods.Paths;
using NexusMods.Paths.Extensions;
using Noggog;
// ReSharper disable AccessToDisposedClosure

namespace NexusMods.Games.BethesdaGameStudios;

[JsonName("BethesdaGameStudios.PluginFile")]
public record PluginFile : AModFile, IGeneratedFile, IToFile, ITriggerFilter<ModFilePair, Plan>
{
    private static RelativePath[] _defaultOrdering = new[]
    {
        "Skyrim.esm",
        "Update.esm",
        "Dawnguard.esm",
        "HearthFires.esm",
        "Dragonborn.esm",
    }.Select(e => e.ToRelativePath()).ToArray();

    public required GamePath To { get; init; }

    private static IEnumerable<IToFile> PluginFiles(IEnumerable<ModFilePair> flattenedList)
    {
        var pluginFiles = flattenedList
            .Select(f => f.File)
            .OfType<IToFile>()
            .Where(f => SkyrimSpecialEdition.PluginExtensions.Contains(f.To.Extension));
        return pluginFiles;
    }

    private ModRuleTuple[] InitialiseFileRuleTuples(IToFile[] pluginFiles)
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

    private IEnumerable<ISortRule<ModRuleTuple, RelativePath>> GenerateRules(ModRuleTuple[] modFiles, IToFile aModFile)
    {
        var defaultIdx = _defaultOrdering.IndexOf(aModFile.To.FileName);
        switch (defaultIdx)
        {
            case 0:
                yield return new First<ModRuleTuple, RelativePath>();
                break;
            case > 1:
                {
                    for (var i = 0; i < defaultIdx; i++)
                    {
                        yield return new After<ModRuleTuple, RelativePath>(_defaultOrdering[i]);
                    }

                    foreach (var itm in modFiles)
                    {
                        if (_defaultOrdering.Contains(itm.Mod.To.FileName)) continue;
                        yield return new Before<ModRuleTuple, RelativePath>(itm.Mod.To.FileName);
                    }

                    break;
                }
            default:
                {
                    foreach (var itm in ((AModFile)aModFile).Metadata.OfType<PluginAnalysisData>())
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

    private struct ModRuleTuple
    {
        public IToFile Mod;
        public IReadOnlyList<ISortRule<ModRuleTuple, RelativePath>> Rules;
    }

    public ITriggerFilter<ModFilePair, Plan> TriggerFilter => this;

    public async Task<Hash> GenerateAsync(Stream stream, ApplyPlan plan, CancellationToken token = default)
    {
        var pluginFiles = PluginFiles(plan.Flattened.Values).ToArray();

        var pluginFileRuleTuples = InitialiseFileRuleTuples(pluginFiles);

        var results = Sorter.Sort<ModRuleTuple, RelativePath, ModRuleTuple[]>(pluginFileRuleTuples,
            i => ((IToFile)i.Mod).To.FileName,
            i => i.Rules, //GenerateRules(pluginFiles, i),
            RelativePath.Comparer);

        await stream.WriteAllLinesAsync(results.Select(i => i.Mod.To.FileName.ToString()), token: token);
        stream.Position = 0;
        return await stream.XxHash64Async(token);
    }

    public Hash GetFingerprint(ModFilePair self, Plan plan)
    {
        using var fingerprinter = Fingerprinter.Create();

        plan.Flattened
            .Where(f => SkyrimSpecialEdition.PluginExtensions.Contains(f.Key.Extension))
            .Select(f => (f.Key.FileName, f.Value.File.DataStoreId))
            .OrderBy(f => f)
            .ForEach(f =>
            {
                fingerprinter.Add(f.FileName);
                fingerprinter.Add(f.DataStoreId);
            });

        return fingerprinter.Digest();
    }
}
