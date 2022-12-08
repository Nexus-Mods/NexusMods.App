namespace NexusMods.DataModel.Abstractions;

public interface IEmptyWithDataStore<out T>
{
    public static abstract T Empty(IDataStore store);
}