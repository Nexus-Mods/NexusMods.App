namespace NexusMods.DataModel.Abstractions;

public interface IWalkable<out TItem>
{
    public TState Walk<TState>(Func<TState, TItem, TState> visitor, TState initial);
}