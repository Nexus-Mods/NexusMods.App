namespace NexusMods.DataModel.Abstractions;

public interface IEmpty<out T>
{
    public static abstract T Empty { get; }
}