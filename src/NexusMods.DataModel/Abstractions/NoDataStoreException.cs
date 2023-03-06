namespace NexusMods.DataModel.Abstractions;

/// <summary>
/// Exception thrown for any object which lacks a backing data store.
/// </summary>
public class NoDataStoreException : Exception
{
    /// <summary>
    /// Throws an exception for an object which lacks a data store.
    /// </summary>
    public NoDataStoreException() : base("Cannot create entity object without bound data store") { }
}
