using NexusMods.DataModel.Abstractions;

namespace NexusMods.DataModel;

public interface IDataStore
{
    public Id Store(IVersionedObject o);
}