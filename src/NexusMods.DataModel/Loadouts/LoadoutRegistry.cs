using System.Reactive.Disposables;
using System.Reactive.Linq;
using DynamicData;
using Microsoft.Extensions.Logging;
using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.Abstractions.Ids;
using NexusMods.DataModel.Extensions;
using NexusMods.DataModel.Games;
using NexusMods.DataModel.Loadouts.Cursors;
using NexusMods.DataModel.Loadouts.Markers;
using NexusMods.DataModel.Loadouts.Mods;

namespace NexusMods.DataModel.Loadouts;

/// <summary>
/// Provides the main entry point for listing, modifying and updating loadouts.
/// </summary>
public class LoadoutRegistry : IDisposable
{
    private bool _isDisposed;

    private readonly ILogger<LoadoutRegistry> _logger;
    private readonly IDataStore _store;
    private SourceCache<IId, LoadoutId> _cache;

    /// <summary>
    /// All the loadoutIds and their current root entity IDs
    /// </summary>
    public IObservable<IChangeSet<IId,LoadoutId>> LoadoutChanges => _cache.Connect();

    /// <summary>
    /// All the loadouts and their current root ids
    /// </summary>
    public IObservable<IChangeSet<Loadout, LoadoutId>> Loadouts =>
        LoadoutChanges.Transform(id => _store.Get<Loadout>(id, true)!);

    /// <summary>
    /// All games that have loadouts
    /// </summary>
    public IObservable<IDistinctChangeSet<IGame>> Games =>
        Loadouts
            .DistinctValues(d => d.Installation.Game);

    private readonly CompositeDisposable _compositeDisposable;

    /// <summary>
    /// DI constructor.
    /// </summary>
    /// <param name="logger"></param>
    /// <param name="store"></param>
    public LoadoutRegistry(ILogger<LoadoutRegistry> logger, IDataStore store)
    {
        _logger = logger;
        _store = store;
        _compositeDisposable = new CompositeDisposable();

        _cache = new SourceCache<IId, LoadoutId>(_ => throw new NotImplementedException());
        _cache.Edit(x =>
        {
            foreach (var loadoutId in AllLoadoutIds())
            {
                var id = GetId(loadoutId);
                x.AddOrUpdate(id!, loadoutId);
            }
        });

        var dispose =_store.IdChanges
            .Where(c => c.Category == EntityCategory.LoadoutRoots)
            .Select(LoadoutId.From)
            .Subscribe(loadoutId =>
            {
                _cache.Edit(x =>
                {
                    var dataStoreId = GetId(loadoutId);
                    x.AddOrUpdate(dataStoreId!, loadoutId);
                });
            });

        _compositeDisposable.Add(dispose);
    }

    /// <summary>
    /// Alters the loadout with the given id. If the loadout does not exist, it is created.
    /// </summary>
    /// <param name="id"></param>
    /// <param name="commitMessage"></param>
    /// <param name="alterFn"></param>
    /// <returns></returns>
    public Loadout Alter(LoadoutId id, string commitMessage, Func<Loadout, Loadout> alterFn)
    {
        var dbId = id.ToEntityId(EntityCategory.LoadoutRoots);
        TryAgain:
        var loadoutRoot = _store.GetRaw(dbId);
        Loadout? loadout = null;
        if (loadoutRoot != null)
        {
            loadout = _store.Get<Loadout>(IId.FromTaggedSpan(loadoutRoot), true);
        }

        var newLoadout = alterFn(loadout ?? Loadout.Empty(_store));

        newLoadout = newLoadout with
        {
            LastModified = DateTime.UtcNow,
            ChangeMessage = commitMessage,
            PreviousVersion = new EntityLink<Loadout>(loadout?.DataStoreId ?? IdEmpty.Empty, _store)
        };

        newLoadout.EnsurePersisted(_store);

        Span<byte> span = stackalloc byte[newLoadout.DataStoreId.SpanSize + 1];
        newLoadout.DataStoreId.ToTaggedSpan(span);


        if (!_store.CompareAndSwap(dbId, span, loadoutRoot))
            goto TryAgain;

        _logger.LogInformation("Loadout {LoadoutId} altered: {CommitMessage}", id, commitMessage);

        return newLoadout;
    }

    /// <summary>
    /// Alters the mod with the given id in the loadout with the given id. If the alter
    /// function returns null, the mod is removed from the loadout.
    /// </summary>
    /// <param name="loadoutId"></param>
    /// <param name="modId"></param>
    /// <param name="commitMessage"></param>
    /// <param name="alterfn"></param>
    public void Alter(LoadoutId loadoutId, ModId modId, string commitMessage, Func<Mod?, Mod?> alterfn)
    {
        Alter(loadoutId, commitMessage, loadout =>
        {
            var existingMod = loadout.Mods.TryGetValue(modId, out var mod) ? mod : null;

            if (existingMod == null)
            {
                existingMod = new Mod()
                {
                    Id = ModId.New(),
                    Name = "",
                    Files = EntityDictionary<ModFileId, AModFile>.Empty(_store)
                };
            }

            var newMod = alterfn(existingMod);
            if (newMod == null)
            {
                return loadout with { Mods = loadout.Mods.Without(modId) };
            }

            return loadout with { Mods = loadout.Mods.With(modId, newMod) };
        });

    }

    /// <summary>
    /// Alters the mod pointed to by the cursor. If the alter function returns null, the mod is removed from the loadout.
    /// </summary>
    /// <param name="cursor"></param>
    /// <param name="commitMessage"></param>
    /// <param name="alterFn"></param>
    public void Alter(ModCursor cursor, string commitMessage, Func<Mod?, Mod?> alterFn)
    {
        Alter(cursor.LoadoutId, cursor.ModId, commitMessage, alterFn);
    }

