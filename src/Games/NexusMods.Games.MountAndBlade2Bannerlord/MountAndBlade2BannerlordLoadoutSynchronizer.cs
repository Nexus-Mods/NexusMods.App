using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Loadouts.Synchronizers;
using NexusMods.Abstractions.Settings;
using NexusMods.Extensions.BCL;
using NexusMods.Games.MountAndBlade2Bannerlord.Models;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Paths;
using NexusMods.Paths.Extensions;
using static NexusMods.Games.MountAndBlade2Bannerlord.MountAndBlade2BannerlordConstants;

namespace NexusMods.Games.MountAndBlade2Bannerlord;

public class MountAndBlade2BannerlordLoadoutSynchronizer : ALoadoutSynchronizer
{
    public MountAndBlade2BannerlordLoadoutSynchronizer(IServiceProvider provider) : base(provider)
    {
        var settingsManager = provider.GetRequiredService<ISettingsManager>();
        _settings = settingsManager.Get<MountAndBlade2BannerlordSettings>();
    }

    private readonly MountAndBlade2BannerlordSettings _settings;

    public override bool IsIgnoredBackupPath(GamePath path)
    {
        if (_settings.DoFullGameBackup) return false;
        return true;
    }

    protected override ValueTask MoveNewFilesToMods(Loadout.ReadOnly loadout, IEnumerable<AddedEntry> newFiles, ITransaction tx)
    {
        var modDirectoryNameToModel = new Dictionary<RelativePath, ModLoadoutItem.ReadOnly>();

        foreach (var newFile in newFiles)
        {
            GamePath gamePath;

            if (!IsModFile(newFile.LoadoutItemWithTargetPath.TargetPath, out var modDirectoryName))
            {
                continue;
            }

            if (!modDirectoryNameToModel.TryGetValue(modDirectoryName, out var mod))
            {
                if (!TryGetSMAPIMod(modDirectoryName, loadout, loadout.Db, out mod))
                {
                    continue;
                }

                modDirectoryNameToModel[modDirectoryName] = mod;
            }
            
            newFile.LoadoutItem.ParentId = mod.Id;
        }
        return ValueTask.CompletedTask;
    }

    private static bool TryGetSMAPIMod(RelativePath modDirectoryName, Loadout.ReadOnly loadout, IDb db, out ModLoadoutItem.ReadOnly mod)
    {
        var manifestFilePath = new GamePath(LocationId.Game, ModsFolder.Join(modDirectoryName).Join(SubModuleFile));

        if (!LoadoutItemWithTargetPath.FindByTargetPath(db, manifestFilePath.ToGamePathParentTuple(loadout))
                .TryGetFirst(x => x.AsLoadoutItem().LoadoutId == loadout && x.Contains(ModuleInfoFileLoadoutFile.ModuleInfoFile), out var file))
        {
            mod = default(ModLoadoutItem.ReadOnly);
            return false;
        }

        mod = ModLoadoutItem.Load(db, file.AsLoadoutItem().Parent);
        return true;
    }

    private static bool IsModFile(GamePath gamePath, out RelativePath modDirectoryName)
    {
        modDirectoryName = RelativePath.Empty;
        if (gamePath.LocationId != LocationId.Game) return false;
        var path = gamePath.Path;

        if (!path.StartsWith(ModsFolder)) return false;
        path = path.DropFirst(numDirectories: 1);

        modDirectoryName = path.TopParent;
        if (modDirectoryName.Equals(RelativePath.Empty)) return false;

        return true;
    }
}
