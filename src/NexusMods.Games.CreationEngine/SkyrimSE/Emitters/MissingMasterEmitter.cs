using System.Collections.Frozen;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using NexusMods.Abstractions.Diagnostics;
using NexusMods.Abstractions.Diagnostics.Emitters;
using NexusMods.Abstractions.Diagnostics.References;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Loadouts.Synchronizers;
using NexusMods.Paths;

namespace NexusMods.Games.CreationEngine.SkyrimSE.Emitters;

public class MissingMasterEmitter : ILoadoutDiagnosticEmitter
{
    private readonly SkyrimSE _game;
    private SkyrimSESynchronizer _synchronizer => (SkyrimSESynchronizer)_game.Synchronizer;

    public MissingMasterEmitter(SkyrimSE game)
    {
        _game = game;
    }
    
    public IAsyncEnumerable<Diagnostic> Diagnose(Loadout.ReadOnly loadout, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    private static readonly Extension[] _extensions = [KnownCEExtensions.ESM, KnownCEExtensions.ESP, KnownCEExtensions.ESL];

    public async IAsyncEnumerable<Diagnostic> Diagnose(Loadout.ReadOnly loadout, FrozenDictionary<GamePath, SyncNode> syncTree, CancellationToken cancellationToken)
    {
        var db = loadout.Db;
        var plugins = syncTree.Where(static node =>
            node.Value.HaveLoadout &&
            _extensions.Contains(node.Key.Extension) &&
            node.Key.Parent == KnownPaths.Data);
        
        Dictionary<string, LoadoutItemGroup.ReadOnly> pluginsInLoadout = new();

        foreach (var item in loadout.Items.OfTypeLoadoutItemWithTargetPath())
        {
            var path = (GamePath)item.TargetPath;
            if (path.LocationId == LocationId.Game && path.Parent == KnownPaths.Data && _extensions.Contains(path.Extension))
                pluginsInLoadout[path.FileName] = item.AsLoadoutItem().Parent;

        }

        Dictionary<ModKey, (SyncNode Node, ISkyrimModHeaderGetter Header)> pluginHeaders = new();
        foreach (var (key, node) in plugins)
        {
            var header = await _synchronizer.HeaderForPlugin(node.Loadout.Hash, key.Path.FileName);
            pluginHeaders[header.Item1] = (node, header.Item2);
        }

        foreach (var header in pluginHeaders)
        {
            foreach (var master in header.Value.Header.MasterReferences)
            {
                if (pluginHeaders.ContainsKey(master.Master))
                    continue;
                yield return Diagnostics.CreateMissingRequiredDependency(
                    new LoadoutItemGroupReference()
                    {
                        TxId = loadout.Db.BasisTxId,
                        DataId = header.Value.Node.Loadout.EntityId
                    },
                    header.Key,
                    master.Master
                );
            }
        }

    }
}
