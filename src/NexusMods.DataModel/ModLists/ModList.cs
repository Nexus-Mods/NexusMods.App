using NexusMods.DataModel.Abstractions;

namespace NexusMods.DataModel.ModLists;

public record ModList(
    EntityHashSet<Mod> Mods,
    ModListId ModListId,
    string Name,
    DateTime LastModified,
    EntityLink<ModList> PreviousVersion) : Entity, ICreatable<ModList>
{
    public override EntityCategory Category => EntityCategory.ModLists;
    public static ModList Create() => new(EntityHashSet<Mod>.Empty, 
        ModListId.Create(),  
        "", 
        DateTime.UtcNow, 
        EntityLink<ModList>.Empty);
}