    /// <summary>
    /// Modify the loadout with the given id using the given visitor. This is not very
    /// optimized, so should only be used in situations were large scale transformations
    /// are being done. The methods on the visitor will be called for every part of the
    /// loadout.
    /// </summary>
    /// <param name="id"></param>
    /// <param name="commitMessage"></param>
    /// <param name="visitor"></param>
    public Loadout Alter(LoadoutId id, string commitMessage, ALoadoutVisitor visitor)
    {
        // Callback hell? Never heard of it!
        return Alter(id, commitMessage, loadout =>
        {
            return visitor.Alter(loadout with
            {
                Mods = loadout.Mods.Keep(mod =>
                {
                    return visitor.Alter(mod with { Files = mod.Files.Keep(visitor.Alter) });
                })
            });
        });

    }

    /// <summary>
    /// Gets the id of the loadout with the given loadout id.
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public IId? GetId(LoadoutId id)
    {
        var bytes = _store.GetRaw(id.ToEntityId(EntityCategory.LoadoutRoots));
        return bytes == null ? null : IId.FromTaggedSpan(bytes);
    }

    /// <summary>
    /// Gets the loadout with the given id.
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    public Loadout? Get(LoadoutId id)
    {
        return _store.Get<Loadout>(GetId(id)!, true);
    }

    /// <summary>
    /// Gets the mod pointed to by the cursor.
    /// </summary>
    /// <param name="cursor"></param>
    /// <returns></returns>
    public Mod? Get(ModCursor cursor)
    {
        return Get(cursor.LoadoutId, cursor.ModId);
    }

    /// <summary>
    /// Finds the loadout with the given name (case insensitive).
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    public Loadout? GetByName(string name)
    {
        return AllLoadouts().First(l =>
            l.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase));
    }

    /// <summary>
    /// Gets the mod with the given id from the given loadout.
    /// </summary>
    /// <param name="loadoutId"></param>
    /// <param name="modId"></param>
    /// <returns></returns>
    public Mod? Get(LoadoutId loadoutId, ModId modId)
    {
        var loadout = Get(loadoutId);
        return loadout?.Mods[modId];
    }

    /// <summary>
    /// Returns all loadout ids.
    /// </summary>
    /// <returns></returns>
    public IEnumerable<LoadoutId> AllLoadoutIds()
    {
        return _store.AllIds(EntityCategory.LoadoutRoots)
                .Select(LoadoutId.From);
    }


    /// <summary>
    /// Returns all loadouts.
    /// </summary>
    /// <returns></returns>
    public IEnumerable<Loadout> AllLoadouts()
    {
        return AllLoadoutIds()
            .Select(id => Get(id)!);
    }

    /// <summary>
    /// An observable of all the revisions of a given LoadoutId
    /// </summary>
    /// <param name="loadoutId"></param>
    /// <returns></returns>
    public IObservable<IId> Revisions(LoadoutId loadoutId)
    {
        var rootId = loadoutId.ToEntityId(EntityCategory.LoadoutRoots);
        return _store.IdChanges
            .Where(id => id.Equals(rootId))
            .Select(id => IId.FromTaggedSpan(_store.GetRaw(id)))
            .StartWith(IId.FromTaggedSpan(_store.GetRaw(rootId)));
    }

    /// <summary>
    /// Same as Revisions, but returns the loadouts instead of the ids.
    /// </summary>
    /// <param name="loadoutId"></param>
    /// <returns></returns>
    public IObservable<Loadout> RevisionsAsLoadouts(LoadoutId loadoutId)
    {
        return Revisions(loadoutId)
            .Select(id => _store.Get<Loadout>(id, true)!)
            .NotNull();
    }

    /// <summary>
    /// An observable of all the revisions of a given loadout and mod
    /// </summary>
    /// <param name="loadoutId"></param>
    /// <param name="modId"></param>
    /// <returns></returns>
    public IObservable<IId> Revisions(LoadoutId loadoutId, ModId modId)
    {
        return Revisions(loadoutId)
            .Select(id => _store.Get<Loadout>(id, true)?.Mods.GetValueId(modId) ?? null)
            .NotNull();
    }

    /// <summary>
    /// Gets the revisions of a given cursor
    /// </summary>
    /// <param name="cursor"></param>
    /// <returns></returns>
    public IObservable<IId> Revisions(ModCursor cursor)
    {
        return Revisions(cursor.LoadoutId, cursor.ModId);
    }

    /// <summary>
    /// Returns the current and future revisions for a mod pointed
    /// to by the cursor. Same as Revisions, but returns the mods
    /// instead of the ids.
    /// </summary>
    /// <param name="cursor"></param>
    /// <returns></returns>
    public IObservable<Mod> RevisionsAsMods(ModCursor cursor)
    {
        return Revisions(cursor)
            .Select(id => _store.Get<Mod>(id, true))
            .NotNull();
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Releases the unmanaged resources and optionally releases the managed resources.
    /// </summary>
    /// <param name="disposing">
    /// <c>true</c> to release both managed and unmanaged resources;
    /// <c>false</c> to release only unmanaged resources.
    /// </param>
    protected virtual void Dispose(bool disposing)
    {
        if (_isDisposed) return;
        if (disposing)
        {
            _cache.Dispose();
            _compositeDisposable.Dispose();
        }

        _isDisposed = true;
    }

    /// <summary>
    /// Gets the marker for the given loadout id.
    /// </summary>
    /// <param name="loadoutId"></param>
    /// <returns></returns>
    public LoadoutMarker GetMarker(LoadoutId loadoutId)
    {
        return new LoadoutMarker(this, loadoutId);
    }
}
