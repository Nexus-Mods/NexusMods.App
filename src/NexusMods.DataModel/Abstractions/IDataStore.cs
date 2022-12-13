using System.Reactive.Disposables;
using NexusMods.DataModel.ArchiveContents;

namespace NexusMods.DataModel.Abstractions;

public interface IDataStore
{
    public Id Put<T>(T value) where T : Entity;
    public void Put<T>(Id id, T value) where T : Entity;
    T? Get<T>(Id id) where T : Entity;

    bool PutRoot(RootType type, Id oldId, Id newId);
    Id? GetRoot(RootType type);
    byte[]? GetRaw(ReadOnlySpan<byte> key, EntityCategory fileHashes);
    void PutRaw(ReadOnlySpan<byte> kSpan, ReadOnlySpan<byte> vSpan, EntityCategory fileHashes);
    IEnumerable<T> GetByPrefix<T>(Id prefix) where T : Entity;
}