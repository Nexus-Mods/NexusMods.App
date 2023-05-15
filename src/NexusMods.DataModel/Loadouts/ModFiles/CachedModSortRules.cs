using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.JsonConverters;
using NexusMods.DataModel.Loadouts.Mods;
using NexusMods.DataModel.Sorting.Rules;

namespace NexusMods.DataModel.Loadouts.ModFiles;

/// <summary>
/// Cached sort rules for a mod. 
/// </summary>
[JsonName("NexusMods.DataModel.Loadouts.ModFiles.CachedModSortRules")]
public record CachedModSortRules : Entity
{
    /// <summary>
    /// The cached sort rules
    /// </summary>
    public required ISortRule<Mod, ModId>[] Rules { get; init; }
    public override EntityCategory Category => EntityCategory.Fingerprints;
}
