using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Loadouts.Files;
using NexusMods.Abstractions.Loadouts.Mods;
using NexusMods.Abstractions.Loadouts.Synchronizers;
using NexusMods.Abstractions.Settings;
using NexusMods.Extensions.BCL;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Paths;
using NexusMods.Paths.Extensions;
using File = NexusMods.Abstractions.Loadouts.Files.File;

namespace NexusMods.Games.StardewValley;

public class StardewValleyLoadoutSynchronizerOld : ALoadoutSynchronizerOld
{
    public StardewValleyLoadoutSynchronizerOld(IServiceProvider provider) : base(provider)
    {
        var settingsManager = provider.GetRequiredService<ISettingsManager>();
        _settings = settingsManager.Get<StardewValleySettings>();
    }

    /// <summary>
    /// The content folder of the game, we ignore files in this folder
    /// </summary>
    private static readonly GamePath ContentFolder = new(LocationId.Game, "Content".ToRelativePath());

    private readonly StardewValleySettings _settings;

    public override bool IsIgnoredBackupPath(GamePath path)
    {
        if (_settings.DoFullGameBackup) return false;
        if (path.LocationId != LocationId.Game) return false;
        return path.Path.InFolder(ContentFolder.Path);
    }


    protected override async Task<Loadout.ReadOnly> MoveNewFilesToMods(Loadout.ReadOnly loadout, StoredFile.ReadOnly[] newFiles)
    {
        using var tx = Connection.BeginTransaction();
        var modifiedMods = new HashSet<ModId>();

        var smapiModDirectoryNameToModel = new Dictionary<RelativePath, Mod.ReadOnly>();

        foreach (var newFile in newFiles)
        {
            var gamePath = newFile.AsFile().To;
            if (!IsModFile(gamePath, out var modDirectoryName))
            {
                continue;
            }

            if (!smapiModDirectoryNameToModel.TryGetValue(modDirectoryName, out var smapiMod))
            {
                if (!TryGetSMAPIMod(modDirectoryName, loadout, loadout.Db, out smapiMod))
                {
                    continue;
                }

                smapiModDirectoryNameToModel[modDirectoryName] = smapiMod;
            }

            tx.Add(newFile.Id, File.Mod, smapiMod.Id);
            modifiedMods.Add(smapiMod.ModId);
        }

        // Revise all modified mods
        foreach (var modId in modifiedMods)
        {
            var mod = Mod.Load(Connection.Db, modId);
            mod.Revise(tx);
        }

        // Only commit if we have changes
        if (modifiedMods.Count <= 0) 
            return loadout;
        
        
        var result = await tx.Commit();
        return loadout.Rebase();
    }

    private static bool TryGetSMAPIMod(RelativePath modDirectoryName, Loadout.ReadOnly loadout, IDb db, out Mod.ReadOnly mod)
    {
        var manifestFilePath = new GamePath(LocationId.Game, Constants.ModsFolder.Join(modDirectoryName).Join(Constants.ManifestFile));

        var hasFile = File.FindByLoadout(db, loadout.LoadoutId)
            .TryGetFirst(x => x.To == manifestFilePath && x.Mod.Enabled,
                out var file);
        
        if (hasFile)
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
