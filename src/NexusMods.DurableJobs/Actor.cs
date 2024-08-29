using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace NexusMods.DurableJobs;

/// <summary>
/// The state of an actor.
/// </summary>
public enum ActorStatus : int
{
    Idle = 1,
    Occupied,
    Stopped
}

/// <summary>
/// An actor that processes messages sequentially.
/// </summary>
public class Actor<TState, TMessage>
    where TMessage : notnull
{
    private TState _state;
    
    /// <summary>
    /// The update function that processes messages.
    /// </summary>
    private readonly Func<Actor<TState, TMessage>, TState, TMessage, ValueTask<TState>> _update;
    
    /// <summary>
    /// Pending messages to be processed.
    /// </summary>
    private readonly ConcurrentQueue<TMessage> _messages = new();
    
    // Stored as int to allow for Interlocked operations
    private int _actorStatus = (int)ActorStatus.Idle;
    
    /// <summary>
    /// Debugger display for the actor status.
    /// </summary>
    private readonly ILogger _logger;
    
    /// <summary>
    /// Constructs a new actor with the given initial state and update function.
    /// </summary>
    public Actor(ILogger logger, TState initialState, Func<Actor<TState, TMessage>, TState, TMessage, ValueTask<TState>> update)
    {
        _logger = logger;
        _state = initialState;
        _update = update;
    }
    
    /// <summary>
    /// Send a message to the actor
    /// </summary>
    /// <param name="message"></param>
    public void Post(TMessage message)
    {
        _messages.Enqueue(message);
        Schedule();
    }

    /// <summary>
    /// The actor state. Don't mutate this value unless you really know what you're doing.
    /// </summary>
    public TState Dereference()
    {
        return _state;
    }

    private async Task Execute()
    {
        const int maxMessages = 100;
        
        var status = await DoIterations(maxMessages);
        if (status == ActorStatus.Stopped)
            return;

        Interlocked.Exchange(ref _actorStatus, (int)ActorStatus.Idle);
        
        if (!_messages.IsEmpty) 
            Schedule();
    }

    private async Task<ActorStatus> DoIterations(int iterations)
    {
        while (true)
        {
            if (Volatile.Read(ref _actorStatus) == (int)ActorStatus.Stopped)
                return ActorStatus.Stopped;

            if (iterations != 0)
            {
                if (_messages.TryDequeue(out var message))
                {
                    try
                    {
                        _state = await _update(this, _state, message);
                    }
                    catch (Exception e)
                    {
                        _logger.LogError(e, "Error processing message {Message}", message);
                    }
                    iterations--;
                }
                else
                {
                    return ActorStatus.Idle;
                }
            }
            else
            {
                return ActorStatus.Idle;
            }
        }
    }

    private void Schedule()
    {
        if (Interlocked.CompareExchange(ref _actorStatus, (int)ActorStatus.Occupied, (int)ActorStatus.Idle) == (int)ActorStatus.Idle)
        {
            Task.Run(Execute);
        }
    }
}
