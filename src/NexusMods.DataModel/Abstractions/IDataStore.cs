using System.Reactive.Disposables;

namespace NexusMods.DataModel.Abstractions;

public interface IDataStore
{
    public static readonly ThreadLocal<IDataStore?> CurrentStore = new();

    public static IDisposable WithCurrent(IDataStore dataStore)
    {
        CurrentStore.Value = dataStore;
        return Disposable.Create(() => CurrentStore.Value = null);
    }
    
    public Id Put<T>(T value) where T : Entity;
    T Get<T>(Id id) where T : Entity;
}