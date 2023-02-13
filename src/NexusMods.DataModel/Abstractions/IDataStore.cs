namespace NexusMods.DataModel.Abstractions;

public interface IDataStore
{
    public Id Put<T>(T value) where T : Entity;
    public void Put<T>(Id id, T value) where T : Entity;
    T? Get<T>(Id id, bool canCache = false) where T : Entity;

    bool PutRoot(RootType type, Id oldId, Id newId);
    Id? GetRoot(RootType type);
    byte[]? GetRaw(Id id);
    void PutRaw(Id key, ReadOnlySpan<byte> val);
    Task<long> PutRaw(IAsyncEnumerable<(Id Key, byte[] Value)> kvs, CancellationToken token = default);
    IEnumerable<T> GetByPrefix<T>(Id prefix) where T : Entity;
    IObservable<(Id Id, Entity Entity)> Changes { get; }
}