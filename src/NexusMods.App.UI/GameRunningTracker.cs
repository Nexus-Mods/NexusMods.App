using System.Reactive.Linq;
using DynamicData;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.Jobs;
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
    /// <summary>
    /// Allows you to listen to when any game is running via the App.
    /// </summary>
    private readonly IObservable<bool> _isGameRunning;

    /// <summary>
    /// Number of currently running game jobs.
    /// </summary>
    private int _numRunningJobs = 0;

    /// <summary>
    /// Retrieves the current state of the game running tracker,
    /// with the current state being immediately emitted as the first item.
    /// </summary>
    public IObservable<bool> GetWithCurrentStateAsStarting() => _isGameRunning.StartWith(GetInitialRunningState());

    /// <summary>
    /// Gets the initial state, being `true` if we're running something, and `false` otherwise.
    /// </summary>
    public bool GetInitialRunningState() => _numRunningJobs > 0;

    public GameRunningTracker(IJobMonitor monitor)
    {
        // Note(sewer): Yes, this technically can lead to a race condition;
        // however it's not possible to start a game before activating GameRunningTracker 
        // singleton for an end user.
        _numRunningJobs = monitor.Jobs.Count(x => x is { Definition: IRunGameTool } && x.Status.IsActive());
        _isGameRunning = monitor.GetObservableChangeSet<IRunGameTool>()
            .TransformOnObservable(job => job.ObservableStatus)
            .Select(changes =>
                {
                    // Note(sewer): We don't currently remove any old/stale jobs, so it's inefficient to
                    // check the whole job set in case the App has been running for a long time.
                    // Therefore, we instead count and manually maintain a running job count.
                    foreach (var change in changes)
                    {
                        var isActivated = change.Current.WasActivated(change.Previous);
                        var isDeactivated = change.Current.WasDeactivated(change.Previous);
                        if (isActivated)
                            _numRunningJobs++;
                        if (isDeactivated)
                            _numRunningJobs--;
                    }

                    return _numRunningJobs > 0;
                }
            )
            .DistinctUntilChanged();
    }
}
