using DynamicData;
using NexusMods.Common;
using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.Loadouts;
using NexusMods.DataModel.Loadouts.ModFiles;
using NexusMods.DataModel.LoadoutSynchronizer;
using NexusMods.DataModel.Sorting;
using NexusMods.DataModel.Sorting.Rules;
using NexusMods.Paths;
using NexusMods.Paths.Extensions;

namespace NexusMods.Games.BethesdaGameStudios;

/// <summary>
/// A component that sorts plugins according to their masters list
/// </summary>
public class PluginSorter
{
    private static readonly RelativePath[] DefaultOrdering = new[]
    {
        "Skyrim.esm",
        "Update.esm",
        "Dawnguard.esm",
        "HearthFires.esm",
        "Dragonborn.esm",
    }.Select(e => e.ToRelativePath()).ToArray();

    private readonly PluginAnalyzer _pluginAnalyzer;
    private readonly IArchiveManager _archiveManager;

    public PluginSorter(PluginAnalyzer pluginAnalyzer, IArchiveManager archiveManager)
    {
        _pluginAnalyzer = pluginAnalyzer;
        _archiveManager = archiveManager;
    }

    public async Task<RelativePath[]> Sort(FileTree tree, CancellationToken token)
    {
        // Get all plugins and analyze them
        var allPlugins = await tree[Constants.DataFolder]
            .Children
            .Values
            .Where(c => c.IsFile)
            .Select(c => c.Value)
            // For now we only support plugins that are not generated on-the-fly
            .OfType<FromArchive>()
            .Where(f => SkyrimSpecialEdition.PluginExtensions.Contains(f.To.Extension))
            .SelectAsync(async f => await GetAnalysis(f, token))
            .Where(result => result is not null)
            .Select(result => result!)
            .ToArrayAsync(cancellationToken: token);

        if (allPlugins.Length == 0) return Array.Empty<RelativePath>();

        // Generate the rules for each plugin
        var tuples = allPlugins
            .Select(r => new RuleTuple
            {
                Plugin = r!.FileName,
                Rules = GenerateRules(allPlugins, r).Distinct().ToArray()
            })
            .ToArray();

        // Sort the plugins
        var results = Sorter.Sort<RuleTuple, RelativePath, RuleTuple[]>(tuples,
            i => i.Plugin,
            i => i.Rules,
            RelativePath.Comparer);

        // Return the sorted plugins, projected from the tuples
        return results.Select(r => r.Plugin).ToArray();
    }

    private IEnumerable<ISortRule<RuleTuple, RelativePath>> GenerateRules(PluginAnalysisData[] allPlugins, PluginAnalysisData plugin)
    {
        // Does this plugin have a predefined (locked) position?
        var defaultIdx = DefaultOrdering.IndexOf(plugin.FileName);
        switch (defaultIdx)
        {
            // It's the first plugin, so we generate a single rule
            case 0:
                yield return new First<RuleTuple, RelativePath>();
                break;
            // It's not the first plugin, so we generate a rule for each plugin that comes before it
            case >= 1:
                {
                    for (var i = 0; i < defaultIdx; i++)
                    {
                        yield return new After<RuleTuple, RelativePath> { Other = DefaultOrdering[i]};
                    }

                    foreach (var itm in allPlugins)
                    {
                        if (DefaultOrdering.Contains(itm.FileName)) continue;
                        yield return new Before<RuleTuple, RelativePath>(itm.FileName);
                    }

                    break;
                }
            // It's not in the default ordering, so we generate a rule for each master and then a
            // rule based on the file type
            default:
                {
                    // Rules for all the masters
                    foreach (var itm in allPlugins)
                    {
                        foreach (var dep in itm.Masters)
                        {
                            yield return new After<RuleTuple, RelativePath> {Other = dep};
                        }
                    }

                    // ESLs come right after the last ESM
                    if (plugin.FileName.Extension == SkyrimSpecialEdition.ESL)
                    {
                        foreach (var file in allPlugins.Where(m => m.FileName.Extension == SkyrimSpecialEdition.ESM))
                            yield return new After<RuleTuple, RelativePath> { Other = file.FileName};
                    }
                    // ESPs come right after the last ESL
                    else if (plugin.FileName.Extension == SkyrimSpecialEdition.ESP)
                    {
                        foreach (var file in allPlugins.Where(m => m.FileName.Extension != SkyrimSpecialEdition.ESP))
                            yield return new After<RuleTuple, RelativePath> { Other = file.FileName};
                    }
                    break;
                }
        }
    }

    /// <summary>
    /// Helper struct to store the plugin and its rules
    /// </summary>
    private struct RuleTuple
    {
        public RelativePath Plugin { get; set; }
        public ISortRule<RuleTuple, RelativePath>[] Rules { get; set; }
    }

    /// <summary>
    /// Gets the analysis data for a plugin
    /// </summary>
    /// <param name="archive"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    private async Task<PluginAnalysisData?> GetAnalysis(FromArchive archive, CancellationToken token)
    {
        await using var stream = await _archiveManager.GetFileStream(archive.Hash, token);
        return await _pluginAnalyzer.AnalyzeAsync(archive.To.FileName, stream, token);
    }
}
