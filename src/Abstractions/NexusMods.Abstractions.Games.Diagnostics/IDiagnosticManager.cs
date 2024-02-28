using DynamicData;
using JetBrains.Annotations;
using NexusMods.Abstractions.Loadouts;

namespace NexusMods.Abstractions.Diagnostics;

/// <summary>
/// A diagnostic manager, which keeps track of all current diagnostics and refreshes them if necessary.
/// </summary>
[PublicAPI]
public interface IDiagnosticManager : IDisposable
{
    /// <summary>
    /// Gets an observable for all diagnostic changes.
    /// </summary>
    IObservable<IChangeSet<Diagnostic>> DiagnosticChanges { get; }

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
