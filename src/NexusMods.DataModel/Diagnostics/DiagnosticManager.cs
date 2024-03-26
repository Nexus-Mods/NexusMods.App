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

    private async Task<Diagnostic[]> GetLoadoutDiagnostics(Loadout loadout, CancellationToken cancellationToken)
    {
        var diagnosticEmitters = loadout.Installation.GetGame().DiagnosticEmitters;

        try
        {
            var nested = await diagnosticEmitters
                .OfType<ILoadoutDiagnosticEmitter>()
                .SelectAsync(async emitter =>
                {
                    var start = DateTimeOffset.UtcNow;

                    try
                    {
                        var res = await emitter
                            .Diagnose(loadout, cancellationToken)
                            .ToListAsync(cancellationToken);

                        return (IList<Diagnostic>)res;
                    }
                    catch (TaskCanceledException)
                    {
                        // ignore
                        return Array.Empty<Diagnostic>();
                    }
                    catch (Exception e)
                    {
                        _logger.LogError(e, "Exception in emitter {Emitter}", emitter.GetType());
                        return Array.Empty<Diagnostic>();
                    }
                    finally
                    {
                        var end = DateTimeOffset.UtcNow;
                        var duration = end - start;
                        _logger.LogTrace("Emitter {Emitter} took {Duration} ms", emitter.GetType(), duration.TotalMilliseconds);
                    }
                })
                .ToListAsync(cancellationToken);

            if (cancellationToken.IsCancellationRequested) return Array.Empty<Diagnostic>();

            var flattened = nested
                .SelectMany(many => many)
                .ToArray();

            return flattened;
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
