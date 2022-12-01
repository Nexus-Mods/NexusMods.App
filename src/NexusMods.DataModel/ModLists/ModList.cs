using NexusMods.DataModel.Abstractions;
using NexusMods.Interfaces;

namespace NexusMods.DataModel.ModLists;

public record ModList(
    EntityHashSet<Mod> Mods,
    ModListId ModListId,
    string Name,
    GameInstallation Installation,
    DateTime LastModified,
    EntityLink<ModList> PreviousVersion) : Entity, ICreatable<ModList>
{
    public override EntityCategory Category => EntityCategory.ModLists;
    public static ModList Create() => new(EntityHashSet<Mod>.Empty, 
        ModListId.Create(),
        "", 
        GameInstallation.Empty, 
        DateTime.UtcNow, 
        EntityLink<ModList>.Empty);
}