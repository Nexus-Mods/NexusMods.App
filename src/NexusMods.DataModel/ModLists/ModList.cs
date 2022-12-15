using System.Collections;
using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.JsonConverters;
using NexusMods.Interfaces;
using NexusMods.Paths;

namespace NexusMods.DataModel.ModLists;

[JsonName("NexusMods.DataModel.ModList")]
public record ModList : Entity, IEmptyWithDataStore<ModList>
{
    
    public required EntityHashSet<Mod> Mods { get; init; }
    public required ModListId ModListId { get; init; }
    
    public required string Name { get; init; }
    public required GameInstallation Installation { get; init; }
    public required DateTime LastModified { get; init; }
    public required EntityLink<ModList> PreviousVersion { get; init; }
    
    public override EntityCategory Category => EntityCategory.ModLists;
    public required string ChangeMessage { get; init; } = "";

    public static ModList Empty(IDataStore store) => new()
    {
        ModListId = ModListId.Create(),
        Installation = GameInstallation.Empty,
        Name = "",
        Mods = EntityHashSet<Mod>.Empty(store),
        LastModified = DateTime.UtcNow,
        PreviousVersion = EntityLink<ModList>.Empty(store),
        ChangeMessage = "",
        Store = store
    };

    public ModList RemoveFileFromAllMods(Func<AModFile, bool> filter)
    {
        var newMods = Mods.Keep(mod => mod with { Files = mod.Files.Keep(f => filter(f) ? null : f)});
        return this with
        {
            Mods = newMods
        };
    }

    public ModList KeepMod(Mod tMod, Func<Mod, Mod?> func)
    {
        return this with
        {
            Mods = Mods.Keep(m => m.Name == tMod.Name ? func(m) : m)
        };
    }
}