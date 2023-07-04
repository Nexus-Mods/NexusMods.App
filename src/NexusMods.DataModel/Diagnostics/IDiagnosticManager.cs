using DynamicData;
using JetBrains.Annotations;
using NexusMods.DataModel.Abstractions.Ids;

namespace NexusMods.DataModel.Diagnostics;

/// <summary>
/// A diagnostic manager, which keeps track of all current diagnostics and refreshes them if necessary.
/// </summary>
[PublicAPI]
public interface IDiagnosticManager : IDisposable
{
    /// <summary>
    /// Gets the current active diagnostics.
    /// </summary>
    IObservable<IChangeSet<Diagnostic, IId>> ActiveDiagnostics { get; }
}
