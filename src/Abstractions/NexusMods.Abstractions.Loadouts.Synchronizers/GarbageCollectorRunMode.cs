namespace NexusMods.Abstractions.Loadouts.Synchronizers;

/// <summary>
/// Defines the mode used to run the Garbage Collector as part of deletion of a loadout.
/// </summary>
public enum GarbageCollectorRunMode
{
    /// <summary>
    /// Don't run the garbage collector at all.
    /// </summary>
    DoNotRun,
    
    /// <summary>
    /// Runs the garbage collector synchronously, blocking the completion
    /// of the deletion operation until the garbage collector has finished.
    /// </summary>
    RunSynchronously,
    
    /// <summary>
    /// Runs the garbage collector in background, without waiting for it to
    /// complete.
    /// </summary>
    RunInBackground,
}
