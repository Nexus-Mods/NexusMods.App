using System.Collections.Concurrent;
using System.Reactive.Subjects;
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
public abstract class Actor<TState, TMessage> : IActor<TMessage>
    where TMessage : notnull
{
    public IObservable<ActorStatus> StatusObservable => _statusSubject; 
    
    private Subject<ActorStatus> _statusSubject = new();
    
    private TState _state;
    
    /// <summary>
    /// This is pretty dangerous to expose, but it's used for several reporting purposes.
    /// </summary>
    internal TState State => _state;
    
    /// <summary>
    /// Pending messages to be processed.
    /// </summary>
    private readonly ConcurrentQueue<TMessage> _messages = new();
    
    // Stored as int to allow for Interlocked operations
    private int _actorStatus = (int)ActorStatus.Idle;

    private void SendStatusUpdate()
    {
        _statusSubject.OnNext((ActorStatus)_actorStatus);
    }
    
    /// <summary>
    /// Debugger display for the actor status.
    /// </summary>
    private readonly ILogger _logger;
    
    /// <summary>
    /// Constructs a new actor with the given initial state and update function.
    /// </summary>
    public Actor(ILogger logger, TState initialState)
    {
        _logger = logger;
        _state = initialState;
    }

    /// <summary>
    /// Process the message and return the new state, and whether the actor should continue processing messages (if it's dead, for example).
    /// </summary>
    /// <param name="message"></param>
    /// <returns></returns>
    public abstract ValueTask<(TState, bool)> Handle(TState state, TMessage message);
    
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
        {
            _actorStatus = (int)ActorStatus.Stopped;
            SendStatusUpdate();
            return;
        }

        Interlocked.Exchange(ref _actorStatus, (int)ActorStatus.Idle);
        SendStatusUpdate();
        
        if (!_messages.IsEmpty) 
            Schedule();
    }

    private async Task<ActorStatus> DoIterations(int iterations)
    {
        bool shouldContinue = true;
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
                        (_state, shouldContinue) = await Handle(_state, message);
                        if (!shouldContinue)
                            return ActorStatus.Stopped;
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
            SendStatusUpdate();
            Task.Run(Execute);
        }
    }
}
