using NexusMods.Abstractions.Serialization.DataModel;
using NexusMods.Hashing.xxHash64;

namespace NexusMods.DataModel.TriggerFilter;


/// <summary>
/// A cache for fingerprinted values
/// </summary>
/// <typeparam name="TSrc">Marker used to discriminate between various fingerprint sources</typeparam>
/// <typeparam name="TValue">Value stored and returned by this cache</typeparam>
public interface IFingerprintCache<TSrc, TValue> where TValue : Entity
{
    /// <summary>
    /// Try and get the cached value for a given fingerprint
    /// </summary>
    /// <param name="hash"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    public bool TryGet(Hash hash, out TValue value);
    
    /// <summary>
    /// Set the value for a given fingerprint in the cache
    /// </summary>
    /// <param name="hash"></param>
    /// <param name="value"></param>
    public void Set(Hash hash, TValue value);
}
