using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Loadouts.Ids;
using NexusMods.Abstractions.Loadouts.Mods;
using NexusMods.Abstractions.Loadouts.Synchronizers;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Models;
using NexusMods.Paths;
using File = NexusMods.Abstractions.Loadouts.Files.File;

namespace NexusMods.Games.StardewValley;

public class StardewValleyLoadoutSynchronizer : ALoadoutSynchronizer
{
    public StardewValleyLoadoutSynchronizer(IServiceProvider provider) : base(provider) { }


    protected override async Task<Loadout.ReadOnly> AddChangedFilesToLoadout(Loadout.ReadOnly loadout, TempEntity[] newFiles)
    {
        using var tx = Connection.BeginTransaction();
        var overridesMod = GetOrCreateOverridesMod(loadout, tx);
        var modifiedMods = new HashSet<ModId>();

        var smapiModDirectoryNameToModel = new Dictionary<RelativePath, Mod.ReadOnly>();

        foreach (var newFile in newFiles)
        {
            newFile.Add(File.Loadout, loadout.Id);

            if (!newFile.Contains(File.To))
            {
                AddToOverride(newFile);
                continue;
            }

            var gamePath = newFile.GetFirst(File.To);
            if (!IsModFile(gamePath, out var modDirectoryName))
            {
                AddToOverride(newFile);
                continue;
            }

            if (!smapiModDirectoryNameToModel.TryGetValue(modDirectoryName, out var smapiMod))
            {
                if (!TryGetSMAPIMod(modDirectoryName, loadout, loadout.Db, out smapiMod))
                {
                    AddToOverride(newFile);
                    continue;
                }

                smapiModDirectoryNameToModel[modDirectoryName] = smapiMod;
            }

            newFile.Add(File.Mod, smapiMod.Id);
            newFile.AddTo(tx);
            modifiedMods.Add(smapiMod.ModId);
        }

        foreach (var modId in modifiedMods)
        {
            // If we created the mod in this transaction (e.g. GetOrCreateOverride created the Override mod),
            // Db property will be null, and we can't call `.Revise` on it.
            // We need to manually revise the loadout in that case
            if (modId.Value.Partition == PartitionId.Temp) 
                continue;
            
            var mod = Mod.Load(Connection.Db, modId);
            mod.Revise(tx);
        }
        loadout.Revise(tx);

        var result = await tx.Commit();
        return loadout.Rebase(result.Db);

        void AddToOverride(TempEntity newFile)
        {
            newFile.Add(File.Mod, overridesMod);
            newFile.AddTo(tx);
            modifiedMods.Add(overridesMod);
        }
    }

    private static bool TryGetSMAPIMod(RelativePath modDirectoryName, Loadout.ReadOnly loadout, IDb db, out Mod.ReadOnly mod)
    {
        var manifestFilePath = new GamePath(LocationId.Game, Constants.ModsFolder.Join(modDirectoryName).Join(Constants.ManifestFile));

        var file = File.All(db)
            .FirstOrDefault(file =>
            {
                if (!file.Contains(File.Loadout)) return false;
                if (!file.LoadoutId.Equals(loadout.LoadoutId)) return false;

                if (!file.To.Equals(manifestFilePath)) return false;

                if (!file.Contains(File.Mod)) return false;
                return file.Mod.Enabled;
            });
        
        if (file.IsValid())
        {
            mod = file.Mod;
            return true;
        }
        mod = default(Mod.ReadOnly);
        return false;
    }

    private static bool IsModFile(GamePath gamePath, out RelativePath modDirectoryName)
    {
        modDirectoryName = RelativePath.Empty;
        if (gamePath.LocationId != LocationId.Game) return false;
        var path = gamePath.Path;

        if (!path.StartsWith(Constants.ModsFolder)) return false;
        path = path.DropFirst(numDirectories: 1);

        modDirectoryName = path.TopParent;
        if (modDirectoryName.Equals(RelativePath.Empty)) return false;

        return true;
    }
}
