using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Records;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Loadouts.Sorting;
using NexusMods.Abstractions.Loadouts.Synchronizers;
using NexusMods.Abstractions.Loadouts.Synchronizers.Rules;
using NexusMods.Games.CreationEngine.Abstractions;
using NexusMods.Hashing.xxHash3;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Paths;
using NexusMods.Sdk.FileStore;

namespace NexusMods.Games.CreationEngine;

public class PluginsFile : IIntrinsicFile
{
    private readonly ICreationEngineGame _game;
    private readonly ISorter _sorter;
    private readonly IFileStore _fileStore;

    private record struct Metadata(RelativePath FileName, ModKey ModKey, Hash Hash, IMod Mod);

    public PluginsFile(ICreationEngineGame game, ISorter sorter, IFileStore fileStore)
    {
        _game = game;
        _fileStore = fileStore;
        _sorter = sorter;
        Path = game.PluginsFile;
    }
    
    public GamePath Path { get; }
    
    public async Task Write(Stream stream, Loadout.ReadOnly loadout, Dictionary<GamePath, SyncNode> syncTree)
    {
        var install = loadout.InstallationInstance;
        var plugins = await syncTree
            .Where(p => p.Key.Parent == KnownPaths.Data && KnownCEExtensions.PluginFiles.Contains(p.Key.Extension))
            .ToAsyncEnumerable()
            .SelectAwait(MakeMetadata)
            .ToDictionaryAsync(x => x.ModKey);

        var sorted = _sorter.Sort<Metadata, ModKey>(plugins.Values.ToList(), IdSelector, plugin => RuleCreator(plugin, plugins));
        
        throw new NotImplementedException();
    }

    private static IReadOnlyList<ISortRule<Metadata, ModKey>> RuleCreator(Metadata metadata, Dictionary<ModKey, Metadata> allPlugins)
    {
        var resultList = new List<ISortRule<Metadata, ModKey>>();
        foreach (var master in metadata.Mod.MasterReferences)
        {
            // Skip missing masters here, we'll catch that in diagnostics
            if (!allPlugins.ContainsKey(master.Master))
                continue;
            
            resultList.Add(new After<Metadata, ModKey>()
            {
                Other = master.Master,
            });
        }

        // ESLs should come after ESMs
        if (metadata.FileName.Extension == KnownCEExtensions.ESL)
        {
            foreach (var other in allPlugins.Values)
            {
                if (other.FileName.Extension == KnownCEExtensions.ESM)
                    resultList.Add(new After<Metadata, ModKey>()
                    {
                        Other = other.ModKey,
                    });
            }
        }
        
        // ESPs should come after ESMs and ESLs
        if (metadata.FileName.Extension == KnownCEExtensions.ESP)
        {
            foreach (var other in allPlugins.Values)
            {
                if (other.FileName.Extension == KnownCEExtensions.ESM || other.FileName.Extension == KnownCEExtensions.ESL)
                    resultList.Add(new After<Metadata, ModKey>()
                    {
                        Other = other.ModKey,
                    });
            }
        }
        
        return resultList;
    }

    private static ModKey IdSelector(Metadata metadata)
    {
        return metadata.ModKey;
    }

    private async ValueTask<Metadata> MakeMetadata(KeyValuePair<GamePath, SyncNode> arg)
    {
        var relPath = arg.Key.Path.FileName;
        Hash hash;
        
        // We need to figure out if we should look at the loadout or the disk
        var syncNode = arg.Value;
        if (syncNode.Actions.HasFlag(Actions.ExtractToDisk) || syncNode.Disk.Hash == Hash.Zero || syncNode.SourceItemType == LoadoutSourceItemType.Game)
            hash = syncNode.Loadout.Hash;
        else
            hash = syncNode.Disk.Hash;
        
        var modHeader = await _game.ParsePlugin(hash, relPath);
        return new Metadata(relPath, modHeader!.ModKey, hash, modHeader);
    }
    


    public Task Ingest(Stream stream, Loadout.ReadOnly loadout, Dictionary<GamePath, SyncNode> syncTree, ITransaction tx)
    {
        throw new NotImplementedException();
    }
}
