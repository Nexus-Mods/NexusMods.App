using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using NexusMods.Abstractions.NexusWebApi.Types.V2.Uid;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.TxFunctions;

namespace NexusMods.Networking.NexusWebApi.UpdateFilters;

/// <summary>
/// Default implementation of <see cref="IModUpdateFilterService"/> that observes
/// <see cref="IgnoreFileUpdate"/> changes and triggers filter re-evaluation.
/// </summary>
public class ModUpdateFilterService : IModUpdateFilterService
{
    private readonly Subject<Unit> _filterTrigger = new();
    private readonly IDisposable _ignoreFilterObserver;
    private readonly IConnection _connection;
    
    /// <inheritdoc />
    public IObservable<Unit> FilterTrigger => _filterTrigger.AsObservable();

    /// <summary>
    /// Creates a new instance of the mod update filter service.
    /// </summary>
    /// <param name="connection">Database connection to observe filter changes.</param>
    public ModUpdateFilterService(IConnection connection)
    {
        _connection = connection;
        _ignoreFilterObserver = ObserveIgnoreFilterChanges(connection);
    }

    /// <inheritdoc />
    public void TriggerFilterUpdate()
    {
        _filterTrigger.OnNext(Unit.Default);
    }

    /// <inheritdoc />
    public async Task HideFileAsync(UidForFile fileUid)
    {
        using var tx = _connection.BeginTransaction();
        _ = new IgnoreFileUpdate.New(tx) { Uid = fileUid };
        await tx.Commit();
    }

    /// <inheritdoc />
    public async Task HideFilesAsync(IEnumerable<UidForFile> fileUids)
    {
        using var tx = _connection.BeginTransaction();
        foreach (var fileUid in fileUids)
        {
            _ = new IgnoreFileUpdate.New(tx) { Uid = fileUid };
        }
        await tx.Commit();
    }

    /// <inheritdoc />
    public async Task ShowFileAsync(UidForFile fileUid)
    {
        using var tx = _connection.BeginTransaction();
        var ignoreEntries = IgnoreFileUpdate.FindByUid(_connection.Db, fileUid);
        foreach (var entry in ignoreEntries)
        {
            tx.Delete(entry.Id, recursive: false);
        }
        await tx.Commit();
    }

    /// <inheritdoc />
    public async Task ShowFilesAsync(IEnumerable<UidForFile> fileUids)
    {
        using var tx = _connection.BeginTransaction();
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
    public void Dispose()
    {
        _filterTrigger.Dispose();
        _ignoreFilterObserver.Dispose();
    }
}
