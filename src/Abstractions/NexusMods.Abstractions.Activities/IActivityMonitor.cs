using System.Collections.ObjectModel;

namespace NexusMods.Abstractions.Activities;

/// <summary>
/// The central registry for all active activities in the process.
/// </summary>
[Obsolete(message: "To be replaced with Jobs")]
public interface IActivityMonitor
{
    /// <summary>
    /// The collection of all active activities in the app.
    /// </summary>
    ReadOnlyObservableCollection<IReadOnlyActivity> Activities { get; }

}
