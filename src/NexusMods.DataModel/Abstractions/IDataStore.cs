using System.Reactive.Disposables;

namespace NexusMods.DataModel.Abstractions;

public interface IDataStore
{
    public static readonly ThreadLocal<IDataStore?> CurrentStore = new();

    public static IDisposable WithCurrent(IDataStore dataStore)
    {
        var prevVal = CurrentStore.Value;
        CurrentStore.Value = dataStore;
        return Disposable.Create(() => CurrentStore.Value = prevVal);
    }
    
    public Id Put<T>(T value) where T : Entity;
    T Get<T>(Id id) where T : Entity;

    bool PutRoot(RootType type, Id oldId, Id newId);
    Id? GetRoot(RootType type);
    byte[] GetRaw(ReadOnlySpan<byte> key, EntityCategory fileHashes);
    void PutRaw(ReadOnlySpan<byte> kSpan, ReadOnlySpan<byte> vSpan, EntityCategory fileHashes);
}