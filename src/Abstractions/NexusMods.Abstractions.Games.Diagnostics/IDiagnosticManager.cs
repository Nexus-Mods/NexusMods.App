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
    /// Returns an observable stream of all diagnostics for a loadout.
    /// </summary>
    IObservable<Diagnostic[]> GetLoadoutDiagnostics(LoadoutId loadoutId);
}
