using NexusMods.DataModel.Abstractions;

namespace NexusMods.DataModel;

public interface IVersionedParent
{
    void NotifyChildChanged(IVersionedObject child);
    void PersistChildren(IDataStore store);
}