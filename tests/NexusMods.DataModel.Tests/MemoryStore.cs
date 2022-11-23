using NexusMods.DataModel.Abstractions;

namespace NexusMods.DataModel.Tests;

public class MemoryStore : IDataStore
{
    public Id Store(IVersionedObject o)
    {
        throw new NotImplementedException();
    }
}