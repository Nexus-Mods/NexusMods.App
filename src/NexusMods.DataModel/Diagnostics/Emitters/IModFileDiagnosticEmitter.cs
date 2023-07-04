using JetBrains.Annotations;
using NexusMods.DataModel.Loadouts;
using NexusMods.DataModel.Loadouts.Mods;
using NexusMods.FileExtractor.FileSignatures;

namespace NexusMods.DataModel.Diagnostics.Emitters;

/// <summary>
/// Interface for diagnostic emitters that only run on <see cref="AModFile"/>.
/// </summary>
[PublicAPI]
public interface IModFileDiagnosticEmitter
{
    /// <summary>
    /// Defines the file types supported by this emitter. The emitter
    /// will not be called for files whose types aren't in this enumerable.
    /// </summary>
    public IEnumerable<FileType> FileTypes { get; }

    /// <summary>
    /// Diagnoses a mod file and creates instances of <see cref="Diagnostic"/>.
    /// </summary>
    /// <param name="loadout">The current loadout.</param>
    /// <param name="mod">The mod that contains the <paramref name="modFile"/>.</param>
    /// <param name="modFile">The file to diagnose.</param>
    IEnumerable<Diagnostic> Diagnose(Loadout loadout, Mod mod, AModFile modFile);
}
