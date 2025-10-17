using System.Reactive.Linq;
using DynamicData;
using NexusMods.Abstractions.Games;
using NexusMods.Sdk.Jobs;

namespace NexusMods.App.UI;

/// <summary>
/// This class helps us efficiently identify whether a game is currently running.
/// </summary>
/// <remarks>
///     There are multiple places in the App where it's necessary to determine if
///     there is already a game running; however the necessary calculation for this
///     can be prohibitively expensive. Therefore we re-use the logic inside this class.
/// </remarks>
public class GameRunningTracker
{
    private readonly IObservable<bool> _observable;
    
    /// <summary>
    /// Retrieves the current state of the game running tracker,
    /// with the current state being immediately emitted as the first item.
    /// </summary>
    public IObservable<bool> GetWithCurrentStateAsStarting() => _observable;

    public GameRunningTracker(IJobMonitor monitor)
    {
        // Note(sewer): Yes, this technically can lead to a race condition;
        // however it's not possible to start a game before activating GameRunningTracker 
        // singleton for an end user.
        var numRunning = monitor.Jobs.Count(x => x is { Definition: IRunGameTool } && x.Status.IsActive());
        _observable = monitor.GetObservableChangeSet<IRunGameTool>()
            .TransformOnObservable(job => job.ObservableStatus)
            .QueryWhenChanged(query => query.Items.Any(x => x.IsActive()))
            .StartWith(numRunning > 0)
            .Replay(1)
            .RefCount();
    }
}
