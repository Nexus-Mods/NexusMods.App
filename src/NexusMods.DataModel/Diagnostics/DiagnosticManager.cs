using System.Reactive.Disposables;
using DynamicData;
using Microsoft.Extensions.Logging;
using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.Abstractions.Ids;
using NexusMods.DataModel.Diagnostics.Emitters;
using NexusMods.DataModel.Loadouts;

namespace NexusMods.DataModel.Diagnostics;

/// <inheritdoc/>
internal sealed class DiagnosticManager : IDiagnosticManager
{
    private bool _isDisposed;

    private readonly ILogger<DiagnosticManager> _logger;
    private readonly ILoadoutDiagnosticEmitter[] _loadoutDiagnosticEmitters;
    private readonly IModDiagnosticEmitter[] _modDiagnosticEmitters;
    private readonly IModFileDiagnosticEmitter[] _modFileDiagnosticEmitters;

    private readonly CompositeDisposable _compositeDisposable;
    private readonly SourceCache<Diagnostic, IId> _diagnosticCache = new(x => x.DataStoreId);

    /// <summary>
    /// Constructor.
    /// </summary>
    public DiagnosticManager(
        ILogger<DiagnosticManager> logger,
        IEnumerable<ILoadoutDiagnosticEmitter> loadoutDiagnosticEmitters,
        IEnumerable<IModDiagnosticEmitter> modDiagnosticEmitters,
        IEnumerable<IModFileDiagnosticEmitter> modFileDiagnosticEmitters,
        IDataStore dataStore,
        LoadoutRegistry loadoutRegistry)
    {
        _logger = logger;

        _loadoutDiagnosticEmitters = loadoutDiagnosticEmitters.ToArray();
        _modDiagnosticEmitters = modDiagnosticEmitters.ToArray();
        _modFileDiagnosticEmitters = modFileDiagnosticEmitters.ToArray();

        _compositeDisposable = new();
    }

    public IObservable<IChangeSet<Diagnostic, IId>> ActiveDiagnostics => _diagnosticCache.Connect();

    public void Dispose()
    {
        if (_isDisposed) return;

        _diagnosticCache.Dispose();
        _compositeDisposable.Dispose();

        _isDisposed = true;
    }
}
