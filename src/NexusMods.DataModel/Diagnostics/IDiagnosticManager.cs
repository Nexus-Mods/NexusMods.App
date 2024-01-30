using DynamicData;
using JetBrains.Annotations;
using NexusMods.Abstractions.Diagnostics;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Serialization.DataModel.Ids;

namespace NexusMods.DataModel.Diagnostics;

/// <summary>
/// A diagnostic manager, which keeps track of all current diagnostics and refreshes them if necessary.
/// </summary>
[PublicAPI]
public interface IDiagnosticManager : IDisposable
{
    /// <summary>
    /// Gets an observable for all diagnostic changes.
    /// </summary>
    IObservable<IChangeSet<Diagnostic, IId>> DiagnosticChanges { get; }

    /// <summary>
    /// Gets all active diagnostics.
    /// </summary>
    IEnumerable<Diagnostic> ActiveDiagnostics { get; }

    /// <summary>
    /// Clears all active diagnostics.
    /// </summary>
    void ClearDiagnostics();

    /// <summary>
    /// Callback for loadout changes.
    /// </summary>
    ValueTask OnLoadoutChanged(Loadout loadout);
}
