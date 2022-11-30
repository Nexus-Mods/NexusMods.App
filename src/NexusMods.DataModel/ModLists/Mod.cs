using NexusMods.DataModel.Abstractions;

namespace NexusMods.DataModel.ModLists;

public record Mod(EntityHashSet<AModFile> Files, string Name) : Entity
{
    public override EntityCategory Category => EntityCategory.ModLists;
}