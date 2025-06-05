using FluentAssertions;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Loadouts.Extensions;
using NexusMods.Games.TestFramework;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;
using NexusMods.MnemonicDB.Abstractions.TxFunctions;
using NexusMods.Paths;
using Xunit.Abstractions;

namespace NexusMods.DataModel.Tests;

public class LoaodutQueriesTests(ITestOutputHelper helper) : ACyberpunkIsolatedGameTest<LoaodutQueriesTests>(helper)
{
    [Fact]
    public async Task DeletingAModShouldUpdateTheLoadout()
    {
        using var loadouts = await Connection.Topology.QueryAsync(Loadout.MostRecentTxForLoadoutFlow);
        using var tx = Connection.BeginTransaction();
        var loadoutId = tx.TempId();

        var loadout = new Loadout.New(tx, loadoutId)
        {
            Name = "Test Loadout",
            ShortName = "B",
            InstallationId = GameInstallation.GameMetadataId,
            LoadoutKind = LoadoutKind.Default,
            Revision = 0,
            GameVersion = VanityVersion.From("Unknown"),
        };

        var group = new LoadoutItemGroup.New(tx, out var groupId)
        {
            IsGroup = true,
            LoadoutItem = new LoadoutItem.New(tx, groupId)
            {
                LoadoutId = loadoutId,
                Name = "Test Group",
            }
        };
        
        var fileId = tx.TempId();
        var file = new LoadoutItem.New(tx, fileId)
        {
            LoadoutId = loadoutId,
            Name = "Test Mod",
            ParentId = groupId,
        };


        var result = await tx.Commit();
        loadoutId = result[loadoutId];
        fileId = result[fileId];
        groupId = result[groupId];

        await Connection.Topology.FlushEffectsAsync();
        var firstRow = loadouts.First();
        firstRow.RowId.Should().Be(loadoutId);
        var originalItemCount = firstRow.ItemCount;

        
        // Delete a file and the row should update
        using var tx2 = Connection.BeginTransaction();
        tx2.Delete(fileId, false);
        var result2 = await tx2.Commit();
        
        await Connection.Topology.FlushEffectsAsync();
        var firstRow2 = loadouts.First();
        var newItemCount = firstRow2.ItemCount;
        newItemCount.Should().NotBe(originalItemCount);
    }
    
    [Fact]
    public async Task IsCollectionEnabledFlow_ShouldReturnCorrectly()
    {
        var loadout = await CreateLoadout();
        var collection = await CreateCollection(loadout.LoadoutId, "Collection 1");
        
        // Check if the collection is enabled
        var initialIsEnabled = Loadout.IsCollectionEnabled(Connection.Db, collection.Id);
        initialIsEnabled.Should().BeTrue();
        
        // Disable the collection
        using var tx = Connection.BeginTransaction();
        tx.Add(collection.Id, LoadoutItem.Disabled, Null.Instance, isRetract: false);
        var result = await tx.Commit();
        
        var finalIsEnabled = Loadout.IsCollectionEnabled(Connection.Db, collection.Id);
        finalIsEnabled.Should().BeFalse();
    }
    
    [Fact]
    public async Task IsLoadoutItemGroupEnabledFlow_ShouldReturnCorrectly()
    {
        var loadout = await CreateLoadout();
        var collection = await CreateCollection(loadout.LoadoutId, "Collection 1");
        var modFiles = new List<RelativePath> { "Data/textureA.dds" };
        
        using var tx = Connection.BeginTransaction();
        
        var modA = await AddModAsync(tx, modFiles, loadout.LoadoutId, "Mod A", parentGroup: collection.Id);
        modA.hashes.Should().ContainSingle();
        
        await tx.Commit();

        var loadoutFiles = LoadoutFile.FindByHash(Connection.Db, modA.hashes.First());
        loadoutFiles.Should().ContainSingle();

        var group = loadoutFiles.First().AsLoadoutItemWithTargetPath().AsLoadoutItem().Parent;
        
        group.AsLoadoutItem().IsDisabled.Should().BeFalse();
        
        group.AsLoadoutItem().IsEnabled().Should().BeTrue();

        // Check if the group is enabled
        var isEnabled = Loadout.IsLoadoutItemGroupEnabled(Connection.Db, group.Id);
        isEnabled.Should().BeTrue();

        // Disable the group
        using var tx1 = Connection.BeginTransaction();
        tx1.Add(group.Id, LoadoutItem.Disabled, Null.Instance, isRetract: false);
        await tx1.Commit();
        
        // Should now be disabled
        isEnabled = Loadout.IsLoadoutItemGroupEnabled(Connection.Db, group.Id);
        isEnabled.Should().BeFalse();
        
        // Re-enable the group
        using var tx2 = Connection.BeginTransaction();
        tx2.Add(group.Id, LoadoutItem.Disabled, Null.Instance, isRetract: true);
        await tx2.Commit();
        
        // Should now be enabled
        isEnabled = Loadout.IsLoadoutItemGroupEnabled(Connection.Db, group.Id);
        isEnabled.Should().BeTrue();
        
        // Disable the collection
        using var tx3 = Connection.BeginTransaction();
        tx3.Add(collection.Id, LoadoutItem.Disabled, Null.Instance, isRetract: false);
        await tx3.Commit();
        
        // Should now be disabled due to collection being disabled
        isEnabled = Loadout.IsLoadoutItemGroupEnabled(Connection.Db, group.Id);
        isEnabled.Should().BeFalse();
    }
    
