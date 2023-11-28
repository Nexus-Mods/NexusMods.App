using System.Collections.ObjectModel;
using NexusMods.DataModel.Activities;

namespace NexusMods.Abstractions.Activities;

/// <summary>
/// The central registry for all active activities in the process.
/// </summary>
public interface IActivityMonitor
{
    /// <summary>
    /// The collection of all active activities in the app.
    /// </summary>
    ReadOnlyObservableCollection<IReadOnlyActivity> Activities { get; }

}
