using DynamicData;
using JetBrains.Annotations;

namespace NexusMods.DataModel.Diagnostics;

/// <summary>
/// A diagnostic manager, which keeps track of all current diagnostics and refreshes them if necessary.
/// </summary>
[PublicAPI]
public interface IDiagnosticManager
{
    /// <summary>
    /// Gets the current active diagnostics.
    /// </summary>
    IObservable<IChangeSet<Diagnostic, Guid>> ActiveDiagnostics { get; }
}
