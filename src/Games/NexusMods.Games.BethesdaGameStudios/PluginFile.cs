using NexusMods.DataModel;
using NexusMods.DataModel.JsonConverters;
using NexusMods.DataModel.Loadouts;
using NexusMods.DataModel.Sorting;
using NexusMods.DataModel.Sorting.Rules;
using NexusMods.FileExtractor.FileSignatures;
using NexusMods.Hashing.xxHash64;
using NexusMods.Paths;
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
        var results = Sorter.Sort<AModFile, RelativePath, AModFile[]>(pluginFiles, 
            i => i.To.FileName,
            i => GenerateRules(pluginFiles, i),
            RelativePath.Comparer);

        await stream.WriteAllLinesAsync(results.Select(i => i.To.FileName.ToString()), token: ct);
    }

    private IEnumerable<ISortRule<AModFile, RelativePath>> GenerateRules(AModFile[] modFiles, AModFile aModFile)
    {
        var defaultIdx = DefaultOrdering.IndexOf(aModFile.To.FileName);
        switch (defaultIdx)
        {
            case 0:
                yield return new First<AModFile, RelativePath>();
                break;
            case > 1:
            {
                for (var i = 0; i < defaultIdx; i++)
                {
                    yield return new After<AModFile, RelativePath>(DefaultOrdering[i]);
                }

                foreach (var itm in modFiles)
                {
                    if (DefaultOrdering.Contains(itm.To.FileName)) continue;
                    yield return new Before<AModFile, RelativePath>(itm.To.FileName);
                }

                break;
            }
            default:
            {
                foreach (var itm in aModFile.Metadata.OfType<AnalysisSortData>())
                {
                    foreach (var dep in itm.Masters)
                    {
                        yield return new After<AModFile, RelativePath>(dep);
                    }
                }

                if (aModFile.To.Extension == SkyrimSpecialEdition.ESL)
                {
                    foreach (var file in modFiles.Where(m => m.To.Extension == SkyrimSpecialEdition.ESM))
                        yield return new After<AModFile, RelativePath>(file.To.FileName);
                }
                else if (aModFile.To.Extension == SkyrimSpecialEdition.ESP)
                {
                    foreach (var file in modFiles.Where(m => m.To.Extension != SkyrimSpecialEdition.ESP))
                        yield return new After<AModFile, RelativePath>(file.To.FileName);
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
}