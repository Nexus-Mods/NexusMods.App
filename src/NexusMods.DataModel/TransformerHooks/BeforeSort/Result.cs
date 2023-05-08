using NexusMods.DataModel.Loadouts;
using NexusMods.DataModel.Sorting.Rules;

namespace NexusMods.DataModel.TransformerHooks.BeforeSort;

/// <summary>
/// Result of the transformer hook.
/// </summary>
public abstract class Result
{
    /// <summary>
    /// The transformer does not need to do anything to this mod
    /// </summary>
    public static Result Nothing { get; } = new NoChanges();
    /// <summary>
    /// This mod should not be included in the rest of the sorting process.
    /// </summary>
    public static Result DisableMod { get; } = new DisableMod();
    
    /// <summary>
    /// Replace the sorting rules with the specified rules, but only for this sorting operation.
    /// </summary>
    /// <param name="newRules"></param>
    /// <returns></returns>
    public static Result ReplaceRules(IEnumerable<ISortRule<Mod, ModId>> newRules) => new ReplaceRules {Rules = newRules};
}
