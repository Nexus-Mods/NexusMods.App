using System.Text.Json.Serialization;
using NexusMods.DataModel.JsonConverters;

namespace NexusMods.DataModel.Abstractions;
public abstract record Entity : IWalkable<Entity>
{
    public Entity(Entity self)
    {
        Store = self.Store;
        if (Store == null)
            throw new NoDataStoreException();
    }
    
    [JsonIgnore]
    public abstract EntityCategory Category { get; }
    
    [JsonInjected]
    public required IDataStore Store { get; init; }

    private Id? _id = null;

    protected Entity()
    {
    }

    protected virtual Id Persist()
    {
        return Store.Put(this);
    }
    
    public void EnsureStored()
    {
        _id ??= Persist();
    }

    [JsonIgnore]
    public Id DataStoreId
    {
        get { return _id ??= Persist(); }
    }
    
    public TState Walk<TState>(Func<TState, Entity, TState> visitor, TState initial)
    {
        // TODO: cache this as a Linq.Expression compiled lambda, for now just use reflection
        var state = visitor(initial, this);
        foreach (var property in GetType().GetProperties())
        {
            if (!property.PropertyType.IsAssignableTo(typeof(IWalkable<Entity>)))
                continue;
            
            var value = property.GetValue(this);
            if (value is IWalkable<Entity> walkable)
            {
                state = walkable.Walk(visitor, state);
            }
        }

        return state;
    }
}