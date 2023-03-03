namespace NexusMods.DataModel.Abstractions;

/// <summary>
/// Provides access to creating an empty element with a backing datastore,
/// </summary>
/// <typeparam name="TSelf">Empty item to return. Usually own/self class.</typeparam>
public interface IEmptyWithDataStore<out TSelf>
{
    /// <summary>
    /// Returns a new empty item for use.
    /// </summary>
    public static abstract TSelf Empty(IDataStore store);
}
