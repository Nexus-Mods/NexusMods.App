using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.Models;
using Splat.ModeDetection;

namespace NexusMods.Abstractions.NexusModsLibrary;

/// <summary>
/// A helper for upserting entities in the database. When created, you must define a "pimary key" attribute and value,
/// these are used to determine if the entity already exists in the database. If it does, the existing entity is updated,
/// otherwise a new entity is created.
///
/// For each attribute you want to add to the entity, call the Add method with the attribute and value and any duplicate values
/// will not be added.
/// </summary>
// ReSharper disable once InconsistentNaming
public readonly struct GraphQLResolver(ITransaction Tx, ReadOnlyModel Model)
{
    /// <summary>
    /// Create a new resolver using the given primary key attribute and value.
    /// </summary>
    public static GraphQLResolver Create<THighLevel, TLowLevel>(IDb referenceDb, ITransaction tx, ScalarAttribute<THighLevel, TLowLevel> primaryKeyAttribute, THighLevel primaryKeyValue) where THighLevel : notnull
    {
        var existing = referenceDb.Datoms(primaryKeyAttribute, primaryKeyValue);
        var exists = existing.Count > 0;
        var id = existing.Count == 0 ? tx.TempId() : existing[0].E;
        if (!exists)
            tx.Add(id, primaryKeyAttribute, primaryKeyValue);
        return new GraphQLResolver(tx, new ReadOnlyModel(referenceDb, id));
    }
    
    /// <summary>
    /// The id of the entity, may be temporary if this is a new entity.
    /// </summary>
    public EntityId Id => Model.Id;
    
    /// <summary>
    /// Add a value to the entity. If the value already exists, it will not be added again.
    /// </summary>
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
    
    /// <summary>
    /// Add a value to the entity. If the value already exists, it will not be added again.
    /// </summary>
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
    
    /// <summary>
    /// Add a value to the entity. If the value already exists, it will not be added again.
    /// </summary>
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
