namespace NexusMods.DataModel.Interprocess;

/// <summary>
/// A shared (multiprocess) array of 64-bit unsigned integers, supports atomic operations via a CAS operation
/// </summary>
public interface ISharedArray : IDisposable
{

    /// <summary>
    /// Get the ulong at the specified index
    /// </summary>
    /// <param name="idx"></param>
    /// <returns></returns>
    public ulong Get(int idx);

    /// <summary>
    /// Set the value at the given index to the given value if the current value is the expected value.
    /// Returns true if the value was set, false otherwise.
    /// </summary>
    /// <param name="idx">item index into the array</param>
    /// <param name="expected">the expected value</param>
    /// <param name="value">the value to replace it with</param>
    /// <returns></returns>
    public bool CompareAndSwap(int idx, ulong expected, ulong value);
}
