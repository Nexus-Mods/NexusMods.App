using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.TxFunctions;
using NexusMods.Sdk.NexusModsApi;

namespace NexusMods.Networking.NexusWebApi.UpdateFilters;

/// <inheritdoc />
public class ModUpdateFilterService : IModUpdateFilterService
{
    private readonly Subject<Unit> _filterTrigger = new();
    private readonly IDisposable _ignoreFilterObserver;
    private readonly IConnection _connection;
    private readonly IgnoreModUpdateFilter _ignoreFilter;
    
    /// <inheritdoc />
    public IObservable<Unit> FilterTrigger => _filterTrigger.AsObservable();

    /// <summary>
    /// Creates a new instance of the mod update filter service.
    /// </summary>
    /// <param name="connection">Database connection to observe filter changes.</param>
    public ModUpdateFilterService(IConnection connection)
    {
        _connection = connection;
        _ignoreFilter = new IgnoreModUpdateFilter(connection);
        _ignoreFilterObserver = ObserveIgnoreFilterChanges(connection);
    }

    /// <inheritdoc />
    public void TriggerFilterUpdate()
    {
        _filterTrigger.OnNext(Unit.Default);
    }

    /// <inheritdoc />
    public async Task HideFileAsync(FileUid fileUid)
    {
        var tx = _connection.BeginTransaction();
        _ = new IgnoreFileUpdate.New(tx) { Uid = fileUid };
        await tx.Commit();
    }

    /// <inheritdoc />
    public async Task HideFilesAsync(IEnumerable<FileUid> fileUids)
    {
        var tx = _connection.BeginTransaction();
        foreach (var fileUid in fileUids)
        {
            _ = new IgnoreFileUpdate.New(tx) { Uid = fileUid };
        }
        await tx.Commit();
    }

    /// <inheritdoc />
    public async Task ShowFileAsync(FileUid fileUid)
    {
        var tx = _connection.BeginTransaction();
        var ignoreEntries = IgnoreFileUpdate.FindByUid(_connection.Db, fileUid);
        foreach (var entry in ignoreEntries)
        {
            tx.Delete(entry.Id, recursive: false);
        }
        await tx.Commit();
    }

    /// <inheritdoc />
    public async Task ShowFilesAsync(IEnumerable<FileUid> fileUids)
    {
        var tx = _connection.BeginTransaction();
        foreach (var fileUid in fileUids)
        {
            var ignoreEntries = IgnoreFileUpdate.FindByUid(_connection.Db, fileUid);
            foreach (var entry in ignoreEntries)
            {
                tx.Delete(entry.Id, recursive: false);
            }
        }
        await tx.Commit();
    }

    private IDisposable ObserveIgnoreFilterChanges(IConnection connection)
    {
        return IgnoreFileUpdate.ObserveAll(connection)
            .Subscribe(changes =>
            {
                // Note(sewer):
                // When ignored mods change, we need to refresh all filtered views
                // so that observables with custom select functions can re-evaluate
                // whether files should be shown or hidden based on the new ignore state.
                _filterTrigger.OnNext(Unit.Default);
            });
    }

    /// <inheritdoc />
    public IObservable<bool> ObserveFileHiddenState(FileUid fileUid)
    {
        // Create an observable that emits the current hidden state and updates when filters change
        return Observable.Create<bool>(observer =>
        {
            // Helper function to check if file is hidden
            bool IsThisFileHidden() => IgnoreFileUpdate.FindByUid(_connection.Db, fileUid).Any();
            
            // Emit the current state immediately
            observer.OnNext(IsThisFileHidden());
            
            // Subscribe to filter changes and emit new state when changes occur
            var subscription = _filterTrigger.Subscribe(_ =>
            {
                observer.OnNext(IsThisFileHidden());
            });
            
            return subscription;
        })
        .DistinctUntilChanged(); // Only emit when the hidden state actually changes
    }

    /// <inheritdoc />
    public bool IsFileHidden(FileUid fileUid)
    {
        return IgnoreFileUpdate.FindByUid(_connection.Db, fileUid).Any();
    }
    
    /// <inheritdoc />
    public ModUpdateOnPage? SelectMod(ModUpdateOnPage modUpdateOnPage)
    {
        return _ignoreFilter.SelectMod(modUpdateOnPage);
    }
    
    /// <inheritdoc />
    public ModUpdatesOnModPage? SelectModPage(ModUpdatesOnModPage modUpdatesOnModPage)
    {
        return _ignoreFilter.SelectModPage(modUpdatesOnModPage);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _filterTrigger.Dispose();
        _ignoreFilterObserver.Dispose();
    }
}
