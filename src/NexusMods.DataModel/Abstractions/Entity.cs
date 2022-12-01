using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using NexusMods.DataModel.ModLists;
using NexusMods.DataModel.ModLists.ModFiles;

namespace NexusMods.DataModel.Abstractions;

[JsonDerivedType(typeof(ModList), nameof(ModList))]
[JsonDerivedType(typeof(Mod), nameof(Mod))]
[JsonDerivedType(typeof(FromArchive), nameof(FromArchive))]
[JsonDerivedType(typeof(ListRegistry), nameof(ListRegistry))]
[JsonDerivedType(typeof(GameFile), nameof(GameFile))]
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
    public IDataStore Store { get; }

    private Id? _id = null;

    protected Entity()
    {
        Store = IDataStore.CurrentStore.Value!;
        if (Store == null)
            throw new Exception("Entity created outside of a IDataStore context");
    }

    [JsonIgnore]
    public Id Id
    {
        get { return _id ??= Store.Put(this); }
    }
}