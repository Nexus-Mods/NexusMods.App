using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using DynamicData;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.Diagnostics;
using NexusMods.Abstractions.Diagnostics.Emitters;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.Loadouts;
using NexusMods.DataModel.Extensions;
using NexusMods.Extensions.BCL;

namespace NexusMods.DataModel.Diagnostics;

/// <inheritdoc/>
[UsedImplicitly]
internal sealed class DiagnosticManager : IDiagnosticManager
{
    private readonly ILogger<DiagnosticManager> _logger;
    private readonly ILoadoutRegistry _loadoutRegistry;

    private static readonly object Lock = new();
    private readonly SourceCache<IConnectableObservable<Diagnostic[]>, LoadoutId> _observableCache = new(_ => throw new NotSupportedException());

    private bool _isDisposed;
    private readonly CompositeDisposable _compositeDisposable = new();

    public DiagnosticManager(
        ILogger<DiagnosticManager> logger,
        ILoadoutRegistry loadoutRegistry)
    {
        _logger = logger;
        _loadoutRegistry = loadoutRegistry;
    }

    public IObservable<Diagnostic[]> GetLoadoutDiagnostics(LoadoutId loadoutId)
    {
        ObjectDisposedException.ThrowIf(_isDisposed, typeof(DiagnosticManager));

        lock (Lock)
        {
            var existingObservable = _observableCache.Lookup(loadoutId);
            if (existingObservable.HasValue) return existingObservable.Value;

            var connectableObservable = _loadoutRegistry
                .RevisionsAsLoadouts(loadoutId)
                .DistinctUntilChanged(loadout => loadout.DataStoreId)
                .Throttle(dueTime: TimeSpan.FromMilliseconds(250))
                .SelectMany(async loadout =>
                {
                    try
                    {
                        // TODO: cancellation token
                        var cancellationToken = CancellationToken.None;
                        return await GetLoadoutDiagnostics(loadout, cancellationToken);
                    }
                    catch (Exception e)
                    {
                        _logger.LogError(e, "Exception while diagnosing loadout {Loadout}", loadout.Name);
                        return Array.Empty<Diagnostic>();
                    }
                })
                .Replay(bufferSize: 1);

            _compositeDisposable.Add(connectableObservable.Connect());
            _observableCache.Edit(updater => updater.AddOrUpdate(connectableObservable, loadoutId));
            return connectableObservable;
        }
    }

    private static async Task<Diagnostic[]> GetLoadoutDiagnostics(Loadout loadout, CancellationToken cancellationToken)
    {
        var diagnosticEmitters = loadout.Installation.GetGame().DiagnosticEmitters;

        try
        {
            var diagnostics = (
                    await diagnosticEmitters
                        .OfType<ILoadoutDiagnosticEmitter>()
                        .SelectAsync(async emitter => await emitter.Diagnose(loadout, cancellationToken).ToArrayAsync())
                        .ToArrayAsync()
                )
                .SelectMany(arr => arr)
                .ToArray();

            return diagnostics;
        }
        catch (TaskCanceledException)
        {
            // ignore
            return Array.Empty<Diagnostic>();
        }
    }

    public IObservable<(int NumSuggestions, int NumWarnings, int NumCritical)> CountDiagnostics(LoadoutId loadoutId)
    {
        return GetLoadoutDiagnostics(loadoutId)
            .Select(diagnostics =>
            {
                int numSuggestions = 0, numWarnings = 0, numCritical = 0;
                foreach (var diagnostic in diagnostics)
                {
                    switch (diagnostic.Severity)
                    {
                        case DiagnosticSeverity.Suggestion:
                            numSuggestions += 1;
                            break;
                        case DiagnosticSeverity.Warning:
                            numWarnings += 1;
                            break;
                        case DiagnosticSeverity.Critical:
                            numCritical += 1;
                            break;
                        default: break;
                    }
                }

                return (numSuggestions, numWarnings, numCritical);
            });
    }

    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;

        lock (Lock)
        {
            _compositeDisposable.Dispose();
            _observableCache.Dispose();
        }
    }
}
