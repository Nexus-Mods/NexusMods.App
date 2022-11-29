using System.Diagnostics;
using System.Text.Json.Serialization;

namespace NexusMods.DataModel.Abstractions;

[JsonDerivedType(typeof(ModFile), "ModFile")]
public record Entity
{
    public Entity(Entity self)
    {
        Store = self.Store;
    }
    
    [JsonIgnore]
    public IDataStore Store { get; init; }

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
        init => _id = value;
    }
}