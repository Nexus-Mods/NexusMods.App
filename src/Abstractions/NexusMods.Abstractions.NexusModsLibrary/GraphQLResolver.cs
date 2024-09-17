using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.Models;
using Splat.ModeDetection;

namespace NexusMods.Abstractions.NexusModsLibrary;

public struct GraphQLResolver(ITransaction Tx, ReadOnlyModel Model)
{
    public static GraphQLResolver Create<THighLevel, TLowLevel>(IDb referenceDb, ITransaction tx, ScalarAttribute<THighLevel, TLowLevel> primaryKeyAttribute, THighLevel primaryKeyValue) where THighLevel : notnull
    {
        var existing = referenceDb.Datoms(primaryKeyAttribute, primaryKeyValue);
        var id = existing.Count == 0 ? tx.TempId() : existing[0].E;
        return new GraphQLResolver(tx, new ReadOnlyModel(referenceDb, id));
    }
    
    /// <summary>
    /// The id of the entity, may be temporary if this is a new entity.
    /// </summary>
    public EntityId Id => Model.Id;
    
    public void Add<THighLevel, TLowLevel>(ScalarAttribute<THighLevel, TLowLevel> attribute, THighLevel value) 
        where THighLevel : notnull
    {
        if (attribute.TryGet(Model, out var foundValue))
        {
            // Deduplicate values
            if (foundValue.Equals(value))
                return;
        }
        // Else add the value
        Tx.Add(Model.Id, attribute, value);
    }
    
    public void Add<TOther>(ReferencesAttribute<TOther> attribute, EntityId id) 
        where TOther : IModelDefinition
    {
        if (PartitionId.Temp == id.Partition)
        {
            Tx.Add(Model.Id, attribute, id);
            return;
        }
        
        if (attribute.Get(Model).Contains(id))
            return;
        
        // Else add the value
        Tx.Add(Model.Id, attribute, id);
    }
    
    public void Add<TOther>(ReferenceAttribute<TOther> attribute, EntityId id) 
        where TOther : IModelDefinition
    {
        if (PartitionId.Temp == id.Partition)
        {
            Tx.Add(Model.Id, attribute, id);
            return;
        }
        
        if (attribute.Get(Model).Equals(id))
            return;
        
        // Else add the value
        Tx.Add(Model.Id, attribute, id);
    }
}
