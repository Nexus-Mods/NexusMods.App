using System.Collections.Concurrent;
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
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Query;
using Observable = R3.Observable;

namespace NexusMods.DataModel.Diagnostics;

/// <inheritdoc/>
[UsedImplicitly]
internal sealed class DiagnosticManager : IDiagnosticManager
{
    private readonly ILogger<DiagnosticManager> _logger;

    private static readonly object Lock = new();
    private readonly SourceCache<IConnectableObservable<Diagnostic[]>, LoadoutId> _observableCache = new(_ => throw new NotSupportedException());

    private bool _isDisposed;
    private readonly CompositeDisposable _compositeDisposable = new();
    private readonly IConnection _connection;

    public DiagnosticManager(ILogger<DiagnosticManager> logger, IConnection connection)
    {
        _logger = logger;
        _connection = connection;
    }

    public IObservable<Diagnostic[]> GetLoadoutDiagnostics(LoadoutId loadoutId)
    {
        ObjectDisposedException.ThrowIf(_isDisposed, typeof(DiagnosticManager));

        lock (Lock)
        {
            var existingObservable = _observableCache.Lookup(loadoutId);
            if (existingObservable.HasValue) return existingObservable.Value;

            var connectableObservable = Loadout.RevisionsWithChildUpdates(_connection, loadoutId)
                .Throttle(dueTime: TimeSpan.FromMilliseconds(250))
                .SelectMany(async _ =>
                {
                    var db = _connection.Db;
                    var loadout = Loadout.Load(db, loadoutId);
                    if (!loadout.IsValid()) return [];

                    try
                    {
                        // TODO: cancellation token
                        // TODO: optimize this a bit so we don't load the model twice, we have the datoms above, we should be able to use them

                        var cancellationToken = CancellationToken.None;
                        return await GetLoadoutDiagnostics(loadout, cancellationToken);
                    }
                    catch (Exception e)
                    {
                        _logger.LogError(e, "Exception while diagnosing loadout {Loadout}", loadout.Name);
                        return [];
                    }
                })
                .Replay(bufferSize: 1);
            
            _compositeDisposable.Add(connectableObservable.Connect());
            _observableCache.Edit(updater => updater.AddOrUpdate(connectableObservable, loadoutId));
            return connectableObservable;
        }
    }

    private async Task<Diagnostic[]> GetLoadoutDiagnostics(Loadout.ReadOnly loadout, CancellationToken cancellationToken)
    {
        var diagnosticEmitters = loadout.InstallationInstance.GetGame().DiagnosticEmitters;

        try
        {
            List<Diagnostic> diagnostics = [];
            
            await Parallel.ForEachAsync(diagnosticEmitters.OfType<ILoadoutDiagnosticEmitter>(), cancellationToken, async (emitter, token) =>
            {
                var start = DateTimeOffset.UtcNow;

                try
                {
                    await foreach (var diagnostic in emitter.Diagnose(loadout, token))
                    {
                        lock(diagnostics)
                        {
                            diagnostics.Add(diagnostic);
                        }
                    }
                }
                catch (TaskCanceledException)
                {
                    // ignore
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Exception in emitter {Emitter}", emitter.GetType());
                }
                finally
                {
                    var end = DateTimeOffset.UtcNow;
                    var duration = end - start;
                    _logger.LogTrace("Emitter {Emitter} took {Duration} ms", emitter.GetType(), duration.TotalMilliseconds);
                }
            });

            if (cancellationToken.IsCancellationRequested) return [];

            var flattened = diagnostics
                .OrderByDescending(x => x.Severity)
                .ThenBy(x => x.Id)
                .ToArray();

            return flattened;
        }
        catch (TaskCanceledException)
        {
            // ignore
            return [];
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
