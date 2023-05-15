using NexusMods.DataModel.Abstractions;
using NexusMods.Hashing.xxHash64;

namespace NexusMods.DataModel.TriggerFilter;


/// <summary>
/// A cache for fingerprinted values
/// </summary>
/// <typeparam name="TSrc">Marker used to discriminate between various fingerprint sources</typeparam>
/// <typeparam name="TValue">Value stored and returned by this cache</typeparam>
public interface IFingerprintCache<TSrc, TValue> where TValue : Entity
{
    public bool TryGet(Hash hash, out TValue value);
    public void Set(Hash hash, TValue value);
}
