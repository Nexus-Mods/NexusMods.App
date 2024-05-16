using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Loadouts.Mods;
using NexusMods.Abstractions.Loadouts.Synchronizers;

namespace NexusMods.Games.BethesdaGameStudios;

public class BethesdaLoadoutSynchronizer(IServiceProvider provider) : ALoadoutSynchronizer(provider)
{
    public override async Task<Loadout.Model> CreateLoadout(GameInstallation installation, string? suggestedName = null)
    {
        var loadout = await base.CreateLoadout(installation, suggestedName);
        return await FixupLoadout(loadout);
    }

    /// <summary>
    /// Patches the loadout to ensure it has the required metadata mod and plugin order file.
    /// </summary>
    /// <param name="loadout"></param>
    /// <returns></returns>
    protected virtual async ValueTask<Loadout.Model> FixupLoadout(Loadout.Model loadout)
    {
        var metadataMod = loadout.Mods
            .FirstOrDefault(m => m.Category == ModCategory.Metadata);
        
        if (metadataMod == null)
        {
            using var tx = Connection.BeginTransaction();
            metadataMod = new Mod.Model(tx)
            {
                Name = "Modding Metadata",
                Category = ModCategory.Metadata,
                Enabled = true,
                Loadout = loadout,
            };
            loadout.Revise(tx);
            var result = await tx.Commit();
            metadataMod = result.Remap(metadataMod);
        }

        
        var mod = metadataMod.Files
            .FirstOrDefault(f => f.IsGeneratedFile<PluginOrderFile>());

        if (mod == null)
        {
            using var tx = Connection.BeginTransaction(); 
            var generated = new GeneratedFile.Model(tx)
            {
                Loadout = loadout,
                Mod = metadataMod,
                To = PluginOrderFile.Path,
            };
            generated.SetGenerator<PluginOrderFile>();
            metadataMod.Revise(tx);
            var result = await tx.Commit(); 
            loadout = result.Remap(loadout);
        }

        return loadout;
    }
}
