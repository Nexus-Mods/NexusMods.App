using System.Collections;
using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.Games;
using NexusMods.DataModel.JsonConverters;
using NexusMods.Interfaces;
using NexusMods.Paths;

namespace NexusMods.DataModel.Loadouts;

[JsonName("NexusMods.DataModel.Loadout")]
public record Loadout : Entity, IEmptyWithDataStore<Loadout>
{
    
    public required EntityDictionary<ModId, Mod> Mods { get; init; }
    public required LoadoutId LoadoutId { get; init; }
    
    public required string Name { get; init; }
    public required GameInstallation Installation { get; init; }
    public required DateTime LastModified { get; init; }
    public required EntityLink<Loadout> PreviousVersion { get; init; }
    
    public override EntityCategory Category => EntityCategory.Loadouts;
    public required string ChangeMessage { get; init; } = "";

    public static Loadout Empty(IDataStore store) => new()
    {
        LoadoutId = LoadoutId.Create(),
        Installation = GameInstallation.Empty,
        Name = "",
        Mods = EntityDictionary<ModId, Mod>.Empty(store),
        LastModified = DateTime.UtcNow,
        PreviousVersion = EntityLink<Loadout>.Empty(store),
        ChangeMessage = "",
        Store = store
    };
    

    public Loadout RemoveFileFromAllMods(Func<AModFile, bool> filter)
    {
        var newMods = Mods.Keep(mod => mod with { Files = mod.Files.Keep(f => filter(f) ? null : f)});
        return this with
        {
            Mods = newMods
        };
    }

    public Loadout KeepMod(Mod tMod, Func<Mod, Mod?> func)
    {
        return this with
        {
            Mods = Mods.Keep(m => m.Name == tMod.Name ? func(m) : m)
        };
    }
    
    public Loadout Alter(ModId modId, Func<Mod?, Mod?> func)
    {
        return this with
        {
            Mods = Mods.Keep(modId, func)
        };
    }

    public Loadout Alter(ModId modId, ModFileId fileId, Func<AModFile?, AModFile?> func)
    {
        return this with
        {
            Mods = Mods.Keep(modId, m => m with
            {
                Files = m.Files.Keep(fileId, func)
            })
        };
    }

    public Loadout Remove(ModId modId)
    {
        return this with
        {
            Mods = Mods.Keep(modId, _ => null)
        };
    }

    public Loadout Remove(ModId modId, ModFileId fileId)
    {
        return this with
        {
            Mods = Mods.Keep(modId, m => m with
            {
                Files = m.Files.Keep(fileId, _ => null)
            })
        };
    }

    public Loadout Add(Mod mod)
    {
        return this with
        {
            Mods = Mods.With(mod.Id, mod)
        };
    }
    
    public Loadout Add(ModId modId, AModFile file)
    {
        return this with
        {
            Mods = Mods.Keep(modId, m => m with
            {
                Files = m.Files.With(file.Id, file)
            })
        };
    }

    public Loadout AlterFiles(Func<AModFile, AModFile?> func)
    {
        return this with
        {
            Mods = Mods.Keep(m => m with
            {
                Files = m.Files.Keep(func)
            })
        };
    }
}