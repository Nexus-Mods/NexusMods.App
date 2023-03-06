namespace NexusMods.DataModel.Abstractions;

/// <summary>
/// Markers are helpful mutable classes for tracking the position of edits in an immutable tree.
/// A marker can be made for a sub portion of the tree, and the value can be read with `Value`,
/// while the sub-section of the tree can be modified via `Alter`.
/// </summary>
/// <typeparam name="T"></typeparam>
public interface IMarker<T>
{
    /// <summary>
    /// Provide a function that will transactionally update this marker (and all the branches)
    /// of this tree up to the top Root
    /// </summary>
    /// <param name="func"></param>
    public void Alter(Func<T, T> func);

    /// <summary>
    /// Get the current value of the subset of the tree pointed at by this marker.
    /// </summary>
    public T Value { get; }

}
