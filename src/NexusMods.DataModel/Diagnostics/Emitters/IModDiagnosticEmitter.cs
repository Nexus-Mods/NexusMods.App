using JetBrains.Annotations;
using NexusMods.DataModel.Loadouts.Mods;

namespace NexusMods.DataModel.Diagnostics.Emitters;

/// <summary>
/// Interface for diagnostic emitters that run on a single <see cref="Mod"/>.
/// </summary>
/// <remarks>
/// This interface should be implemented if the emitter only has to look at
/// a singular <see cref="Mod"/>.
/// </remarks>
/// <seealso cref="ILoadoutDiagnosticEmitter"/>
[PublicAPI]
public interface IModDiagnosticEmitter : IDataDiagnosticEmitter<Mod> { }
