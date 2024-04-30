using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using DynamicData;
using DynamicData.Binding;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Loadouts.Ids;
using NexusMods.Abstractions.Loadouts.Mods;
using NexusMods.Abstractions.Loadouts.Visitors;
using NexusMods.Abstractions.Serialization;
using NexusMods.Abstractions.Serialization.DataModel;
using NexusMods.Abstractions.Serialization.DataModel.Ids;
using NexusMods.DataModel.Extensions;

namespace NexusMods.DataModel.Loadouts;

/// <summary>
/// Provides the main entry point for listing, modifying and updating loadouts.
/// </summary>
public class LoadoutRegistry : IDisposable, ILoadoutRegistry
{
    private bool _isDisposed;

    private ConcurrentDictionary<LoadoutId, LoadoutMarker> _markers;
    private readonly ILogger<LoadoutRegistry> _logger;
    private readonly IDataStore _store;
    private SourceCache<IId, LoadoutId> _cache;
    private readonly ObservableCollection<LoadoutId> _loadoutsIds;

    /// <summary>
    /// All the loadoutIds and their current root entity IDs
    /// </summary>
    public IObservable<IChangeSet<IId,LoadoutId>> LoadoutChanges => _cache.Connect();

    /// <summary>
    /// All the loadoutRoots (<see cref="LoadoutId"/>)
    /// </summary>
    /// <returns></returns>
    public IObservable<IChangeSet<LoadoutId>> LoadoutRootChanges => _loadoutsIds.ToObservableChangeSet();

    /// <summary>
    /// All the loadouts and their current root ids
    /// </summary>
    public IObservable<IChangeSet<Loadout, LoadoutId>> Loadouts =>
        LoadoutChanges.Transform(id => _store.Get<Loadout>(id, true)!);

    /// <summary>
    /// All games that have loadouts
    /// </summary>
    public IObservable<IDistinctChangeSet<ILocatableGame>> Games =>
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
        _markers = new ConcurrentDictionary<LoadoutId, LoadoutMarker>();

        _loadoutsIds = new ObservableCollection<LoadoutId>();
        _loadoutsIds.AddRange(AllLoadoutIds().Distinct());
        
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
                
                if (!_loadoutsIds.Contains(loadoutId))
                    _loadoutsIds.Add(loadoutId);
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

        var newLoadout = alterFn(loadout ?? Loadout.Empty(_store) with {LoadoutId = id});

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

        var marker = _markers.GetOrAdd(id, id => new LoadoutMarker(this, id));
        marker.SetDataStoreId(newLoadout.DataStoreId);

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
                    Id = ModId.NewId(),
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
    /// Alters the file with the given id in the mod with the given id in the loadout with the given id. If the file
    /// does not exist, an error is thrown.
    /// </summary>
    /// <param name="loadoutId"></param>
    /// <param name="modId"></param>
    /// <param name="fileId"></param>
    /// <param name="commitMessage"></param>
    /// <param name="alterFn"></param>
    /// <typeparam name="T"></typeparam>
    public void Alter<T>(LoadoutId loadoutId, ModId modId, ModFileId fileId, string commitMessage, Func<T, T> alterFn)
    where T : AModFile
    {
        Alter(loadoutId, modId, commitMessage, mod =>
        {
            var existingFile = mod!.Files[fileId];
            var newFile = alterFn((T)existingFile);
            return mod with { Files = mod.Files.With(fileId, newFile) };
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
        return Alter(id, commitMessage, visitor.Transform);
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
        var databaseId = GetId(id)!;
        if (databaseId == null)
            throw new InvalidOperationException($"Loadout {id} does not exist");
        return _store.Get<Loadout>(databaseId, true);
    }

    /// <summary>
    /// Loads the loadout with the given id, or null if it does not exist.
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public Loadout? GetLoadout(IId id)
    {
        return _store.Get<Loadout>(id, true);
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
    public IEnumerable<Loadout> GetByName(string name)
    {
        return AllLoadouts().Where(l =>
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
        return _markers.GetOrAdd(loadoutId, id => new LoadoutMarker(this, id));
    }

    /// <summary>
    /// Suggestions a name for a new loadout.
    /// </summary>
    /// <param name="installation"></param>
    /// <returns></returns>
    public string SuggestName(GameInstallation installation)
    {
        var names = AllLoadouts().Select(l => l.Name).ToHashSet();
        for (var i = 1; i < 1000; i++)
        {
            var name = $"My Loadout {i}";
            if (!names.Contains(name))
                return name;
        }

        return $"My Loadout {Guid.NewGuid()}";
    }

    /// <summary>
    /// Manages the given installation, returning a marker for the new loadout.
    /// </summary>
    /// <param name="installation"></param>
    /// <returns></returns>
    public async Task<LoadoutMarker> Manage(GameInstallation installation, string name = "")
    {
        if (string.IsNullOrWhiteSpace(name))
            name = SuggestName(installation);

        var result = await installation.GetGame().Synchronizer.Manage(installation, name);

        return GetMarker(result.LoadoutId);
    }

    /// <summary>
    /// Returns true if the given loadout id exists.
    /// </summary>
    /// <param name="loadoutId"></param>
    /// <returns></returns>
    public bool Contains(LoadoutId loadoutId)
    {
        return GetId(loadoutId) != null;
    }
}
