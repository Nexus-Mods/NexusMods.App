namespace NexusMods.DurableJobs;

/// <summary>
/// An abstract actor that can receive messages of a given type.
/// </summary>
public interface IActor<TMessage> where TMessage : notnull
{
    /// <summary>
    /// Send a message to the actor.
    /// </summary>
    public void Post(TMessage message);
}
