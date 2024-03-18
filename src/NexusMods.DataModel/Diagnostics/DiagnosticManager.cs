using System.Reactive.Linq;
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
    private bool _isDisposed;

    private readonly ILogger<DiagnosticManager> _logger;
    private readonly ILoadoutRegistry _loadoutRegistry;

    public DiagnosticManager(
        ILogger<DiagnosticManager> logger,
        ILoadoutRegistry loadoutRegistry)
    {
        _logger = logger;
        _loadoutRegistry = loadoutRegistry;
    }

    public IObservable<Diagnostic[]> GetLoadoutDiagnostics(LoadoutId loadoutId)
    {
        // TODO: cancellation token
        var cancellationToken = CancellationToken.None;

        return _loadoutRegistry
            .RevisionsAsLoadouts(loadoutId)
            .DistinctUntilChanged(loadout => loadout.DataStoreId)
            .Throttle(dueTime: TimeSpan.FromMilliseconds(250))
            .SelectMany(async loadout => await GetLoadoutDiagnostics(loadout, cancellationToken));
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

    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;
    }
}
