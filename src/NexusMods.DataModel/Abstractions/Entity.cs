﻿using System.Text.Json.Serialization;

namespace NexusMods.DataModel.Abstractions;
public abstract record Entity
{
    public Entity(Entity self)
    {
        Store = self.Store;
        if (Store == null)
            throw new NoDataStoreException();
    }
    
    [JsonIgnore]
    public abstract EntityCategory Category { get; }
    
    [JsonIgnore]
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
    public Id Id
    {
        get { return _id ??= Persist(); }
    }
}