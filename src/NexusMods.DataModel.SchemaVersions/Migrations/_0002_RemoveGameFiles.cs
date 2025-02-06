using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Games.FileHashes;
using NexusMods.Abstractions.Loadouts;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.TxFunctions;

namespace NexusMods.DataModel.SchemaVersions.Migrations;

/// <summary>
/// Delete the game files group, and add the loadout version and locatorids
/// </summary>
public class _0002_RemoveGameFiles : ITransactionalMigration
{
    private readonly IFileHashesService _fileHashesService;
    private HashSet<Symbol> _gameGroupAttrs = [];
    private HashSet<IAttribute> _resolvedGroupAttrs = [];
    public static (MigrationId Id, string Name) IdAndName => MigrationId.ParseNameAndId(nameof(_0002_RemoveGameFiles));
    
    /// <summary>
    /// DI Constructor
    /// </summary>
    public _0002_RemoveGameFiles(IFileHashesService fileHashService)
    {
        _fileHashesService = fileHashService;
    }
    
    public async Task Prepare(IDb db)
    {
        await _fileHashesService.GetFileHashesDb();
        _gameGroupAttrs = db.AttributeCache.AllAttributeIds
            .Where(sym => sym.Namespace == "NexusMods.Loadouts.LoadoutGameFilesGroup")
            .ToHashSet();
        _resolvedGroupAttrs = db.Connection.AttributeResolver.DefinedAttributes.Where(attr => _gameGroupAttrs.Contains(attr.Id)).ToHashSet();
    }

    public void Migrate(ITransaction tx, IDb db)
    {
        
        foreach (var loadout in Loadout.All(db))
        {
            // This doesn't seem likely, that we'd have this without this migration being run, but we'll check anyway
            if (loadout.Contains(Loadout.LocatorIds))
                continue;
            
            // Get groups attached to the loadout that have the game files group attributes
            var gameFilesGroups = loadout.Items.OfTypeLoadoutItemGroup().Where(grp => _resolvedGroupAttrs.Any(attr => grp.Contains(attr)));

            var files = gameFilesGroups
                .SelectMany(grp => grp.Children)
                .OfTypeLoadoutItemWithTargetPath()
                .OfTypeLoadoutFile()
                .Select(file => ((GamePath)file.AsLoadoutItemWithTargetPath().TargetPath, file.Hash));
            
            var suggestedVersion = _fileHashesService.SuggestGameVersion(loadout.InstallationInstance, files);
            if (!_fileHashesService.TryGetLocatorIdsForVersion(loadout.InstallationInstance, suggestedVersion, out var locatorIds))
                throw new Exception("Could not find locatorIds for version, this should never happen");
            
            tx.Add(loadout, Loadout.GameVersion, suggestedVersion);
            foreach (var locatorId in locatorIds) 
                tx.Add(loadout, Loadout.LocatorIds, locatorId);

            foreach (var group in gameFilesGroups)
            {
                tx.Delete(group, false);
                DeleteChildren(tx, group.AsLoadoutItem());
            }


            void DeleteChildren(ITransaction tx, LoadoutItem.ReadOnly item)
            {
                if (!item.TryGetAsLoadoutItemGroup(out var group))
                    return;
                
                foreach (var child in group.Children)
                {
                    tx.Delete(child, false);
                    DeleteChildren(tx, child);
                }
            }
        }
        
    }
}
