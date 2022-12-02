using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.JsonConverters;
using NexusMods.Interfaces;

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

    public static ModList Empty(IDataStore store) => new()
    {
        ModListId = ModListId.Create(),
        Installation = GameInstallation.Empty,
        Name = "",
        Mods = EntityHashSet<Mod>.Empty(store),
        LastModified = DateTime.UtcNow,
        PreviousVersion = EntityLink<ModList>.Empty,
        Store = store
    };
}