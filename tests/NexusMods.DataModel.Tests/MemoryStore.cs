using NexusMods.DataModel.Abstractions;

namespace NexusMods.DataModel.Tests;

public class MemoryStore : IDataStore
{
    public Id Put<T>(T value) where T : Entity
    {
        throw new NotImplementedException();
    }

    public T Get<T>(Id id) where T : Entity
    {
        throw new NotImplementedException();
    }
}