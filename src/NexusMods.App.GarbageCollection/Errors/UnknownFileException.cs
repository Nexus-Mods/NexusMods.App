using NexusMods.App.GarbageCollection.Structs;
namespace NexusMods.App.GarbageCollection.Errors;

/// <summary>
///     This exception is thrown when the ref count of a file not known by
///     the garbage collector is being increased. This indicates a bug in the
///     core application.
/// </summary>
public class UnknownFileException : Exception
{
    /// <summary/>
    /// <param name="hash">The hash that was not found by the GC.</param>
    public UnknownFileException(Hash hash)
        : base($"File with hash {hash.Value:X2} has not been previously added to the Garbage Collector. This is indicative of a bug. Either an existing archive was not added to the GC (via ' AddArchive '), or the (DataStore) item being added via ' AddReferencedFile ' is not in any archive.") { }
}
