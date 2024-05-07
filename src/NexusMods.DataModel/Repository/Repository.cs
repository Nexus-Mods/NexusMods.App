using System.Collections.ObjectModel;
using System.Reactive.Linq;
using DynamicData;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.MnemonicDB.Attributes;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Models;
using static System.Reactive.Linq.Observable;

namespace NexusMods.DataModel.Repository;

internal class Repository<TModel> : IRepository<TModel> where TModel : Entity
{
    private readonly IConnection _conn;
    private readonly IAttribute[] _watchedAttributes;

    /// <summary>
    /// DI constructor.
    /// </summary>
    public Repository(IAttribute[] watchedAttributes, IConnection connection)
    {
        _conn = connection;
        _watchedAttributes = watchedAttributes;
        
        _cache
            .Connect()
            .Bind(out _observable)
            .Subscribe();
        
        _conn.Revisions
            .SelectMany(db => db.Datoms(db.BasisTxId).Select(datom => (datom, db)))
            .Where(d => _watchedAttributes.Contains(d.datom.A))
            .Select(d => (Db: d.db, E: d.datom.E))
            .StartWith(All.Select(l => (Db: l.Db, E:l.Id)))
            .Subscribe(dbAndE =>
            {
                _cache.Edit(x =>
                {
                    x.AddOrUpdate(dbAndE.Db.Get<TModel>(dbAndE.E));
                });
            });
    }

    /// <inheritdoc />
    public IEnumerable<TModel> All
    {
        get
        {
            var db = _conn.Db;
            var items = db.Find(_watchedAttributes[0]);
            foreach (var attr in _watchedAttributes.Skip(1))
            {
                items = items.Intersect(db.Find(attr));
            }

            return items.Select(id => db.Get<TModel>(id));
        }
    }
    
    private readonly SourceCache<TModel, EntityId> _cache = new(x => x.Id);

    private readonly ReadOnlyObservableCollection<TModel> _observable;

    /// <inheritdoc />
    public ReadOnlyObservableCollection<TModel> Observable => _observable;

    /// <inheritdoc />
    public IObservable<TModel> Revisions(EntityId id)
    {
        return _conn.Revisions
            .Where(db => db.Datoms(db.BasisTxId).Any(datom => datom.E == id.Value))
            .StartWith(_conn.Db)
            .Select(db => db.Get<TModel>(id))
            .Where(IsValid);
    }

    private bool IsValid(TModel model)
    {
        foreach (var attribute in _watchedAttributes)
        {
            if (!model.Contains(attribute))
                return false;
        }
        return true;
    }

    /// <inheritdoc />
    public bool Exists(EntityId eid)
    {
        var entity = _conn.Db.Get<TModel>(eid);
        foreach (var attribute in _watchedAttributes)
        {
            if (!entity.Contains(attribute))
                return false;
        }

        return true;
    }
}


/// <summary>
/// DI extensions for the repository.
/// </summary>
public static class Extensions
{
    
    /// <summary>
    /// Registers a repository for a model with the given attributes.
    /// </summary>
    public static IServiceCollection AddRepository<TModel>(this IServiceCollection collection, params IAttribute[] attributes) where TModel : Entity
    {
        return collection
            .AddSingleton<IRepository<TModel>>(provider => 
                new Repository<TModel>(attributes, provider.GetRequiredService<IConnection>()));
    }
}
