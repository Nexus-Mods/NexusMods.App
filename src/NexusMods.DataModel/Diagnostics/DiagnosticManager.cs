using DynamicData;
using NexusMods.DataModel.Diagnostics.Emitters;

namespace NexusMods.DataModel.Diagnostics;

/// <inheritdoc/>
internal sealed class DiagnosticManager : IDiagnosticManager
{
    private readonly ILoadoutDiagnosticEmitter[] _loadoutDiagnosticEmitters;
    private readonly IModDiagnosticEmitter[] _modDiagnosticEmitters;
    private readonly IModFileDiagnosticEmitter[] _modFileDiagnosticEmitters;

    private readonly SourceCache<Diagnostic, Guid> _diagnosticCache = new(x => x.Guid);

    /// <summary>
    /// Constructor.
    /// </summary>
    public DiagnosticManager(
        IEnumerable<ILoadoutDiagnosticEmitter> loadoutDiagnosticEmitters,
        IEnumerable<IModDiagnosticEmitter> modDiagnosticEmitters,
        IEnumerable<IModFileDiagnosticEmitter> modFileDiagnosticEmitters)
    {
        _loadoutDiagnosticEmitters = loadoutDiagnosticEmitters.ToArray();
        _modDiagnosticEmitters = modDiagnosticEmitters.ToArray();
        _modFileDiagnosticEmitters = modFileDiagnosticEmitters.ToArray();
    }

    public IObservable<IChangeSet<Diagnostic, Guid>> ActiveDiagnostics => _diagnosticCache.Connect();
}
