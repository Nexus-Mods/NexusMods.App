using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.JsonConverters;

namespace NexusMods.DataModel.ModLists;

[JsonName("NexusMods.DataModel.ListRegistry")]
public record Mod : Entity
{
    public required EntityHashSet<AModFile> Files { get; init; }
    public required string Name { get; init; }
    public override EntityCategory Category => EntityCategory.ModLists;
}