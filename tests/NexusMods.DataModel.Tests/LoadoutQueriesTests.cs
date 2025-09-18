using FluentAssertions;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Games.TestFramework;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;
using NexusMods.Paths;
using Xunit.Abstractions;

namespace NexusMods.DataModel.Tests;

public class LoadoutQueriesTests(ITestOutputHelper helper) : ACyberpunkIsolatedGameTest<LoadoutQueriesTests>(helper)
{

    [Fact]
    public async Task CollectionEnabledStateInLoadoutQuery_Test()
    {
        var loadout = await CreateLoadout();
        var collection1 = await CreateCollection(loadout.LoadoutId, "Collection 1");
        var collection2 = await CreateCollection(loadout.LoadoutId, "Collection 2");

        // Check if all collections are enabled
        {
            var collectionStates = Loadout.CollectionEnabledStateInLoadoutQuery(Connection, loadout.Id);
            collectionStates.Should().OnlyContain(x => x.IsEnabled);
        }
        
        // Disable the collection
        using (var tx = Connection.BeginTransaction())
        {
            tx.Add(collection1.Id,
                LoadoutItem.Disabled,
                Null.Instance,
                isRetract: false
            );
            await tx.Commit();
        }
        
        // Check the collection states again
        {
            var collectionStates = Loadout.CollectionEnabledStateInLoadoutQuery(Connection, loadout.Id);
            // The collection should be disabled
            var targetItem = collectionStates.Should().ContainSingle(x => x.CollectionId == collection1.Id).Subject;
            targetItem.IsEnabled.Should().BeFalse();
        }
    }

    [Fact]
    public async Task LoadoutItemGroupEnabledStateInLoadoutQuery_Test()
    {
        var loadout = await CreateLoadout();
        var collection1 = await CreateCollection(loadout.LoadoutId, "Collection 1");
        
        var modFiles = new List<RelativePath> { "Data/textureA.dds" };

        // Add a mod to the collection
        using var addModTx = Connection.BeginTransaction();
        var modA = await AddModAsync(addModTx,
            modFiles,
            loadout.LoadoutId,
            "Mod A",
            parentGroup: collection1.Id
        );
        await addModTx.Commit();
        
        var loadoutFiles = LoadoutFile.FindByHash(Connection.Db, modA.hashes.First());
        loadoutFiles.Should().ContainSingle();
        var group = loadoutFiles.First().AsLoadoutItemWithTargetPath().AsLoadoutItem().Parent;
        
        // Check if the group is enabled
        {
            var groupStates = Loadout.LoadoutItemGroupEnabledStateInLoadoutQuery(Connection, loadout.Id);
            groupStates.Should().OnlyContain(x => x.IsEnabled);
        }
        
        // Disable the group
        using (var tx = Connection.BeginTransaction())
        {
            tx.Add(group.Id,
                LoadoutItem.Disabled,
                Null.Instance,
                isRetract: false
            );
            await tx.Commit(); 
        }
        
        // Check the group states again
        {
            var groupStates = Loadout.LoadoutItemGroupEnabledStateInLoadoutQuery(Connection, loadout.Id);
            var targetItem = groupStates.Should().ContainSingle(x => x.GroupId == group.Id).Subject;
            targetItem.IsEnabled.Should().BeFalse();
        }
        
        // Re-enable the group, disable the collection
        using (var tx = Connection.BeginTransaction())
        {
            tx.Add(group.Id,
                LoadoutItem.Disabled,
                Null.Instance,
                isRetract: true
            ); // Re-enable the group
            tx.Add(collection1.Id,
                LoadoutItem.Disabled,
                Null.Instance,
                isRetract: false
            ); // Disable the collection
            await tx.Commit();
        }
        
        // Check the group states again
        {
            var groupStates = Loadout.LoadoutItemGroupEnabledStateInLoadoutQuery(Connection, loadout.Id);
            var targetItem = groupStates.Should().ContainSingle(x => x.GroupId == group.Id).Subject;
            targetItem.IsEnabled.Should().BeFalse(); // Group should still be disabled due to collection being disabled
        }
    }

    [Fact]
    public async Task LoadoutItemEnabledStateInLoadoutQuery_Test()
    {
        var loadout = await CreateLoadout();
        var collection1 = await CreateCollection(loadout.LoadoutId, "Collection 1");

        var modFiles = new List<RelativePath> { "Data/textureA.dds" };

        // Add a mod to the collection
        using var addModTx = Connection.BeginTransaction();
        var modA = await AddModAsync(addModTx,
            modFiles,
            loadout.LoadoutId,
            "Mod A",
            parentGroup: collection1.Id
        );
        await addModTx.Commit();

        var loadoutFiles = LoadoutFile.FindByHash(Connection.Db, modA.hashes.First());
        loadoutFiles.Should().ContainSingle();
        var item = loadoutFiles.First().AsLoadoutItemWithTargetPath().AsLoadoutItem();

        // Check if the item is enabled
        {
            var itemStates = Loadout.LoadoutItemEnabledStateInLoadoutQuery(Connection, loadout.Id);
            itemStates.Should().OnlyContain(x => x.IsEnabled);
        }

        // Disable the item
        using (var tx = Connection.BeginTransaction())
        {
            tx.Add(item.Id,
                LoadoutItem.Disabled,
                Null.Instance,
                isRetract: false
            );
            await tx.Commit();
        }

        // Check the item states again
        {
            var itemStates = Loadout.LoadoutItemEnabledStateInLoadoutQuery(Connection, loadout.Id);
            var targetItem = itemStates.Should().ContainSingle(x => x.ItemId == item.Id).Subject;
            targetItem.IsEnabled.Should().BeFalse();
        }

        // Re-enable the item, disable the group
        using (var tx = Connection.BeginTransaction())
        {
            tx.Add(item.Id,
                LoadoutItem.Disabled,
                Null.Instance,
                isRetract: true
            ); // Re-enable the item
            tx.Add(item.Parent.Id,
                LoadoutItem.Disabled,
                Null.Instance,
                isRetract: false
            ); // Disable the group
            await tx.Commit();
        }

        // Check the item states again
        {
            var itemStates = Loadout.LoadoutItemEnabledStateInLoadoutQuery(Connection, loadout.Id);
            var targetItem = itemStates.Should().ContainSingle(x => x.ItemId == item.Id).Subject;
            targetItem.IsEnabled.Should().BeFalse(); // Item should still be disabled due to group being disabled
        }

        // Re-enable the group, disable the collection
        using (var tx = Connection.BeginTransaction())
        {
            tx.Add(item.Parent.Id,
                LoadoutItem.Disabled,
                Null.Instance,
                isRetract: true
            ); // Re-enable the group
            tx.Add(collection1.Id,
                LoadoutItem.Disabled,
                Null.Instance,
                isRetract: false
            ); // Disable the collection
            await tx.Commit();
        }

        // Check the item states again
        {
            var itemStates = Loadout.LoadoutItemEnabledStateInLoadoutQuery(Connection, loadout.Id);
            var targetItem = itemStates.Should().ContainSingle(x => x.ItemId == item.Id).Subject;
            targetItem.IsEnabled.Should().BeFalse(); // Item should still be disabled due to collection being disabled
        }
    }
}
