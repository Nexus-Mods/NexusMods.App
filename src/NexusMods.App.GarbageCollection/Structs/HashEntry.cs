namespace NexusMods.App.GarbageCollection.Structs;

/// <summary>
///     This is a tuple which stores the hash of a file and its reference count
///     for a file in an archive.
/// </summary>
internal struct HashEntry
{
    /// <summary>
    ///     The hash of the entry in the archive.
    /// </summary>
    public required Hash Hash { get; init; }

    /// <summary>
    ///     Number of references to this specific hash.
    /// </summary>
    private int _refCount;

    /// <summary>
    ///     Increments the reference counter of this archive.
    /// </summary>
    public int IncrementRefCount() => Interlocked.Increment(ref _refCount);

    /// <summary>
    ///     Returns the reference counter of this archive.
    /// </summary>
    public int GetRefCount() => _refCount;

    public static implicit operator HashEntry(Hash hash) => new()
    {
        Hash = hash,
    };
}
