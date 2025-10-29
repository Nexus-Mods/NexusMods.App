using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.Models;

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
public readonly struct GraphQLResolver(Transaction Tx, ReadOnlyModel Model, bool Existing)
{
    /// <summary>
    /// Create a new resolver using the given primary key attribute and value.
    /// </summary>
    public static GraphQLResolver Create<THighLevel, TLowLevel, TSerializer>(IDb db, Transaction tx, Attribute<THighLevel, TLowLevel, TSerializer> primaryKeyAttribute, THighLevel primaryKeyValue)
        where THighLevel : IEquatable<THighLevel> 
        where TLowLevel : notnull 
        where TSerializer : IValueSerializer<TLowLevel>
    {
        if (primaryKeyAttribute == null) throw new ArgumentNullException(nameof(primaryKeyAttribute));
        if (!primaryKeyAttribute.IsIndexed) throw new ArgumentException($"Attribute {primaryKeyAttribute.Id} is not indexed", nameof(primaryKeyAttribute));

        var existing = db.Datoms(primaryKeyAttribute, primaryKeyValue);
        var exists = existing.Count > 0;
        var id = existing.Count == 0 ? tx.TempId() : existing[0].E;
        if (!exists) tx.Add(id, primaryKeyAttribute, primaryKeyValue);
        return new GraphQLResolver(tx, new ReadOnlyModel(db, id), exists);
    }
    
    /// <summary>
    /// The id of the entity, may be temporary if this is a new entity.
    /// </summary>
    public EntityId Id => Model.Id;
    
    /// <summary>
    /// Add a value to the entity. If the value already exists, it will not be added again.
    /// </summary>
    public void Add<THighLevel, TLowLevel, TSerializer>(Attribute<THighLevel, TLowLevel, TSerializer> attribute, THighLevel value) 
        where THighLevel : notnull 
        where TLowLevel : notnull 
        where TSerializer : IValueSerializer<TLowLevel>
    {
        foreach (var datom in Model)
        {
            if (datom.A != attribute)
                continue;
            
            if (datom.V.Equals(value))
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
        
        if (Model.EntitySegment.GetAllResolved(attribute).Contains(id))
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
        
        if (Model.EntitySegment.GetAllResolved(attribute).Contains(id))
            return;
        
        // Else add the value
        Tx.Add(Model.Id, attribute, id);
    }
}
