using NexusMods.Abstractions.DiskState;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Loadouts.Synchronizers;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Models;
using NexusMods.Paths;
using File = NexusMods.Abstractions.Loadouts.Files.File;

namespace NexusMods.Games.StardewValley;

public class StardewValleyLoadoutSynchronizer : ALoadoutSynchronizer
{
    public StardewValleyLoadoutSynchronizer(IServiceProvider provider) : base(provider) { }

    protected override ValueTask<TempEntity> HandleNewFile(DiskStateEntry newEntry, GamePath gamePath, AbsolutePath absolutePath)
    {
        if (!IsModFile(gamePath)) return base.HandleNewFile(newEntry, gamePath, absolutePath);
        return HandleNewModFile(newEntry, gamePath, absolutePath);
    }

    private async ValueTask<TempEntity> HandleNewModFile(DiskStateEntry newEntry, GamePath gamePath, AbsolutePath absolutePath)
    {
        var newFile = await base.HandleNewFile(newEntry, gamePath, absolutePath);

        // TODO: find the matching files
        newFile.Add(File.Loadout, EntityId.MinValue);
        newFile.Add(File.Mod, EntityId.MinValue);

        return newFile;
    }

    private static readonly RelativePath ModsDirectoryName = new("Mods");

    private static bool IsModFile(GamePath gamePath)
    {
        if (gamePath.LocationId != LocationId.Game) return false;
        var path = gamePath.Path;

        if (!path.StartsWith(ModsDirectoryName)) return false;

        // TODO
        return false;
    }
}
