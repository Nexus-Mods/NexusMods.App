using NexusMods.DataModel.Abstractions;

namespace NexusMods.DataModel.ModLists;

public record ModList(
    EntityHashSet<Mod> Mods,
    string Name,
    DateTime LastModified,
    EntityLink<ModList> PreviousVersion) : Entity, IEmpty<ModList>
{
    public ModList() : this(
        Mods: EntityHashSet<Mod>.Empty, 
        Name: "", 
        LastModified: DateTime.UtcNow,
        PreviousVersion: EntityLink<ModList>.Empty)
    {
        
    }
    public override EntityCategory Category => EntityCategory.ModLists;
    public static ModList Empty => new(EntityHashSet<Mod>.Empty, "", DateTime.UtcNow, EntityLink<ModList>.Empty);
}