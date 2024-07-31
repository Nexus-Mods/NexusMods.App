using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Loadouts.Files;
using NexusMods.Abstractions.Loadouts.Mods;
using NexusMods.Abstractions.Loadouts.Synchronizers;
using NexusMods.Abstractions.Settings;
using NexusMods.Extensions.BCL;
using NexusMods.Games.StardewValley.Models;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Paths;
using NexusMods.Paths.Extensions;
using File = NexusMods.Abstractions.Loadouts.Files.File;

namespace NexusMods.Games.StardewValley;

public class StardewValleyLoadoutSynchronizer : ALoadoutSynchronizer
{
    public StardewValleyLoadoutSynchronizer(IServiceProvider provider) : base(provider)
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
    
    protected override async Task<Loadout.ReadOnly> MoveNewFilesToMods(Loadout.ReadOnly loadout, LoadoutFile.ReadOnly[] newFiles)
    {
        using var tx = Connection.BeginTransaction();
        var smapiModDirectoryNameToModel = new Dictionary<RelativePath, SMAPIModLoadoutItem.ReadOnly>();

        var modified = 0;
        
        foreach (var newFile in newFiles)
        {
            var gamePath = newFile.AsLoadoutItemWithTargetPath().TargetPath;
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

            tx.Add(newFile.Id, LoadoutItem.Parent, smapiMod.Id);
            modified += 1;
        }

        // Only commit if we have changes
        if (modified <= 0)
            return loadout;
        
        
        var result = await tx.Commit();
        return loadout.Rebase();
    }

    private static bool TryGetSMAPIMod(RelativePath modDirectoryName, Loadout.ReadOnly loadout, IDb db, out SMAPIModLoadoutItem.ReadOnly mod)
    {
        var manifestFilePath = new GamePath(LocationId.Game, Constants.ModsFolder.Join(modDirectoryName).Join(Constants.ManifestFile));

        if (!LoadoutItemWithTargetPath.FindByTargetPath(db, manifestFilePath)
                .TryGetFirst(x => x.AsLoadoutItem().LoadoutId == loadout && x.Contains(SMAPIManifestLoadoutFile.ManifestFile), out var file))
        {
            mod = default(SMAPIModLoadoutItem.ReadOnly);
            return false;
        }

        mod = SMAPIModLoadoutItem.Load(db, file.AsLoadoutItem().Parent);
        return true;
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