    [Fact]
    public async Task IsLoadoutItemEnabledFlow_ShouldReturnCorrectly()
    {
        var loadout = await CreateLoadout();
        var collection = await CreateCollection(loadout.LoadoutId, "Collection 1");
        var modFiles = new List<RelativePath> { "Data/textureA.dds" };
        
        using var tx = Connection.BeginTransaction();
        
        var modA = await AddModAsync(tx, modFiles, loadout.LoadoutId, "Mod A", parentGroup: collection.Id);
        modA.hashes.Should().ContainSingle();
        
        await tx.Commit();

        var loadoutFiles = LoadoutFile.FindByHash(Connection.Db, modA.hashes.First());
        loadoutFiles.Should().ContainSingle();

        var item = loadoutFiles.First().AsLoadoutItemWithTargetPath().AsLoadoutItem();
        
        item.IsDisabled.Should().BeFalse();
        
        item.IsEnabled().Should().BeTrue();

        // Check if the item is enabled
        var isEnabled = Loadout.IsLoadoutItemEnabled(Connection.Db, item.Id);
        isEnabled.Should().BeTrue();

        // Disable the item
        using var tx2 = Connection.BeginTransaction();
        tx2.Add(item.Id, LoadoutItem.Disabled, Null.Instance, isRetract: false);
        await tx2.Commit();
        
        // Should now be disabled
        isEnabled = Loadout.IsLoadoutItemEnabled(Connection.Db, item.Id);
        isEnabled.Should().BeFalse();
        
        // Re-enable the item
        using var tx3 = Connection.BeginTransaction();
        tx3.Add(item.Id, LoadoutItem.Disabled, Null.Instance, isRetract: true);
        await tx3.Commit();
        
        // Should now be enabled
        isEnabled = Loadout.IsLoadoutItemEnabled(Connection.Db, item.Id);
        isEnabled.Should().BeTrue();
        
        // Disable the collection
        using var tx4 = Connection.BeginTransaction();
        tx4.Add(collection.Id, LoadoutItem.Disabled, Null.Instance, isRetract: false);
        await tx4.Commit();
        
        // Should now be disabled due to collection being disabled
        isEnabled = Loadout.IsLoadoutItemEnabled(Connection.Db, item.Id);
        isEnabled.Should().BeFalse();
        
        // Re-enable the collection
        using var tx5 = Connection.BeginTransaction();
        tx5.Add(collection.Id, LoadoutItem.Disabled, Null.Instance, isRetract: true);
        await tx5.Commit();
        
        // Should now be enabled again
        isEnabled = Loadout.IsLoadoutItemEnabled(Connection.Db, item.Id);
        isEnabled.Should().BeTrue();
        
        // Disable the group
        var group = item.Parent;
        using var tx6 = Connection.BeginTransaction();
        tx6.Add(group.Id, LoadoutItem.Disabled, Null.Instance, isRetract: false);
        await tx6.Commit();
        
        // Should now be disabled due to group being disabled
        isEnabled = Loadout.IsLoadoutItemEnabled(Connection.Db, item.Id);
        isEnabled.Should().BeFalse();
    }
    
}
