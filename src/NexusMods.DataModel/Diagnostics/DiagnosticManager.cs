using System.Reactive.Disposables;
using DynamicData;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.Abstractions.Ids;
using NexusMods.DataModel.Diagnostics.Emitters;
using NexusMods.DataModel.Diagnostics.References;
using NexusMods.DataModel.Loadouts;
using NexusMods.DataModel.Loadouts.Cursors;
using NexusMods.DataModel.Loadouts.Mods;

namespace NexusMods.DataModel.Diagnostics;

/// <inheritdoc/>
[UsedImplicitly]
internal sealed class DiagnosticManager : IDiagnosticManager
{
    private bool _isDisposed;

    private readonly ILogger<DiagnosticManager> _logger;
    private readonly IDataStore _dataStore;
    private readonly IOptionsMonitor<DiagnosticOptions> _optionsMonitor;

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
        IDataStore dataStore,
        IOptionsMonitor<DiagnosticOptions> optionsMonitor,
        IEnumerable<ILoadoutDiagnosticEmitter> loadoutDiagnosticEmitters,
        IEnumerable<IModDiagnosticEmitter> modDiagnosticEmitters,
        IEnumerable<IModFileDiagnosticEmitter> modFileDiagnosticEmitters)
    {
        _logger = logger;
        _dataStore = dataStore;
        _optionsMonitor = optionsMonitor;

        _loadoutDiagnosticEmitters = loadoutDiagnosticEmitters.ToArray();
        _modDiagnosticEmitters = modDiagnosticEmitters.ToArray();
        _modFileDiagnosticEmitters = modFileDiagnosticEmitters.ToArray();

        _compositeDisposable = new();

        // Listen to option changes and update the currently existing diagnostics.
        var disposable = _optionsMonitor.OnChange(newOptions =>
        {
            var filteredDiagnostics = FilterDiagnostics(_diagnosticCache.Items, newOptions);
            _diagnosticCache.Edit(updater =>
            {
                updater.Load(filteredDiagnostics);
            });
        });

        if (disposable is not null) _compositeDisposable.Add(disposable);
    }

    public IObservable<IChangeSet<Diagnostic, IId>> DiagnosticChanges => _diagnosticCache.Connect();
    public IEnumerable<Diagnostic> ActiveDiagnostics => _diagnosticCache.Items;

    public void OnLoadoutChanged(Loadout loadout)
    {
        RefreshLoadoutDiagnostics(loadout);
        RefreshModDiagnostics(loadout);
    }

    public void ClearDiagnostics()
    {
        _diagnosticCache.Edit(updater => updater.Clear());
    }

    internal void RefreshLoadoutDiagnostics(Loadout loadout)
    {
        // Remove outdated diagnostics for previous revisions of the loadout
        RemoveDiagnostics(kv => kv.Value.DataReferences
            .OfType<LoadoutReference>()
            .Any(loadoutReference =>
                loadoutReference.DataId == loadout.LoadoutId
                && !Equals(loadoutReference.DataStoreId, loadout.DataStoreId)
            )
        );

        var newDiagnostics = _loadoutDiagnosticEmitters
            .SelectMany(emitter => emitter.Diagnose(loadout))
            .ToArray();

        AddDiagnostics(newDiagnostics);
    }

    internal void RefreshModDiagnostics(Loadout loadout)
    {
        var previousLoadout = loadout.PreviousVersion.Value;

        Mod[] addedMods;
        if (previousLoadout is not null)
        {
            var modChangeSet = loadout.Mods.Diff(previousLoadout.Mods);
            var removedMods = modChangeSet
                .Where(change => change.Reason is ChangeReason.Remove or ChangeReason.Update)
                .Select(change => new ModCursor(loadout.LoadoutId, change.Key))
                .ToHashSet();

            // Remove outdated diagnostics for mods that have been removed or updated.
            RemoveDiagnostics(kv => kv.Value.DataReferences
                .OfType<ModReference>()
                .Any(modReference => removedMods.Contains(modReference.DataId))
            );

            // Prepare a collection of new or updated mods.
            addedMods = modChangeSet
                .Where(change => change.Reason is ChangeReason.Add or ChangeReason.Update)
                .Select(change => change.Key)
                .Select(modId => loadout.Mods[modId])
                .ToArray();
        }
        else
        {
            // Every mod is considered to be new if there is no previous loadout revision.
            addedMods = loadout.Mods
                .Select(x => x.Value)
                .ToArray();
        }

        // Run the emitters on the added mods.
        var newDiagnostics = addedMods
            .SelectMany(mod => _modDiagnosticEmitters
                .SelectMany(emitter => emitter.Diagnose(loadout, mod))
            )
            .ToArray();

        AddDiagnostics(newDiagnostics);
    }

    internal void RefreshModFileDiagnostics()
    {
        // TODO: figure out how to track changes to files (mods and files are "immutable")
        throw new NotImplementedException();
    }

    private void RemoveDiagnostics(Func<KeyValuePair<IId, Diagnostic>, bool> predicate)
    {
        // TODO: consider removing them from the DataStore as well. This requires batching.
        _diagnosticCache.Edit(updater =>
        {
            var toRemove = updater.KeyValues
                .Where(predicate)
                .Select(kv => kv.Key);

            updater.RemoveKeys(toRemove);
        });
    }

    private void AddDiagnostics(IEnumerable<Diagnostic> newDiagnostics)
    {
        var toAdd = FilterDiagnostics(newDiagnostics, _optionsMonitor.CurrentValue);

        _diagnosticCache.Edit(updater =>
        {
            updater.AddOrUpdate(toAdd.Select(diagnostic => diagnostic.WithPersist(_dataStore)));
        });
    }

    internal static IEnumerable<Diagnostic> FilterDiagnostics(
        IEnumerable<Diagnostic> diagnostics,
        DiagnosticOptions options)
    {
        return diagnostics
            .Where(x => x.Severity >= options.MinimumSeverity)
            .Where(x => !options.IgnoredDiagnostics.Contains(x.Id));
    }

    public void Dispose()
    {
        if (_isDisposed) return;

        _diagnosticCache.Dispose();
        _compositeDisposable.Dispose();

        _isDisposed = true;
    }
}
