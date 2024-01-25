namespace NexusMods.Abstractions.Serialization.DataModel;

/// <summary>
/// An interface used to allow a user to apply a function over all of the items in a collection.
/// </summary>
/// <typeparam name="TItem">The type of item returned by walking.</typeparam>
public interface IWalkable<out TItem>
{
    /// <summary>
    /// Goes over each item in the collection, firing your callback function.<br/>
    /// A 'state' parameter can be specified, which can hold shared state between individual callback invocations.<br/><br/>
    ///
    /// This method effectively is equivalent to LINQ's <see cref="Enumerable.Aggregate{TSource}"/>.
    /// </summary>
    /// <param name="visitor">The function to call for each item in the collection.</param>
    /// <param name="initial">The item that holds the state.</param>
    /// <typeparam name="TState">The initial state.</typeparam>
    public TState Walk<TState>(Func<TState, TItem, TState> visitor, TState initial);
}
