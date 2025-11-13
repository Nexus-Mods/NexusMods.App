using NexusMods.Abstractions.Loadouts;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.Sdk.Loadouts;
using OneOf;

namespace NexusMods.DataModel.SchemaVersions.Migrations;

/// <summary>
/// Adds the ParentEntity attribute to all SortOrder entities, setting it to the Loadout's EntityId.
/// </summary>
internal class _0007_AddSortOrderParentEntity : ITransactionalMigration
{
    public static (MigrationId Id, string Name) IdAndName { get; } = MigrationId.ParseNameAndId(nameof(_0007_AddSortOrderParentEntity));
    
    private (EntityId SortOrderId, EntityId LoadoutId)[] _sortOrdersToUpdate = [];

    /// <inheritdoc />
    public Task Prepare(IDb db)
    {
        // Select all SortOrder entities that do not yet have the ParentEntity attribute
        _sortOrdersToUpdate = db.Datoms(SortOrder.LoadoutId)
            .Resolved(db.Connection)
            .OfType<ReferenceAttribute<Loadout>.ReadDatom>()
            .Select(datom =>
                {
                    return (datom.E, datom.V);
                }
            ).ToArray();

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public void Migrate(ITransaction tx, IDb db)
    {
        foreach (var (sortOrderId, loadoutId) in _sortOrdersToUpdate)
        {
            // Add the ParentEntity attribute to the SortOrder, using the Loadout's EntityId
            // We assume that all SortOrders without the attribute are for Loadouts and not Collections.
            tx.Add(sortOrderId, SortOrder.ParentEntity, OneOf<LoadoutId,CollectionGroupId>.FromT0(loadoutId));
        }
    }
}
