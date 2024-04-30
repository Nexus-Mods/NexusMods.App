using NexusMods.Abstractions.DataModel.Entities.Sorting;
using NexusMods.Abstractions.Loadouts.Mods;
using NexusMods.Abstractions.NexusWebApi.Types;
using NexusMods.Abstractions.Serialization.Attributes;
using NexusMods.Abstractions.Serialization.DataModel;

namespace NexusMods.DataModel.Loadouts.Mods;

/// <summary>
/// Cached sort rules for a mod. 
/// </summary>
[JsonName("NexusMods.DataModel.Loadouts.ModFiles.CachedModSortRules")]
public record CachedModSortRules : Entity
{
    /// <summary>
    /// The cached sort rules
    /// </summary>
    public required ISortRule<Mod.Model, ModId>[] Rules { get; init; }

    /// <inheritdoc />
    public override EntityCategory Category => EntityCategory.Fingerprints;
}
