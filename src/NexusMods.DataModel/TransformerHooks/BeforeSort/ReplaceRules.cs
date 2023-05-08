using NexusMods.DataModel.Loadouts;
using NexusMods.DataModel.Sorting.Rules;

namespace NexusMods.DataModel.TransformerHooks.BeforeSort;

/// <summary>
/// Specifies that the transformer will replace the sorting rules with the specified rules.
/// This replacement is transient and only applies to the current sorting operation.
/// </summary>
public class ReplaceRules : Result
{
    /// <summary>
    /// New sorting rules to replace the existing ones.
    /// </summary>
    public required IEnumerable<ISortRule<Mod, ModId>> Rules { get; init; }
}
