using NexusMods.Hashing.xxHash64;
using NexusMods.Paths;

namespace NexusMods.DataModel.TransformerHooks.BeforeMakeApplyPlan;

/// <summary>
/// Instructs the transformer to alter the cached size/hash of the file
/// </summary>
public class SetSizeAndHash : Result
{

    /// <summary>
    /// If the size is null, the transformer will not alter the cached size otherwise
    /// or it will update the cached size to the specified value.
    /// </summary>
    public Size? Size {get; init;}
    
    /// <summary>
    /// If the hash is null, the transformer will not alter the cached hash otherwise
    /// it will update the cached hash to the specified value.
    /// </summary>
    public Hash? Hash { get; init; }
}
