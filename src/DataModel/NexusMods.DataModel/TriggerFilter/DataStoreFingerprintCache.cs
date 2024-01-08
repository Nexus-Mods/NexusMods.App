using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.Abstractions.Ids;
using NexusMods.Hashing.xxHash64;

namespace NexusMods.DataModel.TriggerFilter;

/// <summary>
/// A cache for fingerprinted values, that persists to a data store
/// </summary>
/// <typeparam name="TSrc"></typeparam>
/// <typeparam name="TValue"></typeparam>
public class DataStoreFingerprintCache<TSrc, TValue> : IFingerprintCache<TSrc, TValue> where TValue : Entity {
    private readonly IDataStore _store;
    private readonly Hash _prefix;

    /// <summary>
    /// DI Constructor
    /// </summary>
    /// <param name="store"></param>
    public DataStoreFingerprintCache(IDataStore store)
    {
        _store = store;
        using var fp = Fingerprinter.Create();
        fp.Add(typeof(TSrc).Namespace ?? "");
        fp.Add(typeof(TSrc).Name);
        fp.Add(typeof(TValue).Namespace ?? "");
        fp.Add(typeof(TValue).Name);
        _prefix = fp.Digest();
    }


    /// <inheritdoc />
    public bool TryGet(Hash hash, out TValue value)
    {
        var ret = _store.Get<TValue>(GetId(hash));
        if (ret != null)
        {
            value = ret;
            return true;
        }
        value = null!;
        return false;

    }

    /// <inheritdoc />
    public void Set(Hash hash, TValue value)
    {
        _store.Put(GetId(hash), value);
    }

    private TwoId64 GetId(Hash hash)
    {
        return new TwoId64(EntityCategory.Loadouts, _prefix.Value, hash.Value);
    }
}
