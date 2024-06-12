using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Loadouts.Mods;
using NexusMods.Abstractions.Loadouts.Synchronizers;
using NexusMods.Abstractions.MnemonicDB.Attributes;
using File = NexusMods.Abstractions.Loadouts.Files.File;

namespace NexusMods.Games.BethesdaGameStudios;

public class BethesdaLoadoutSynchronizer(IServiceProvider provider) : ALoadoutSynchronizer(provider)
{
    public override async Task<Loadout.ReadOnly> CreateLoadout(GameInstallation installation, string? suggestedName = null)
    {
        var loadout = await base.CreateLoadout(installation, suggestedName);
        return await FixupLoadout(loadout);
    }

    /// <summary>
    /// Patches the loadout to ensure it has the required metadata mod and plugin order file.
    /// </summary>
    /// <param name="loadout"></param>
    /// <returns></returns>
    protected virtual async ValueTask<Loadout.ReadOnly> FixupLoadout(Loadout.ReadOnly loadout)
    {
        var metadataMod = loadout.Mods
            .FirstOrDefault(m => m.Category == ModCategory.Metadata);
        
        if (metadataMod.IsValid())
        {
            using var tx = Connection.BeginTransaction();
            var newMetadataMod = new Mod.New(tx)
            {
                Name = "Modding Metadata",
                Category = ModCategory.Metadata,
                Enabled = true,
                LoadoutId = loadout,
                Revision = 0,
                Status = ModStatus.Installed,
            };
            loadout.Revise(tx);
            var result = await tx.Commit();
            metadataMod = newMetadataMod.Remap(result);
        }

        
        var mod = metadataMod.Files
            .FirstOrDefault(f => f.TryGetAsGeneratedFile(out _));

        if (!mod.IsValid())
        {
            using var tx = Connection.BeginTransaction(); 
            var generated = new GeneratedFile.New(tx)
            {
                File = new File.New(tx)
                {
                    LoadoutId = loadout,
                    ModId = metadataMod,
                    To = PluginOrderFile.Path,
                },
                Generator = PluginOrderFile.Guid,
            };
            metadataMod.Revise(tx);
            var result = await tx.Commit(); 
            loadout = result.Remap(loadout);
        }

        return loadout;
    }
}
