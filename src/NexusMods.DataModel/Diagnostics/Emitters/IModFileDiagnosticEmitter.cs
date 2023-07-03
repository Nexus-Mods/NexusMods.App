using JetBrains.Annotations;
using NexusMods.DataModel.Loadouts;
using NexusMods.FileExtractor.FileSignatures;

namespace NexusMods.DataModel.Diagnostics.Emitters;

/// <summary>
/// Interface for diagnostic emitters that only run on <see cref="AModFile"/>.
/// </summary>
[PublicAPI]
public interface IModFileDiagnosticEmitter : IDataDiagnosticEmitter<AModFile>
{
    /// <summary>
    /// Defines the file types supported by this emitter. The emitter
    /// will not be called for files whose types aren't in this enumerable.
    /// </summary>
    public IEnumerable<FileType> FileTypes { get; }
}
