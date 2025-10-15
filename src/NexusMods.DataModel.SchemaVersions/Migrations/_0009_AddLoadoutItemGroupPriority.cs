using System.Diagnostics;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Loadouts.Synchronizers.Conflicts;
using NexusMods.HyperDuck;
using NexusMods.MnemonicDB.Abstractions;

namespace NexusMods.DataModel.SchemaVersions.Migrations;

public class _0009_AddLoadoutItemGroupPriority : ITransactionalMigration
{
    public static (MigrationId Id, string Name) IdAndName => MigrationId.ParseNameAndId(nameof(_0009_AddLoadoutItemGroupPriority));

    private static EntityId[] Query(IDb db, LoadoutId loadoutId)
    {
        return db.Connection.Query<EntityId>($"""
                                                SELECT
                                                  item_group.Id
                                                FROM
                                                  loadouts.ItemGroupEnabledState ({db}, {loadoutId}) item_group
                                                  LEFT JOIN MDB_LOADOUTITEMGROUPPRIORITY (Db=>{db}) group_priority ON item_group.Id = group_priority.Target
                                                WHERE
                                                  group_priority.Target IS NULL
                                                ORDER BY item_group.Id;
                                                """
        ).ToArray();
    }

    private Dictionary<LoadoutId, EntityId[]> _loadouts = [];

    public Task Prepare(IDb db)
    {
        var loadouts = Loadout.All(db);
        _loadouts = loadouts.ToDictionary(loadout => loadout.LoadoutId, loadout => Query(db, loadout));
        return Task.CompletedTask;
    }

    public void Migrate(ITransaction tx, IDb db)
    {
        foreach (var kv in _loadouts)
        {
            var groups = kv.Value;
            Debug.Assert(groups.Length >= 0);
            for (ulong i = 0; i < (ulong)groups.Length; i++)
            {
                var loadoutItemGroup = LoadoutItemGroupId.From(groups[i]);
                var priority = new LoadoutItemGroupPriority.New(tx)
                {
                    TargetId = loadoutItemGroup,
                    Priority = ConflictPriority.From(i),
                    LoadoutId = kv.Key
                };
            }
        }
    }
}
