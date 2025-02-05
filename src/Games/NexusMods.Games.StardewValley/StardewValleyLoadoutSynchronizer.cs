using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Loadouts.Synchronizers;
using NexusMods.Abstractions.Settings;
using NexusMods.Extensions.BCL;
using NexusMods.Games.StardewValley.Models;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Paths;
using NexusMods.Paths.Extensions;

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

    // The apis for this are removed for now. Post-beta we will update the structure of the app to support updating config files
    // in read-only collections as overlay mods.
    /*
    protected override ValueTask MoveNewFilesToMods(Loadout.ReadOnly loadout, IEnumerable<AddedEntry> newFiles, ITransaction tx)
    {
        var smapiModDirectoryNameToModel = new Dictionary<RelativePath, SMAPIModLoadoutItem.ReadOnly>();

        foreach (var newFile in newFiles)
        {
            if (!IsModFile(newFile.LoadoutItemWithTargetPath.TargetPath, out var modDirectoryName))
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
            
            newFile.LoadoutItem.ParentId = smapiMod.Id;
        }
        return ValueTask.CompletedTask;
    }
    */

    private static bool TryGetSMAPIMod(RelativePath modDirectoryName, Loadout.ReadOnly loadout, IDb db, out SMAPIModLoadoutItem.ReadOnly mod)
    {
        var manifestFilePath = new GamePath(LocationId.Game, Constants.ModsFolder.Join(modDirectoryName).Join(Constants.ManifestFile));

        if (!LoadoutItemWithTargetPath.FindByTargetPath(db, manifestFilePath.ToGamePathParentTuple(loadout))
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
