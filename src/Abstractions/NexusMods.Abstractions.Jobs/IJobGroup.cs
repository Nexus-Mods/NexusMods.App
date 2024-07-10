using System.Collections.ObjectModel;
using JetBrains.Annotations;

namespace NexusMods.Abstractions.Jobs;

/// <summary>
/// Represents a job of <see cref="IJob"/>.
/// </summary>
[PublicAPI]
public interface IJobGroup : IJob, IReadOnlyList<IJob>
{
    /// <summary>
    /// Gets the read-only observable collection for the underlying jobs list.
    /// </summary>
    ReadOnlyObservableCollection<IJob> ObservableCollection { get; }
}
