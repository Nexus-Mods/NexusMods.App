using System.Collections.Immutable;
using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.JsonConverters;
using NexusMods.DataModel.Sorting.Rules;

namespace NexusMods.DataModel.Loadouts;

[JsonName("NexusMods.DataModel.Mod")]
public record Mod : Entity, IHasEntityId<ModId>
{
    public required ModId Id { get; init; }
    public required EntityDictionary<ModFileId, AModFile> Files { get; init; }
    public required string Name { get; init; }
    public override EntityCategory Category => EntityCategory.Loadouts;
    public ImmutableList<ISortRule<Mod, ModId>> SortRules { get; init; } = ImmutableList<ISortRule<Mod, ModId>>.Empty;
}