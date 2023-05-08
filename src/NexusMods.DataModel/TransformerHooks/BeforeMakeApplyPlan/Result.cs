using NexusMods.DataModel.TransformerHooks.BeforeSort;
using NexusMods.Hashing.xxHash64;
using NexusMods.Paths;

namespace NexusMods.DataModel.TransformerHooks.BeforeMakeApplyPlan;

/// <summary>
/// Represents the result of a transformer hook.
/// </summary>
public abstract class Result
{
    /// <summary>
    /// Instructs the transformer to do nothing.
    /// </summary>
    public static Result Nothing { get; } = new Nothing();
    
    /// <summary>
    /// Instructions the transformer to alter the cached size/hash of the file.
    /// </summary>
    /// <param name="size">If the size is null, the transformer will not alter the cached size otherwise or it will update the cached size to the specified value.</param>
    /// <param name="hash">If the hash is null, the transformer will not alter the cached hash otherwise it will update the cached hash to the specified value.</param>
    /// <returns></returns>
    public static Result SetSizeAndHash(Size? size = null, Hash? hash = null) 
        => new SetSizeAndHash {Size = size, Hash = hash};
}
