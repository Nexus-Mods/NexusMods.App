using JetBrains.Annotations;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Loadouts.Mods;

namespace NexusMods.Abstractions.Diagnostics.Emitters;

/// <summary>
/// Interface for diagnostic emitters that run on a single <see cref="Mod"/>.
/// </summary>
/// <remarks>
/// This interface should be implemented if the emitter only has to look at
/// a singular <see cref="Mod"/>.
/// </remarks>
/// <seealso cref="ILoadoutDiagnosticEmitter"/>
[PublicAPI]
public interface IModDiagnosticEmitter : IDiagnosticEmitter
{
    /// <summary>
    /// Diagnoses a mod and creates instances of <see cref="Diagnostic"/>.
    /// </summary>
    /// <param name="loadout">The current loadout.</param>
    /// <param name="mod">The current mod.</param>
    IEnumerable<Diagnostic> Diagnose(Loadout loadout, Mod mod);
}
