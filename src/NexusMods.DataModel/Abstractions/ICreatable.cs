namespace NexusMods.DataModel.Abstractions;

/// <summary>
/// Interface that allows types to have a new instance created without having either
/// an instance or any arguments. This is most often used to create new instances of
/// Ids.
/// </summary>
/// <typeparam name="T"></typeparam>
public interface ICreatable<out T>
{
    public static abstract T Create();
}
