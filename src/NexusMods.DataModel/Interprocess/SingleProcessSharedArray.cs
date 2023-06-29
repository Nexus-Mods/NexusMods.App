namespace NexusMods.DataModel.Interprocess;

/// <summary>
/// A shared array of 64-bit unsigned integers, supports atomic operations via a CAS operation, for use in a single process
/// and mostly for testing purposes.
/// </summary>
public class SingleProcessSharedArray : ISharedArray
{
    private readonly ulong[] _slots;

    /// <summary>
    /// Create a new shared array with the given number of items
    /// </summary>
    /// <param name="slots"></param>
    public SingleProcessSharedArray(int slots)
    {
        _slots = new ulong[slots];
    }
    
    /// <inheritdoc />
    public void Dispose()
    {
        // Nothing to do
    }

    /// <inheritdoc />
    public ulong Get(int idx)
    {
        return _slots[idx];
    }
    
    /// <inheritdoc />
    public bool CompareAndSwap(int idx, ulong expected, ulong value)
    {
        var span = _slots.AsSpan();
        return Interlocked.CompareExchange(ref span[idx], value, expected) == expected;
    }
}
