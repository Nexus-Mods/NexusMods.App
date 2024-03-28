using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Loadouts.Mods;
using NexusMods.Abstractions.Loadouts.Synchronizers;
using NexusMods.Abstractions.Serialization.DataModel;

namespace NexusMods.Games.BethesdaGameStudios;

public class BethesdaLoadoutSynchronizer : ALoadoutSynchronizer
{
    public BethesdaLoadoutSynchronizer(IServiceProvider provider) : base(provider) { }

    public override async Task<Loadout> Manage(GameInstallation installation, string? suggestedName = null)
    {
        var loadout = await base.Manage(installation, suggestedName);
        return FixupLoadout(loadout);
    }

    /// <summary>
    /// Patches the loadout to ensure it has the required metadata mod and plugin order file.
    /// </summary>
    /// <param name="loadout"></param>
    /// <returns></returns>
    protected virtual Loadout FixupLoadout(Loadout loadout)
    {
        var gameMod = loadout.Mods.Where(m => m.Value.ModCategory == Mod.ModdingMetaData)
            .Select(m => m.Value)
            .FirstOrDefault();

        ModId metadataModId;
        if (gameMod == default)
        {
            metadataModId = ModId.NewId();
            loadout = loadout with
            {
                Mods = loadout.Mods.With(metadataModId, new Mod()
                {
                    Id = metadataModId,
                    Name = "Modding Metadata",
                    ModCategory = Mod.ModdingMetaData,
                    Files = EntityDictionary<ModFileId, AModFile>.Empty(loadout.Mods.Store),
                    Enabled = true
                })
            };
        }
        else
        {
            metadataModId = gameMod.Id;
        }

        gameMod = loadout.Mods[metadataModId];
        var pluginFile = gameMod.Files.Values.OfType<PluginOrderFile>().FirstOrDefault();

        if (pluginFile == default)
        {
            pluginFile = new PluginOrderFile { Id = ModFileId.NewId() };
            gameMod = gameMod with
            {
                Files = gameMod.Files.With(pluginFile.Id, pluginFile)
            };
            loadout = loadout with
            {
                Mods = loadout.Mods.With(metadataModId, gameMod)
            };
        }

        return loadout;
    }
}
