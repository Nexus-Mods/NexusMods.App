using System.Collections.Immutable;
using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.JsonConverters;
using NexusMods.DataModel.Sorting;
using NexusMods.DataModel.Sorting.Rules;

namespace NexusMods.DataModel.Loadouts;

[JsonName("NexusMods.DataModel.ListRegistry")]
public record Mod : Entity, IHasEntityId<ModId>
{
    public required ModId Id { get; init; }
    public required EntityHashSet<AModFile> Files { get; init; }
    public required string Name { get; init; }
    public override EntityCategory Category => EntityCategory.Loadouts;
    
    public ImmutableHashSet<ISortRule<Mod, ModId>> SortRules { get; init; } = ImmutableHashSet<ISortRule<Mod, ModId>>.Empty;
    
}