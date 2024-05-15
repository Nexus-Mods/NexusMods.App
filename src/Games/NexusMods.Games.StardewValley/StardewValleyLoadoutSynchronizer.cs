using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Loadouts;
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


    protected override async Task<Loadout.Model> AddChangedFilesToLoadout(Loadout.Model loadout, TempEntity[] newFiles)
    {
        using var tx = Connection.BeginTransaction();
        var overridesMod = GetOrCreateOverridesMod(loadout, tx);

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

            var smapiMod = GetSMAPIMod(modDirectoryName, loadout, loadout.Db);
            if (smapiMod is null)
            {
                AddToOverride(newFile);
                continue;
            }

            newFile.Add(File.Mod, smapiMod.Id);
            newFile.AddTo(tx);
        }

        var result = await tx.Commit();
        return result.Db.Get<Loadout.Model>(loadout.Id);

        void AddToOverride(TempEntity newFile)
        {
            newFile.Add(File.Mod, overridesMod.Id);
            newFile.AddTo(tx);
        }
    }

    private static Mod.Model? GetSMAPIMod(RelativePath modDirectoryName, Loadout.Model loadout, IDb db)
    {
        var manifestFilePath = new GamePath(LocationId.Game, Constants.ModsFolder.Join(modDirectoryName).Join(Constants.ManifestFile));

        var manifestFile = db
            .Find(File.To)
            .Select(db.Get<File.Model>)
            .FirstOrDefault(file => file.LoadoutId.Equals(loadout.LoadoutId) && file.To.Equals(manifestFilePath));

        return manifestFile?.Mod;
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
